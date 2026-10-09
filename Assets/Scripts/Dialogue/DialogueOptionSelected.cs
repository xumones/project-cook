namespace ProjectCook.Dialogue
{
    /// <summary>ข่าว: ผู้เล่นเลือกตัวเลือกในเมนูของผู้พูดคนนี้</summary>
    public readonly struct DialogueOptionSelected
    {
        public readonly string SpeakerId;

        public DialogueOptionSelected(string speakerId)
        {
            SpeakerId = speakerId;
        }
    }
}
