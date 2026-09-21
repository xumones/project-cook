using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using ProjectCook.Core;

namespace ProjectCook.Player
{
    /// <summary>ปรับกล้องตามโหมดของเกม สถานีต้องตั้ง StationCamera ก่อนเปลี่ยนโหมด</summary>
    public class CameraManager : SceneSingleton<CameraManager>
    {
        [Header("Default Player Camera")]
        [Tooltip("Cinemachine Virtual Camera")]
        [SerializeField] private CinemachineCamera playerCamera;

        [Header("Priority Settings")]
        [SerializeField] private int activePriority = 1;
        [SerializeField] private int inactivePriority = 0;

        [Header("Dialogue Look Settings")]
        [Tooltip("มุมกวาดแนวนอนทั้งหมดที่หมุนได้ระหว่างคุย (องศา) ต้องมาปรับจูนอีกทีตอนทดสอบจริง")]
        [SerializeField] private float dialoguePanRange = 70f;
        [Tooltip("มุมก้ม/เงยต่ำสุดที่หมุนได้ระหว่างคุย (องศา) ต้องมาปรับจูนอีกทีตอนทดสอบจริง")]
        [SerializeField] private float dialogueTiltMin = -15f;
        [Tooltip("มุมก้ม/เงยสูงสุดที่หมุนได้ระหว่างคุย (องศา) ต้องมาปรับจูนอีกทีตอนทดสอบจริง")]
        [SerializeField] private float dialogueTiltMax = 40f;

        // แคชไว้ตอน subscribe เพราะตอนหยุดเกม Instance คืน null ไปแล้ว แต่ยังต้องถอน event ให้ครบ
        private GameModeManager gameMode;

        private GameMode currentMode = GameMode.Exploring;

        // ของในมือยังลอยอยู่ที่ Hand Anchor ระหว่างอยู่สถานี จึงซ่อนทั้ง layer HeldItem จากกล้องหลักแทน
        private int heldItemLayer = -1;

        // ปฏิกิริยาของกล้องต่อแต่ละโหมด เพิ่มโหมดใหม่ = เพิ่ม handler ที่นี่ ไม่ต้องแก้ HandleModeChanged
        private readonly Dictionary<GameMode, ICameraModeHandler> modeHandlers = new Dictionary<GameMode, ICameraModeHandler>();

        /// <summary>กล้องของสถานีที่กำลังจะเข้าใช้งาน ตั้งก่อนสั่งเปลี่ยนโหมดเป็น Station</summary>
        public CinemachineCamera StationCamera { get; set; }

        /// <summary>เปิด/ปิดการรับ mouse look ของกล้องตัวนั้น กล้องที่ไม่ได้ใช้อยู่ต้องปิดไม่งั้นมันหมุนตามเมาส์อยู่เงียบ ๆ</summary>
        public static void SetLookInput(CinemachineCamera camera, bool enabled)
        {
            if (camera == null) return;

            var input = camera.GetComponent<CinemachineInputAxisController>();
            if (input != null) input.enabled = enabled;
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            modeHandlers[GameMode.Station] = new StationCameraHandler(playerCamera, activePriority, inactivePriority,
                () => StationCamera, SetHeldItemVisible);
            modeHandlers[GameMode.Dialogue] = new DialogueCameraHandler(playerCamera, dialoguePanRange, dialogueTiltMin, dialogueTiltMax);
        }

        private void Start()
        {
            if (playerCamera != null)
            {
                playerCamera.Priority = activePriority;
            }

            gameMode = GameModeManager.Instance;
            if (gameMode != null)
            {
                gameMode.ModeChanged += HandleModeChanged;
            }

            heldItemLayer = LayerMask.NameToLayer(GameLayers.HeldItem);
        }

        private void SetHeldItemVisible(bool visible)
        {
            Camera cam = Camera.main;
            if (cam == null || heldItemLayer < 0) return;

            int bit = 1 << heldItemLayer;
            cam.cullingMask = visible ? cam.cullingMask | bit : cam.cullingMask & ~bit;
        }

        protected override void OnDestroy()
        {
            if (gameMode != null)
            {
                gameMode.ModeChanged -= HandleModeChanged;
            }

            base.OnDestroy();
        }

        private void HandleModeChanged(GameMode mode)
        {
            GameMode previous = currentMode;
            currentMode = mode;

            if (modeHandlers.TryGetValue(previous, out ICameraModeHandler previousHandler))
            {
                previousHandler.OnExit();
            }

            if (modeHandlers.TryGetValue(mode, out ICameraModeHandler nextHandler))
            {
                nextHandler.OnEnter();
            }
        }
    }
}
