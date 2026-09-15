using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Cooking;
using ProjectCook.Core;

namespace ProjectCook.NPC
{
    /// <summary>คำขอหนึ่งรายการของ NPC: ต้องส่งอะไร รับได้เมื่อไหร่ และ NPC พูดว่าอะไรในแต่ละจังหวะ</summary>
    [CreateAssetMenu(fileName = "Quest_", menuName = "ProjectCook/NPC/Quest")]
    public class QuestSO : ScriptableObject
    {
        [Tooltip("รหัสเควส ใช้เก็บสถานะใน QuestLog ต้องไม่ซ้ำกันทั้งเกม")]
        [SerializeField] private string questId;

        [Tooltip("สูตรที่ต้องส่ง วัตถุดิบในสูตรจะถูกปลดล็อกให้ขอได้ตอนรับเควส")]
        [SerializeField] private RecipeSO recipe;

        [Tooltip("เควสที่ต้องจบก่อนถึงจะรับอันนี้ได้ จะเป็นของ NPC ไหนก็ได้ เว้นว่าง = รับได้ทันที")]
        [SerializeField] private List<QuestSO> prerequisites = new List<QuestSO>();

        [Header("Lines")]
        [TextArea(2, 4)] [Tooltip("ตอนยังรับไม่ได้เพราะเควสก่อนหน้ายังไม่จบ ({quest_locked})")]
        [SerializeField] private string lockedText;

        [TextArea(2, 4)] [Tooltip("ตอนผู้เล่นรับเควส ({quest_offer})")]
        [SerializeField] private string offerText;

        [TextArea(2, 4)] [Tooltip("ตอนถามซ้ำระหว่างยังไม่ส่ง ({quest_reminder})")]
        [SerializeField] private string reminderText;

        [TextArea(2, 4)] [Tooltip("ส่งครบพอดี ({quest_result})")]
        [SerializeField] private string exactText;

        [TextArea(2, 4)] [Tooltip("ส่งครบแต่มีของเกินมา ({quest_result})")]
        [SerializeField] private string extraText;

        public string QuestId => questId;
        public RecipeSO Recipe => recipe;
        public string LockedText => lockedText;
        public string OfferText => offerText;

        /// <summary>เควสก่อนหน้าจบครบแล้วหรือยัง</summary>
        public bool PrerequisitesMet
        {
            get
            {
                for (int i = 0; i < prerequisites.Count; i++)
                {
                    if (prerequisites[i] != null && QuestLog.GetState(prerequisites[i].QuestId) != QuestState.Completed)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        public string ReminderText => reminderText;

        public string ResultText(RecipeOutcome outcome) =>
            outcome == RecipeOutcome.Extra ? extraText : exactText;
    }
}
