namespace ProjectCook.Quests
{
    /// <summary>ข่าว: เควสนี้ครบทุกเป้าหมายและจ่ายรางวัลแล้ว รหัสว่าง = เควสทำซ้ำได้</summary>
    public readonly struct QuestCompleted
    {
        public readonly string QuestId;

        /// <summary>รหัส NPC ที่ให้เควสนี้</summary>
        public readonly string GiverId;

        public QuestCompleted(string questId, string giverId)
        {
            QuestId = questId;
            GiverId = giverId;
        }
    }
}
