using System;
using UnityEngine;
using Unity.Cinemachine;

namespace ProjectCook.CameraSystem
{
    /// <summary>
    /// Singleton สำหรับควบคุม State กลางของกล้องทั้งเกม และส่ง Event แจ้งระบบอื่นๆ
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [Header("Default Player Camera")]
        [Tooltip("Cinemachine Virtual Camera")]
        [SerializeField] private CinemachineCamera playerCamera;

        [Header("Priority Settings")]
        [SerializeField] private int activePriority = 1;
        [SerializeField] private int inactivePriority = 0;

        private CameraState currentState = CameraState.FirstPerson;
        private CinemachineCamera activeStationCamera;

        // บันทึกมุมมองเดิมของผู้เล่นก่อนเข้าโหมดทำอาหาร
        private float savedPlayerPan = 0f;
        private float savedPlayerTilt = 0f;

        public CameraState CurrentState => currentState;
        public event Action<CameraState> OnCameraStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

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
                activeStationCamera.Priority = inactivePriority;
                activeStationCamera = null;
            }

            currentState = CameraState.FirstPerson;
            OnCameraStateChanged?.Invoke(currentState);
        }
    }
}
