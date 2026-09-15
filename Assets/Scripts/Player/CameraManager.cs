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
        private CinemachineCamera activeStationCamera;

        // ของในมือยังลอยอยู่ที่ Hand Anchor ระหว่างอยู่สถานี จึงซ่อนทั้ง layer HeldItem จากกล้องหลักแทน
        private int heldItemLayer = -1;

        // บันทึกมุมมองเดิมของผู้เล่นก่อนเข้าสถานี
        private float savedPlayerPan = 0f;
        private float savedPlayerTilt = 0f;

        // บันทึกขอบเขตการหมุนเดิมของกล้องผู้เล่นก่อนเข้าโหมดบทสนทนา เพื่อคืนค่าตอนจบ
        private Vector2 savedDialoguePanRange;
        private Vector2 savedDialogueTiltRange;
        private bool savedDialoguePanWrap;
        private bool dialogueRangeSaved;

        /// <summary>กล้องของสถานีที่กำลังจะเข้าใช้งาน ตั้งก่อนสั่งเปลี่ยนโหมดเป็น Station</summary>
        public CinemachineCamera StationCamera { get; set; }

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

            if (previous == GameMode.Station) ExitStation();
            else if (previous == GameMode.Dialogue) ExitDialogue();

            if (mode == GameMode.Station) EnterStation(StationCamera);
            else if (mode == GameMode.Dialogue) EnterDialogue();
        }

        private void EnterStation(CinemachineCamera stationCamera)
        {
            if (stationCamera == null) return;

            // 1. บันทึกมุมมองเดิมของผู้เล่น + ปิด Input กล้องผู้เล่นชั่วคราวไม่ให้หมุนตามเมาส์
            if (playerCamera != null)
            {
                var playerPanTilt = playerCamera.GetComponent<CinemachinePanTilt>();
                if (playerPanTilt != null)
                {
                    savedPlayerPan = playerPanTilt.PanAxis.Value;
                    savedPlayerTilt = playerPanTilt.TiltAxis.Value;
                }

                var playerInput = playerCamera.GetComponent<CinemachineInputAxisController>();
                if (playerInput != null)
                {
                    playerInput.enabled = false;
                }
            }

            activeStationCamera = stationCamera;

            // 2. รีเซ็ตมุมมอง Pan / Tilt ของกล้องสถานี (ถ้ามี)
            var panTilt = activeStationCamera.GetComponent<CinemachinePanTilt>();
            if (panTilt != null)
            {
                panTilt.PanAxis.Value = 0f;
                panTilt.TiltAxis.Value = 40f;
            }

            // เปิด Input ของกล้องสถานีเฉพาะตอนกำลังใช้งานอยู่เท่านั้น ป้องกันไม่ให้มันรับ mouse look
            // แล้วหมุนตามผู้เล่นอยู่เงียบๆ ตลอดเวลาทั้งที่ยังไม่ได้เข้าสถานีนี้เลย
            var stationInput = activeStationCamera.GetComponent<CinemachineInputAxisController>();
            if (stationInput != null)
            {
                stationInput.enabled = true;
            }

            // 3. สลับ Priority กล้อง
            activeStationCamera.Priority = activePriority;
            if (playerCamera != null)
            {
                playerCamera.Priority = inactivePriority;
            }

            SetHeldItemVisible(false);
        }

        private void ExitStation()
        {
            // 4. คืนค่ามุมมองเดิมของผู้เล่น + เปิด Input กล้องผู้เล่นกลับมาทำงาน
            if (playerCamera != null)
            {
                var playerPanTilt = playerCamera.GetComponent<CinemachinePanTilt>();
                if (playerPanTilt != null)
                {
                    playerPanTilt.PanAxis.Value = savedPlayerPan;
                    playerPanTilt.TiltAxis.Value = savedPlayerTilt;
                }

                var playerInput = playerCamera.GetComponent<CinemachineInputAxisController>();
                if (playerInput != null)
                {
                    playerInput.enabled = true;
                }

                playerCamera.Priority = activePriority;
            }

            if (activeStationCamera != null)
            {
                var stationInput = activeStationCamera.GetComponent<CinemachineInputAxisController>();
                if (stationInput != null)
                {
                    stationInput.enabled = false;
                }

                activeStationCamera.Priority = inactivePriority;
                activeStationCamera = null;
            }

            SetHeldItemVisible(true);
        }

        /// <summary>กล้องยังหมุนตามเมาส์ได้ แต่จำกัดมุมไว้รอบทิศที่เล็งอยู่ตอนเริ่มคุย</summary>
        private void EnterDialogue()
        {
            if (playerCamera == null)
            {
                return;
            }

            var panTilt = playerCamera.GetComponent<CinemachinePanTilt>();
            if (panTilt != null)
            {
                // บันทึกขอบเขตเดิมไว้คืนตอนจบบทสนทนา (กันเคสเผลอเรียกซ้ำโดยไม่ได้ ExitDialogue ก่อน)
                if (!dialogueRangeSaved)
                {
                    savedDialoguePanRange = panTilt.PanAxis.Range;
                    savedDialogueTiltRange = panTilt.TiltAxis.Range;
                    savedDialoguePanWrap = panTilt.PanAxis.Wrap;
                    dialogueRangeSaved = true;
                }

                // ปิด Wrap ชั่วคราวแล้วจำกัดช่วงหมุนแนวนอนไว้รอบมุมที่กล้องกำลังเล็งอยู่ตอนนี้
                float currentPan = panTilt.PanAxis.Value;
                panTilt.PanAxis.Wrap = false;
                panTilt.PanAxis.Range = new Vector2(currentPan - dialoguePanRange * 0.5f, currentPan + dialoguePanRange * 0.5f);

                // จำกัดมุมก้ม/เงยไว้ในช่วงที่กำหนด
                panTilt.TiltAxis.Value = Mathf.Clamp(panTilt.TiltAxis.Value, dialogueTiltMin, dialogueTiltMax);
                panTilt.TiltAxis.Range = new Vector2(dialogueTiltMin, dialogueTiltMax);
            }

        }

        private void ExitDialogue()
        {
            if (playerCamera != null && dialogueRangeSaved)
            {
                var panTilt = playerCamera.GetComponent<CinemachinePanTilt>();
                if (panTilt != null)
                {
                    panTilt.PanAxis.Range = savedDialoguePanRange;
                    panTilt.PanAxis.Wrap = savedDialoguePanWrap;
                    panTilt.TiltAxis.Range = savedDialogueTiltRange;
                }
            }
            dialogueRangeSaved = false;

        }
    }
}
