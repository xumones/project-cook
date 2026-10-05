using UnityEngine;
using ProjectCook.Items;

namespace ProjectCook.Quests
{
    /// <summary>เป้าหมายของเควสเนื้อเรื่องหนึ่งชนิด ต้องไม่เก็บสถานะของตัวเอง (asset หนึ่งตัวใช้ร่วมกันได้หลายเควส)</summary>
    public abstract class QuestObjectiveSO : ScriptableObject, IQuestObjective
    {
        public virtual int Required => 1;

        public abstract string Describe();

        public virtual int OnItemCollected(ItemInstance item) => 0;

        public virtual int OnWorldEvent(string eventId) => 0;

        public virtual int OnTalkedTo(string speakerId) => 0;
    }
}
