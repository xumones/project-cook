namespace ProjectCook.Dialogue
{
    /// <summary>รับ Action ID ที่ผู้เล่นเลือกจากตัวเลือกคำตอบ</summary>
    public interface IDialogueActionHandler
    {
        void HandleAction(string actionId);
    }
}
