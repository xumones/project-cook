using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Core;
using ProjectCook.Cooking;
using ProjectCook.Dialogue;
using ProjectCook.Inventory;
using ProjectCook.Items;

namespace ProjectCook.NPC.Capabilities
{
    /// <summary>ให้เควสทีละอันต่อ NPC: เควสเนื้อเรื่องตามลำดับก่อน หมดแล้วสุ่มเควสจากวัตถุดิบ และรับจานที่ผู้เล่นถือมาส่ง</summary>
    public class NPCQuestGiver : NPCCapability
    {
        [Tooltip("เควสเนื้อเรื่องตามลำดับ ทำครั้งเดียว ตัวแรกที่ยังไม่จบคือเควสที่เสนอ")]
        [SerializeField] private List<QuestSO> storyQuests = new List<QuestSO>();

        [Tooltip("วัตถุดิบที่ NPC นี้สั่งได้ในเควสสุ่มหลังเนื้อเรื่องจบ ว่าง = ไม่มีเควสสุ่ม")]
        [SerializeField] private List<IngredientDataSO> orderIngredients = new List<IngredientDataSO>();

        [Header("Labels")]
        [SerializeField] private LocalizedString askLabel;
        [SerializeField] private LocalizedString reminderLabel;
        [SerializeField] private LocalizedString deliverLabel;

        // เควสที่ให้อยู่ตอนนี้ null = ว่าง ขอเควสใหม่ได้
        private IQuestRequest active;

        public override void BuildOptions(List<DialogueOption> options)
        {
            if (active == null)
            {
                IQuestRequest next = NextRequest();
                if (next != null)
                {
                    options.Add(new DialogueOption { Label = askLabel, Response = next.OfferText, OnSelected = () => active = next });
                }
                return;
            }

            options.Add(new DialogueOption { Label = reminderLabel, Response = active.ReminderText });

            if (EquipmentManager.TryGetHeld(out ServingFoodContainer dish) && dish.HasContents)
            {
                // คำตอบขึ้นกับผลประเมิน จึงตั้งตอนกดเลือก
                var option = new DialogueOption { Label = deliverLabel };
                option.OnSelected = () => option.Response = Deliver(dish);
                options.Add(option);
            }
        }

        private IQuestRequest NextRequest()
        {
            GameProgress progress = GameProgress.Instance;
            foreach (QuestSO quest in storyQuests)
            {
                if (quest != null && !progress.IsQuestCompleted(quest.QuestId)) return quest;
            }

            // ขอได้เฉพาะเควสที่เงินตอนนี้พอซื้อวัตถุดิบ
            return PlayerWallet.Instance != null
                ? IngredientOrder.TryCreate(orderIngredients, PlayerWallet.Instance.Money, Npc.Profile)
                : null;
        }

        // ไม่ผ่าน = จานยังอยู่กับผู้เล่น แก้แล้วส่งใหม่ได้
        private LocalizedString Deliver(ServingFoodContainer dish)
        {
            IQuestRequest request = active;
            QualityGrade grade = request.Evaluate(dish).Grade;
            if (grade == QualityGrade.F) return request.ResultText(grade);

            request.Consume(dish);
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.Earn(request.Reward);
            }
            request.OnCompleted();
            active = null;
            return request.ResultText(grade);
        }
    }
}
