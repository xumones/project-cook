namespace ProjectCook.Dialogue
{
    /// <summary>ข่าว: ผู้เล่นเริ่มคุยกับผู้พูดคนนี้</summary>
    public readonly struct DialogueStarted
    {
        public readonly string SpeakerId;

        public DialogueStarted(string speakerId)
        {
            SpeakerId = speakerId;
        }
    }
}
