using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;
using ProjectCook.Cooking;
using ProjectCook.Dialogue;
using ProjectCook.Items;

namespace ProjectCook.NPC
{
    /// <summary>ไล่เควสของ NPC ทีละตัว: ให้ผู้เล่นรับ (ปลดล็อกวัตถุดิบ) และรับของที่ส่งมา บทสนทนาเห็นแค่ flag กลางไม่กี่ตัว</summary>
    public class NPCQuestGiver : MonoBehaviour, IDialogueActionHandler, IDialogueStateListener, IDialogueTextProvider
    {
        [Tooltip("เควสตามลำดับ ตัวแรกที่ยังไม่เสร็จคือเควสปัจจุบัน")]
        [SerializeField] private List<QuestSO> quests = new List<QuestSO>();

        private const string AcceptActionId = "ACCEPT_QUEST";
        private const string DeliverActionId = "DELIVER_QUEST";

        // flag ที่ JSON อ้างได้ ตอบแค่ว่า "ตอนนี้ทำอะไรได้" ไม่บอกว่าเรื่องอยู่ขั้นไหน จำนวนจึงไม่โตตามเนื้อหา
        public const string QuestAvailableFlag = "quest_available";
        public const string QuestActiveFlag = "quest_active";
        public const string QuestDeliverableFlag = "quest_deliverable";

        private readonly List<IngredientInstance> ingredientBuffer = new List<IngredientInstance>();

        // เควสที่เพิ่งส่งเสร็จ ไว้ให้ข้อความตอบรับอ้างถึงได้ แม้เควสปัจจุบันจะเลื่อนไปตัวถัดไปแล้ว
        private QuestSO lastCompleted;
        private RecipeOutcome lastOutcome;

        /// <summary>Flag ที่บอกว่าผู้เล่นขอวัตถุดิบชนิดนี้ได้แล้ว</summary>
        public static string UnlockedFlag(string ingredientId) => $"unlocked_{ingredientId}";

        private QuestSO CurrentQuest
        {
            get
            {
                for (int i = 0; i < quests.Count; i++)
                {
                    if (quests[i] != null && QuestLog.GetState(quests[i].QuestId) != QuestState.Completed)
                    {
                        return quests[i];
                    }
                }
                return null;
            }
        }

        public void RefreshDialogueState()
        {
            GameFlagsManager flags = GameFlagsManager.Instance;
            if (flags == null) return;

            QuestSO quest = CurrentQuest;
            QuestState state = quest != null ? QuestLog.GetState(quest.QuestId) : QuestState.Completed;

            flags.SetFlag(QuestAvailableFlag, state == QuestState.NotStarted ? 1 : 0);
            flags.SetFlag(QuestActiveFlag, state == QuestState.Active ? 1 : 0);

            // ส่งได้เมื่อของที่สูตรต้องการมีครบและผ่านขั้นต่ำทุกชิ้น ของเกินไม่ขัด (ต่างกันแค่คำตอบ NPC)
            bool deliverable = state == QuestState.Active && TryEvaluateHeldDish(quest, out RecipeMatch match) && match.IsDeliverable;
            flags.SetFlag(QuestDeliverableFlag, deliverable ? 1 : 0);
        }

        private bool TryEvaluateHeldDish(QuestSO quest, out RecipeMatch match)
        {
            match = default;
            if (quest.Recipe == null || !EquipmentManager.TryGetHeld(out ServingFoodContainer dish) || !dish.HasContents) return false;

            dish.GetIngredients(ingredientBuffer);
            match = quest.Recipe.Evaluate(ingredientBuffer);
            return true;
        }

        public void HandleAction(string actionId)
        {
            QuestSO quest = CurrentQuest;
            if (quest == null) return;

            switch (actionId)
            {
                case AcceptActionId:
                    Accept(quest);
                    break;
                case DeliverActionId:
                    Deliver(quest);
                    break;
            }
        }

        private void Accept(QuestSO quest)
        {
            if (QuestLog.GetState(quest.QuestId) != QuestState.NotStarted) return;

            QuestLog.SetState(quest.QuestId, QuestState.Active);

            GameFlagsManager flags = GameFlagsManager.Instance;
            if (flags == null || quest.Recipe == null) return;

            foreach (RecipeSO.Requirement requirement in quest.Recipe.Requirements)
            {
                if (requirement.ingredient != null)
                {
                    flags.SetFlag(UnlockedFlag(requirement.ingredient.IngredientId), 1);
                }
            }
        }

        private void Deliver(QuestSO quest)
        {
            if (QuestLog.GetState(quest.QuestId) != QuestState.Active) return;

            // เช็คซ้ำแม้ตัวเลือกจะถูกซ่อนด้วย flag แล้ว กัน JSON ที่ลืมใส่เงื่อนไข
            if (!TryEvaluateHeldDish(quest, out RecipeMatch match) || !match.IsDeliverable)
            {
                Debug.Log("[NPCQuestGiver] ของในจานยังไม่ครบตามสูตร ส่งมอบไม่ได้");
                return;
            }

            lastOutcome = match.Outcome;
            lastCompleted = quest;

            EquipmentManager.TryGetHeld(out ServingFoodContainer dish);
            dish.ConsumeAll();
            GameFlagsManager.Instance?.SetDishCooked(quest.Recipe.RecipeId);
            QuestLog.SetState(quest.QuestId, QuestState.Completed);
        }

        public string ResolveText(string text)
        {
            QuestSO quest = CurrentQuest;
            if (quest != null)
            {
                text = text.Replace("{quest_offer}", quest.OfferText)
                           .Replace("{quest_reminder}", quest.ReminderText);
            }

            if (lastCompleted != null)
            {
                text = text.Replace("{quest_result}", lastCompleted.ResultText(lastOutcome));
            }
            return text;
        }
    }
}
