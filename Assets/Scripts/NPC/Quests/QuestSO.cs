using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Core;
using ProjectCook.Cooking;

namespace ProjectCook.NPC
{
    /// <summary>เควสเนื้อเรื่องหนึ่งรายการ ทำครั้งเดียว: เงื่อนไขอะไร ได้เท่าไหร่ และ NPC พูดว่าอะไรในแต่ละจังหวะ</summary>
    [CreateAssetMenu(fileName = "Quest_", menuName = "ProjectCook/NPC/Quest")]
    public class QuestSO : ScriptableObject, IQuestRequest
    {
        [Tooltip("รหัสเควส ใช้จำใน GameProgress ว่าจบแล้ว ต้องไม่ซ้ำกันทั้งเกม")]
        [SerializeField] private string questId;

        [SerializeField] private QuestObjectiveSO objective;

        [Tooltip("ค่าตอบแทนตอนส่งผ่าน")]
        [Min(0)]
        [SerializeField] private int reward;

        [Header("Lines")]
        [Tooltip("ตอนผู้เล่นรับเควส")]
        [SerializeField] private LocalizedString offerText;

        [Tooltip("ตอนถามซ้ำระหว่างยังไม่ส่ง")]
        [SerializeField] private LocalizedString reminderText;

        [Tooltip("ส่งไม่ผ่าน (เกรด F) เควสยังค้างอยู่")]
        [SerializeField] private LocalizedString failText;

        [Tooltip("ส่งผ่าน (เกรด D ถึง A)")]
        [SerializeField] private LocalizedString passText;

        [Tooltip("ส่งได้เกรด S")]
        [SerializeField] private LocalizedString excellentText;

        public string QuestId => questId;
        public int Reward => reward;
        public LocalizedString OfferText => offerText;
        public LocalizedString ReminderText => reminderText;

        public DishScore Evaluate(ServingFoodContainer dish) => objective.Evaluate(dish);

        public void Consume(ServingFoodContainer dish) => objective.Consume(dish);

        public string Describe() => objective.Describe();

        public LocalizedString ResultText(QualityGrade grade) => grade switch
        {
            QualityGrade.F => failText,
            QualityGrade.S => excellentText,
            _ => passText
        };

        public void OnCompleted() => GameProgress.Instance.CompleteQuest(questId);
    }
}
