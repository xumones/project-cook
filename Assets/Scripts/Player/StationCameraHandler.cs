using System;
using Unity.Cinemachine;

namespace ProjectCook.Player
{
    /// <summary>ปฏิกิริยากล้องตอนเข้า/ออกโหมด Station: สลับ priority มาที่กล้องสถานี คืนมุมมองผู้เล่นเดิมตอนออก และซ่อนของในมือระหว่างอยู่สถานี</summary>
    public class StationCameraHandler : ICameraModeHandler
    {
        private readonly CinemachineCamera playerCamera;
        private readonly int activePriority;
        private readonly int inactivePriority;
        private readonly Func<CinemachineCamera> getStationCamera;
        private readonly Action<bool> setHeldItemVisible;

        private CinemachineCamera activeStationCamera;
        private float savedPlayerPan;
        private float savedPlayerTilt;

        public StationCameraHandler(CinemachineCamera playerCamera, int activePriority, int inactivePriority,
            Func<CinemachineCamera> getStationCamera, Action<bool> setHeldItemVisible)
        {
            this.playerCamera = playerCamera;
            this.activePriority = activePriority;
            this.inactivePriority = inactivePriority;
            this.getStationCamera = getStationCamera;
            this.setHeldItemVisible = setHeldItemVisible;
        }

        public void OnEnter()
        {
            CinemachineCamera stationCamera = getStationCamera();
            if (stationCamera == null) return;

            // บันทึกมุมมองเดิมของผู้เล่น + ปิด mouse look ของกล้องผู้เล่นชั่วคราว
            if (playerCamera != null)
            {
                var playerPanTilt = playerCamera.GetComponent<CinemachinePanTilt>();
                if (playerPanTilt != null)
                {
                    savedPlayerPan = playerPanTilt.PanAxis.Value;
                    savedPlayerTilt = playerPanTilt.TiltAxis.Value;
                }
                CameraManager.SetLookInput(playerCamera, false);
            }

            activeStationCamera = stationCamera;

            // รีเซ็ตมุมมอง Pan / Tilt ของกล้องสถานี (ถ้ามี)
            var panTilt = activeStationCamera.GetComponent<CinemachinePanTilt>();
            if (panTilt != null)
            {
                panTilt.PanAxis.Value = 0f;
                panTilt.TiltAxis.Value = 40f;
            }
            CameraManager.SetLookInput(activeStationCamera, true);

            activeStationCamera.Priority = activePriority;
            if (playerCamera != null)
            {
                playerCamera.Priority = inactivePriority;
            }

            setHeldItemVisible(false);
        }

        public void OnExit()
        {
            // คืนมุมมองเดิมของผู้เล่น + เปิด mouse look กลับ
            if (playerCamera != null)
            {
                var playerPanTilt = playerCamera.GetComponent<CinemachinePanTilt>();
                if (playerPanTilt != null)
                {
                    playerPanTilt.PanAxis.Value = savedPlayerPan;
                    playerPanTilt.TiltAxis.Value = savedPlayerTilt;
                }
                CameraManager.SetLookInput(playerCamera, true);
                playerCamera.Priority = activePriority;
            }

            if (activeStationCamera != null)
            {
                CameraManager.SetLookInput(activeStationCamera, false);
                activeStationCamera.Priority = inactivePriority;
                activeStationCamera = null;
            }

            setHeldItemVisible(true);
        }
    }
}
