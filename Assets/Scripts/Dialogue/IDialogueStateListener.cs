namespace ProjectCook.Dialogue
{
    /// <summary>เตรียม flag ที่บทสนทนาต้องอ่าน ถูกเรียกตอนเริ่มคุยและทุกครั้งหลัง action เพื่อให้ flag สดเสมอ</summary>
    public interface IDialogueStateListener
    {
        void RefreshDialogueState();
    }
}
