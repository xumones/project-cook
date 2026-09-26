using System;
using System.Collections.Generic;
using UnityEngine;
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
        private readonly Dictionary<CinemachinePanTilt, Vector2> initialAngles = new Dictionary<CinemachinePanTilt, Vector2>();
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

            Activate(stationCamera);
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

            Deactivate();
            setHeldItemVisible(true);
        }

        /// <summary>สลับกล้องระหว่างยังอยู่ในสถานี (เช่น จากมุมรวมของเตาไปมุมกระทะ)</summary>
        public void SwitchTo(CinemachineCamera camera)
        {
            if (camera == null || camera == activeStationCamera) return;

            Deactivate();
            Activate(camera);
        }

        private void Activate(CinemachineCamera camera)
        {
            activeStationCamera = camera;

            // คืนมุม Pan/Tilt เป็นค่าที่ตั้งไว้ใน Scene ซึ่งจำไว้ตอนเปิดใช้กล้องครั้งแรก
            var panTilt = camera.GetComponent<CinemachinePanTilt>();
            if (panTilt != null)
            {
                if (!initialAngles.TryGetValue(panTilt, out Vector2 angles))
                {
                    angles = new Vector2(panTilt.PanAxis.Value, panTilt.TiltAxis.Value);
                    initialAngles[panTilt] = angles;
                }
                panTilt.PanAxis.Value = angles.x;
                panTilt.TiltAxis.Value = angles.y;
            }

            CameraManager.SetLookInput(camera, true);
            camera.Priority = activePriority;
        }

        private void Deactivate()
        {
            if (activeStationCamera == null) return;

            CameraManager.SetLookInput(activeStationCamera, false);
            activeStationCamera.Priority = inactivePriority;
            activeStationCamera = null;
        }
    }
}
