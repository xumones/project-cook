namespace ProjectCook.Quests
{
    /// <summary>ข่าว: ผู้เล่นรับหรือเริ่มเควสนี้ รหัสว่าง = เควสทำซ้ำได้ (เช่นเควสสุ่มของ NPC)</summary>
    public readonly struct QuestStarted
    {
        public readonly string QuestId;

        public QuestStarted(string questId)
        {
            QuestId = questId;
        }
    }
}
