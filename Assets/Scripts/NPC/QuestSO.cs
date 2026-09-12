using UnityEngine;
using ProjectCook.Cooking;

namespace ProjectCook.NPC
{
    /// <summary>คำขอหนึ่งรายการของ NPC: ต้องส่งอะไร และ NPC พูดว่าอะไรในแต่ละจังหวะ</summary>
    [CreateAssetMenu(fileName = "Quest_", menuName = "ProjectCook/NPC/Quest")]
    public class QuestSO : ScriptableObject
    {
        [Tooltip("รหัสเควส ใช้เก็บสถานะใน QuestLog ต้องไม่ซ้ำกันทั้งเกม")]
        [SerializeField] private string questId;

        [Tooltip("สูตรที่ต้องส่ง วัตถุดิบในสูตรจะถูกปลดล็อกให้ขอได้ตอนรับเควส")]
        [SerializeField] private RecipeSO recipe;

        [Header("Lines")]
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
        public string OfferText => offerText;
        public string ReminderText => reminderText;

        public string ResultText(RecipeOutcome outcome) =>
            outcome == RecipeOutcome.Extra ? extraText : exactText;
    }
}
