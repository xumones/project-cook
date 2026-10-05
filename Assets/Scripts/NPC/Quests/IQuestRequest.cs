using UnityEngine.Localization;
using ProjectCook.Core;
using ProjectCook.Cooking;

namespace ProjectCook.NPC
{
    /// <summary>สิ่งที่ NPC ขอให้ทำหนึ่งรายการ ไม่ว่าเป็นเควสเนื้อเรื่องหรือเควสสุ่ม NPCQuestGiver จึงจัดการได้ทางเดียว</summary>
    public interface IQuestRequest
    {
        int Reward { get; }

        LocalizedString OfferText { get; }

        LocalizedString ReminderText { get; }

        /// <summary>สิ่งที่ขอแบบสั้น ใช้เขียนบนโน้ต เช่น "Egg, Bacon"</summary>
        string Describe();

        DishScore Evaluate(ServingFoodContainer dish);

        void Consume(ServingFoodContainer dish);

        /// <summary>คำตอบตามเกรดที่ส่ง F = ไม่ผ่าน</summary>
        LocalizedString ResultText(QualityGrade grade);

        /// <summary>หลังส่งผ่าน เช่น เควสเนื้อเรื่องจดว่าจบแล้ว</summary>
        void OnCompleted();
    }
}
