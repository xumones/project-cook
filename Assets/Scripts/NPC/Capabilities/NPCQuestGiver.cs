using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Core;
using ProjectCook.Cooking;
using ProjectCook.Dialogue;
using ProjectCook.Inventory;

namespace ProjectCook.NPC.Capabilities
{
    /// <summary>ไล่เควสของ NPC ทีละตัวให้รับ และรับช่วงการส่งของจาก NPCReceiver ไปเป็นการส่งเควสที่ตรงที่สุด</summary>
    [RequireComponent(typeof(NPCReceiver))]
    public class NPCQuestGiver : NPCCapability, IDeliveryHandler
    {
        [Tooltip("เควสตามลำดับ ตัวแรกที่ยังไม่เสร็จคือเควสที่เสนอให้รับ")]
        [SerializeField] private List<QuestSO> quests = new List<QuestSO>();

        [Header("Labels")]
        [SerializeField] private LocalizedString askLabel;
        [SerializeField] private LocalizedString reminderLabel;

        private QuestSO CurrentQuest
        {
            get
            {
                GameProgress progress = GameProgress.Instance;
                for (int i = 0; i < quests.Count; i++)
                {
                    if (quests[i] != null && progress.GetQuestState(quests[i].QuestId) != QuestState.Completed)
                    {
                        return quests[i];
                    }
                }
                return null;
            }
        }

        public override void BuildOptions(List<DialogueOption> options)
        {
            QuestSO quest = CurrentQuest;
            if (quest == null) return;

            switch (GameProgress.Instance.GetQuestState(quest.QuestId))
            {
                case QuestState.NotStarted when quest.PrerequisitesMet:
                    options.Add(new DialogueOption { Label = askLabel, Response = quest.OfferText, OnSelected = () => Accept(quest) });
                    break;

                case QuestState.NotStarted:
                    options.Add(new DialogueOption { Label = askLabel, Response = quest.LockedText });
                    break;

                case QuestState.Active:
                    options.Add(new DialogueOption { Label = reminderLabel, Response = quest.ReminderText });
                    break;
            }
        }

        private void Accept(QuestSO quest)
        {
            GameProgress.Instance.SetQuestState(quest.QuestId, QuestState.Active);
            quest.Objective.OnAccepted();
        }

        // ส่งได้ทั้งเควสที่ทำอยู่และเควสที่จบแล้วแต่ยังไม่ S จานเป็นตัวบอกว่าส่งเควสไหน (คะแนนสูงสุด เท่ากันเอาที่ทำอยู่)
        public bool TryHandleDelivery(ServingFoodContainer dish, out LocalizedString response)
        {
            GameProgress progress = GameProgress.Instance;
            QuestSO best = null;
            DishScore bestScore = default;

            for (int i = 0; i < quests.Count; i++)
            {
                QuestSO quest = quests[i];
                if (quest == null || quest.Objective == null) continue;

                QuestState state = progress.GetQuestState(quest.QuestId);
                bool deliverable = state == QuestState.Active
                    || (state == QuestState.Completed && progress.GetQuestGrade(quest.QuestId) < QualityGrade.S);
                if (!deliverable) continue;

                DishScore score = quest.Objective.Evaluate(dish);
                if (best == null || score.Score > bestScore.Score || (score.Score == bestScore.Score && state == QuestState.Active))
                {
                    best = quest;
                    bestScore = score;
                }
            }

            if (best == null)
            {
                response = null;
                return false;
            }

            best.Objective.Consume(dish);
            if (progress.GetQuestState(best.QuestId) == QuestState.Active && PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.Earn(best.Reward);
            }
            progress.SetQuestState(best.QuestId, QuestState.Completed);
            progress.SetQuestGrade(best.QuestId, bestScore.Grade);
            response = best.ResultText(bestScore.Grade);
            return true;
        }
    }
}
