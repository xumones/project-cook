using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using ProjectCook.Core;

namespace ProjectCook.Player
{
    /// <summary>ปรับกล้องตามโหมดของเกม สถานีต้องเรียก SetStationCamera ก่อนเข้าโหมด</summary>
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

        // ของในมือยังลอยอยู่ที่ Hand Anchor ระหว่างอยู่สถานี จึงซ่อนทั้ง layer HeldItem จากกล้องหลักแทน
        private int heldItemLayer = -1;

        // ปฏิกิริยาของกล้องต่อแต่ละโหมด ทำงานตลอดที่โหมดนั้นอยู่ชั้นใดชั้นหนึ่ง เพิ่มโหมดใหม่ = เพิ่ม handler ที่นี่
        private readonly Dictionary<GameMode, ICameraModeHandler> modeHandlers = new Dictionary<GameMode, ICameraModeHandler>();
        private readonly HashSet<GameMode> activeModes = new HashSet<GameMode>();

        // ผู้ขอล็อกการหันกล้อง และตัวรับ mouse look ที่ถูกปิดไว้ (เพื่อเปิดคืนตัวเดิมตอนปลด)
        private readonly HashSet<Object> lookLocks = new HashSet<Object>();
        private CinemachineInputAxisController lockedLookInput;

        private CinemachineCamera stationCamera;
        private StationCameraHandler stationHandler;

        private bool InStation => activeModes.Contains(GameMode.Station);

        /// <summary>ตั้งกล้องสถานี เรียกก่อนเข้าโหมด Station หรือระหว่างอยู่ในสถานีเพื่อสลับมุมกล้อง</summary>
        public void SetStationCamera(CinemachineCamera camera)
        {
            stationCamera = camera;
            if (InStation) stationHandler.SwitchTo(camera);
        }

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

            stationHandler = new StationCameraHandler(playerCamera, activePriority, inactivePriority,
                () => stationCamera, SetHeldItemVisible);
            modeHandlers[GameMode.Station] = stationHandler;
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

        /// <summary>ขอหยุด mouse look ของกล้องที่ใช้งานอยู่ชั่วคราว ต้องเรียก ReleaseLookLock คู่กันเสมอ</summary>
        public void RequestLookLock(Object requester)
        {
            if (requester != null && lookLocks.Add(requester)) ApplyLookLock();
        }

        public void ReleaseLookLock(Object requester)
        {
            if (requester == null || !lookLocks.Remove(requester) || lookLocks.Count > 0) return;

            UnlockLook();
        }

        private void ApplyLookLock()
        {
            if (lockedLookInput != null) return;

            CinemachineCamera live = InStation ? stationCamera : playerCamera;
            var input = live != null ? live.GetComponent<CinemachineInputAxisController>() : null;
            if (input == null || !input.enabled) return;

            input.enabled = false;
            lockedLookInput = input;
        }

        private void UnlockLook()
        {
            if (lockedLookInput != null) lockedLookInput.enabled = true;
            lockedLookInput = null;
        }

        private void HandleModeChanged(GameMode mode)
        {
            // ปลดล็อกก่อนให้ handler ตั้งสถานะ mouse look ใหม่ แล้วค่อยล็อกกล้องตัวที่ใช้อยู่ตอนนี้อีกที
            UnlockLook();

            // ออกก่อนเข้า กล้องจะได้ไม่ถูกสองโหมดแย่งกันตั้งค่า
            foreach (KeyValuePair<GameMode, ICameraModeHandler> pair in modeHandlers)
            {
                if (!gameMode.Contains(pair.Key) && activeModes.Remove(pair.Key)) pair.Value.OnExit();
            }
            foreach (KeyValuePair<GameMode, ICameraModeHandler> pair in modeHandlers)
            {
                if (gameMode.Contains(pair.Key) && activeModes.Add(pair.Key)) pair.Value.OnEnter();
            }

            // เมาส์ใช้จัดของ จึงหยุดหันกล้องตัวที่ใช้อยู่ตลอดที่เปิดหน้าต่างกระเป๋า ไม่ว่าจะเปิดทับโหมดไหน
            if (gameMode.Contains(GameMode.Inventory)) lookLocks.Add(this);
            else lookLocks.Remove(this);

            if (lookLocks.Count > 0) ApplyLookLock();
        }
    }
}
