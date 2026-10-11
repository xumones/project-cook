namespace ProjectCook.Quests
{
    /// <summary>ข่าว: เควสนี้ครบทุกเป้าหมายและจ่ายรางวัลแล้ว รหัสว่าง = เควสทำซ้ำได้</summary>
    public readonly struct QuestCompleted
    {
        public readonly string QuestId;

        public QuestCompleted(string questId)
        {
            QuestId = questId;
        }
    }
}
