using System.Collections.Generic;
using UnityEngine.Localization;
using ProjectCook.Core;

namespace ProjectCook.Quests
{
    /// <summary>เนื้อหาของเควสหนึ่งเรื่อง ไม่ว่าจะเป็นเควสเนื้อเรื่องจาก asset หรือเควสสุ่มที่สร้างตอนเล่น</summary>
    public interface IQuestDefinition
    {
        /// <summary>รหัสเควสที่ทำครั้งเดียว ว่าง = ทำซ้ำได้ ไม่จดว่าจบ</summary>
        string QuestId { get; }

        int Reward { get; }

        /// <summary>ต้องครบทุกข้อเควสถึงจะจบ ทำข้อไหนก่อนก็ได้</summary>
        IReadOnlyList<IQuestObjective> Objectives { get; }

        LocalizedString OfferText { get; }

        LocalizedString ReminderText { get; }

        /// <summary>คำตอบตอนส่งอาหารตามเกรด F = ไม่ผ่าน</summary>
        LocalizedString ResultText(QualityGrade grade);

        /// <summary>ชนิดของเนื้อหาเควส ใช้เลือกผู้สร้างคืนตอนโหลด save</summary>
        string SaveKind { get; }

        /// <summary>ข้อมูลที่ต้องใช้สร้างเนื้อหาเควสนี้คืน</summary>
        string SaveDefinition();
    }
}
