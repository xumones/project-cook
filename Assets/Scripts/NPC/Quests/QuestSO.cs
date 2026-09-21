using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Core;

namespace ProjectCook.NPC
{
    /// <summary>คำขอหนึ่งรายการของ NPC: เงื่อนไขอะไร รับได้เมื่อไหร่ และ NPC พูดว่าอะไรในแต่ละจังหวะ</summary>
    [CreateAssetMenu(fileName = "Quest_", menuName = "ProjectCook/NPC/Quest")]
    public class QuestSO : ScriptableObject
    {
        [Tooltip("รหัสเควส ใช้เก็บสถานะใน GameProgress ต้องไม่ซ้ำกันทั้งเกม")]
        [SerializeField] private string questId;

        [SerializeField] private QuestObjectiveSO objective;

        [Tooltip("เควสที่ต้องจบก่อนถึงจะรับอันนี้ได้ จะเป็นของ NPC ไหนก็ได้ เว้นว่าง = รับได้ทันที")]
        [SerializeField] private List<QuestSO> prerequisites = new List<QuestSO>();

        [Header("Lines")]
        [Tooltip("ตอนยังรับไม่ได้เพราะเควสก่อนหน้ายังไม่จบ")]
        [SerializeField] private LocalizedString lockedText;

        [Tooltip("ตอนผู้เล่นรับเควส")]
        [SerializeField] private LocalizedString offerText;

        [Tooltip("ตอนถามซ้ำระหว่างยังไม่ส่ง")]
        [SerializeField] private LocalizedString reminderText;

        [Tooltip("คำตอบตามเกรดที่ส่งได้ เรียง F, D, C, B, A, S ช่องที่ว่างจะใช้ของเกรดต่ำกว่าที่ใกล้สุด (ไม่มีจึงไล่ขึ้น)")]
        [SerializeField] private LocalizedString[] resultTexts = new LocalizedString[6];

        public string QuestId => questId;
        public QuestObjectiveSO Objective => objective;
        public LocalizedString LockedText => lockedText;
        public LocalizedString OfferText => offerText;
        public LocalizedString ReminderText => reminderText;

        public LocalizedString ResultText(QualityGrade grade)
        {
            for (int i = (int)grade; i >= 0; i--)
            {
                if (HasResultText(i)) return resultTexts[i];
            }
            for (int i = (int)grade + 1; i < resultTexts.Length; i++)
            {
                if (HasResultText(i)) return resultTexts[i];
            }
            return null;
        }

        private bool HasResultText(int index) =>
            index < resultTexts.Length && resultTexts[index] != null && !resultTexts[index].IsEmpty;

        /// <summary>เควสก่อนหน้าจบครบแล้วหรือยัง</summary>
        public bool PrerequisitesMet
        {
            get
            {
                for (int i = 0; i < prerequisites.Count; i++)
                {
                    if (prerequisites[i] != null && GameProgress.Instance.GetQuestState(prerequisites[i].QuestId) != QuestState.Completed)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
