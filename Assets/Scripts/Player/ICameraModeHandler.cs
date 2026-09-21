namespace ProjectCook.Player
{
    /// <summary>ปฏิกิริยาของกล้องต่อโหมดเกมโหมดหนึ่ง แยกออกจาก CameraManager เพื่อเพิ่มโหมดใหม่ได้โดยไม่ต้องแก้ dispatch เดิม</summary>
    public interface ICameraModeHandler
    {
        void OnEnter();
        void OnExit();
    }
}
