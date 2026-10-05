using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Core;
using ProjectCook.Items;

namespace ProjectCook.Quests
{
    /// <summary>เควสเนื้อเรื่องหนึ่งเรื่อง ทำครั้งเดียว: เป้าหมาย รางวัล ประโยคแต่ละจังหวะ และสิ่งที่ทำให้เควสเริ่มเอง</summary>
    [CreateAssetMenu(fileName = "Quest_", menuName = "ProjectCook/Quests/Quest")]
    public class QuestSO : ScriptableObject, IQuestDefinition
    {
        [Tooltip("รหัสเควส ใช้จำว่าจบแล้ว ต้องไม่ซ้ำกันทั้งเกม")]
        [SerializeField] private string questId;

        [Tooltip("ต้องครบทุกข้อเควสถึงจะจบ ทำข้อไหนก่อนก็ได้")]
        [SerializeField] private List<QuestObjectiveSO> objectives = new List<QuestObjectiveSO>();

        [Tooltip("ค่าตอบแทนตอนเควสจบ")]
        [Min(0)]
        [SerializeField] private int reward;

        [Header("Self Start")]
        [Tooltip("หยิบไอเทมชนิดนี้แล้วเควสเริ่มเอง ต้องใส่เควสนี้ไว้ใน Triggered Quests ของ QuestLog ด้วย")]
        [SerializeField] private ItemDefinitionSO startItem;

        [Tooltip("ไปถึง WorldEventTrigger รหัสนี้แล้วเควสเริ่มเอง ต้องใส่เควสนี้ไว้ใน Triggered Quests ของ QuestLog ด้วย")]
        [SerializeField] private string startEvent;

        [Header("Lines")]
        [Tooltip("ตอนผู้เล่นรับเควส")]
        [SerializeField] private LocalizedString offerText;

        [Tooltip("ตอนถามซ้ำระหว่างยังไม่จบ")]
        [SerializeField] private LocalizedString reminderText;

        [Tooltip("ส่งอาหารไม่ผ่าน (เกรด F) เควสยังค้างอยู่")]
        [SerializeField] private LocalizedString failText;

        [Tooltip("ส่งอาหารผ่าน (เกรด D ถึง A)")]
        [SerializeField] private LocalizedString passText;

        [Tooltip("ส่งอาหารได้เกรด S")]
        [SerializeField] private LocalizedString excellentText;

        public string QuestId => questId;
        public int Reward => reward;
        public IReadOnlyList<IQuestObjective> Objectives => objectives;
        public ItemDefinitionSO StartItem => startItem;
        public string StartEvent => startEvent;
        public LocalizedString OfferText => offerText;
        public LocalizedString ReminderText => reminderText;

        public LocalizedString ResultText(QualityGrade grade) => grade switch
        {
            QualityGrade.F => failText,
            QualityGrade.S => excellentText,
            _ => passText
        };
    }
}
