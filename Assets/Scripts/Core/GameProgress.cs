using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Core
{
    public enum QuestState
    {
        NotStarted,
        Active,
        Completed
    }

    /// <summary>ความคืบหน้าถาวรของผู้เล่น เก็บเป็นถังเดียวเพื่อให้ save ทีเดียว แต่เปิดให้ใช้ผ่าน API แบบ typed เท่านั้น</summary>
    public class GameProgress : PersistentSingleton<GameProgress>
    {
        private readonly Dictionary<string, int> values = new Dictionary<string, int>();

        public QuestState GetQuestState(string questId) => (QuestState)Get(QuestKey(questId));

        public void SetQuestState(string questId, QuestState state) => Set(QuestKey(questId), (int)state);

        /// <summary>เกรดสูงสุดที่เคยส่งได้ ยังไม่เคยส่ง = F</summary>
        public QualityGrade GetQuestGrade(string questId) => (QualityGrade)Get(GradeKey(questId));

        /// <summary>บันทึกเฉพาะเมื่อดีกว่าเดิม ส่งซ้ำได้แย่กว่าไม่ทำให้เกรดตก</summary>
        public void SetQuestGrade(string questId, QualityGrade grade)
        {
            if (grade > GetQuestGrade(questId)) Set(GradeKey(questId), (int)grade);
        }

        /// <summary>ไอเทมที่ปลดล็อกแล้วถึงจะมีขายในร้าน</summary>
        public bool IsItemUnlocked(string itemId) => Get(ItemKey(itemId)) > 0;

        public void UnlockItem(string itemId) => Set(ItemKey(itemId), 1);

        private int Get(string key) => values.TryGetValue(key, out int value) ? value : 0;

        private void Set(string key, int value)
        {
            if (string.IsNullOrEmpty(key)) return;
            values[key] = value;
        }

        private static string QuestKey(string questId) => $"quest_{questId}";
        private static string GradeKey(string questId) => $"quest_{questId}_grade";
        private static string ItemKey(string itemId) => $"item_{itemId}";
    }
}
