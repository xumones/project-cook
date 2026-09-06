using System;
using UnityEngine;
using Unity.Cinemachine;
using ProjectCook.Core;

namespace ProjectCook.CameraSystem
{
    /// <summary>
    /// Singleton สำหรับควบคุม State กลางของกล้องทั้งเกม และส่ง Event แจ้งระบบอื่นๆ
    /// </summary>
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

        private CameraState currentState = CameraState.FirstPerson;
        private CinemachineCamera activeStationCamera;

        // บันทึกมุมมองเดิมของผู้เล่นก่อนเข้าโหมดทำอาหาร
        private float savedPlayerPan = 0f;
        private float savedPlayerTilt = 0f;

        // บันทึกขอบเขตการหมุนเดิมของกล้องผู้เล่นก่อนเข้าโหมดบทสนทนา เพื่อคืนค่าตอนจบ
        private Vector2 savedDialoguePanRange;
        private Vector2 savedDialogueTiltRange;
        private bool savedDialoguePanWrap;
        private bool dialogueRangeSaved;

        public CameraState CurrentState => currentState;
        public event Action<CameraState> OnCameraStateChanged;

        private void Start()
        {
            if (playerCamera != null)
            {
                playerCamera.Priority = activePriority;
            }
        }

        /// <summary>
        /// สลับไปที่โหมดทำอาหาร พร้อมสลับกล้อง Cinemachine ไปยังสถานีทำอาหารนั้นๆ
        /// </summary>
        public void SwitchToCooking(CinemachineCamera stationCamera)
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

            // 2. รีเซ็ตมุมมอง Pan / Tilt ของกล้องสถานีทำอาหาร
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

            currentState = CameraState.Cooking;
            OnCameraStateChanged?.Invoke(currentState);
        }

        /// <summary>
        /// สลับกลับสู่โหมดมุมมองผู้เล่นปกติ (First Person)
        /// </summary>
        public void SwitchToFirstPerson()
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

            currentState = CameraState.FirstPerson;
            OnCameraStateChanged?.Invoke(currentState);
        }

        /// <summary>
        /// เข้าสู่โหมดบทสนทนา: กล้องผู้เล่นยังหมุนตามเมาส์ได้เหมือนเดิม แต่ถูกจำกัดมุมกวาดไว้รอบทิศที่กล้องกำลังเล็งอยู่ตอนเริ่มคุย
        /// (ใช้กล้องผู้เล่นตัวเดิม ไม่สลับ Virtual Camera เหมือนโหมดทำอาหาร)
        /// </summary>
        public void SwitchToDialogue()
        {
            if (playerCamera == null)
            {
                currentState = CameraState.Dialogue;
                OnCameraStateChanged?.Invoke(currentState);
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

            currentState = CameraState.Dialogue;
            OnCameraStateChanged?.Invoke(currentState);
        }

        /// <summary>
        /// ออกจากโหมดบทสนทนา คืนขอบเขตการหมุนกล้องผู้เล่นกลับเป็นค่าเดิม
        /// </summary>
        public void ExitDialogue()
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

            currentState = CameraState.FirstPerson;
            OnCameraStateChanged?.Invoke(currentState);
        }
    }
}
