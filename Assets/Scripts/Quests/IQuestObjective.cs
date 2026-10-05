using System.Collections.Generic;
using ProjectCook.Cooking;
using ProjectCook.Items;

namespace ProjectCook.Quests
{
    /// <summary>เป้าหมายหนึ่งข้อของเควส ไม่เก็บความคืบหน้าเอง ActiveQuest เป็นคนนับ</summary>
    public interface IQuestObjective
    {
        /// <summary>จำนวนครั้งที่ต้องทำจนครบ</summary>
        int Required { get; }

        /// <summary>สิ่งที่ต้องทำแบบสั้น ใช้เขียนบนโน้ต</summary>
        string Describe();

        /// <summary>ความคืบหน้าที่ได้จากข่าวนี้ 0 = ไม่เกี่ยวกับเป้าหมายนี้</summary>
        int OnItemCollected(ItemInstance item);

        int OnWorldEvent(string eventId);

        int OnTalkedTo(string speakerId);
    }

    /// <summary>เป้าหมายที่ต้องเอาอาหารไปส่ง ผู้รับเป็นคนเรียก QuestLog.Deliver ตอนผู้เล่นยื่นจาน</summary>
    public interface IDeliveryObjective : IQuestObjective
    {
        /// <summary>รหัสผู้รับ ว่าง = ผู้ให้เควส</summary>
        string RecipientId { get; }

        DishScore Evaluate(IReadOnlyList<IngredientSnapshot> dish);
    }
}
