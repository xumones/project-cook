namespace ProjectCook.Dialogue
{
    /// <summary>ข่าว: บทสนทนากับผู้พูดคนนี้ปิดลงแล้ว ส่งก่อนออกจากโหมด Dialogue</summary>
    public readonly struct DialogueEnded
    {
        public readonly string SpeakerId;

        public DialogueEnded(string speakerId)
        {
            SpeakerId = speakerId;
        }
    }
}
