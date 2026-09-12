namespace ProjectCook.Dialogue
{
    /// <summary>คู่สนทนาที่ DialogueManager คุยด้วย ทำให้ระบบบทสนทนาไม่ต้องรู้จักคลาส NPC</summary>
    public interface IDialogueTarget : IDialogueActionHandler
    {
        /// <summary>ชื่อที่ขึ้นบนกล่องข้อความเมื่อโหนดไม่ได้ระบุชื่อผู้พูดเอง</summary>
        string DisplayName { get; }

        /// <summary>เติม placeholder ในข้อความก่อนแสดง คืนข้อความเดิมถ้าไม่มีอะไรให้แทน</summary>
        string ResolveText(string text);

        void OnDialogueEnded();
    }
}
