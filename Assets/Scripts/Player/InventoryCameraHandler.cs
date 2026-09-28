using Unity.Cinemachine;

namespace ProjectCook.Player
{
    /// <summary>ปฏิกิริยากล้องตอนเปิดหน้าต่างกระเป๋า: หยุด mouse look เพราะเมาส์ใช้จัดของแทน</summary>
    public class InventoryCameraHandler : ICameraModeHandler
    {
        private readonly CinemachineCamera playerCamera;

        public InventoryCameraHandler(CinemachineCamera playerCamera)
        {
            this.playerCamera = playerCamera;
        }

        public void OnEnter() => CameraManager.SetLookInput(playerCamera, false);

        public void OnExit() => CameraManager.SetLookInput(playerCamera, true);
    }
}
