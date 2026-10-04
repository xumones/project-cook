using System.Collections.Generic;

namespace ProjectCook.Core
{
    /// <summary>ความคืบหน้าถาวรของผู้เล่น เก็บเป็นถังเดียวเพื่อให้ save ทีเดียว แต่เปิดให้ใช้ผ่าน API แบบ typed เท่านั้น</summary>
    public class GameProgress : PersistentSingleton<GameProgress>
    {
        private readonly Dictionary<string, int> values = new Dictionary<string, int>();

        /// <summary>เควสเนื้อเรื่องที่ทำครั้งเดียว จบแล้วหรือยัง</summary>
        public bool IsQuestCompleted(string questId) => Get(QuestKey(questId)) > 0;

        public void CompleteQuest(string questId) => Set(QuestKey(questId), 1);

        private int Get(string key) => values.TryGetValue(key, out int value) ? value : 0;

        private void Set(string key, int value)
        {
            if (string.IsNullOrEmpty(key)) return;
            values[key] = value;
        }

        private static string QuestKey(string questId) => $"quest_{questId}";
    }
}
