namespace ProjectCook.Core
{
    public enum QuestState
    {
        NotStarted,
        Active,
        Completed
    }

    /// <summary>สถานะของเควสแต่ละตัว เก็บลง GameFlagsManager เพื่อให้ระบบเซฟมีถังเดียว แต่ซ่อนรูปแบบ key ไว้ที่นี่</summary>
    public static class QuestLog
    {
        public static QuestState GetState(string questId)
        {
            GameFlagsManager flags = GameFlagsManager.Instance;
            return flags != null ? (QuestState)flags.GetFlag(Key(questId)) : QuestState.NotStarted;
        }

        public static void SetState(string questId, QuestState state)
        {
            GameFlagsManager.Instance?.SetFlag(Key(questId), (int)state);
        }

        private static string Key(string questId) => $"quest_{questId}";
    }
}
