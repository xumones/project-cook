using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;
using ProjectCook.Cooking;
using ProjectCook.Dialogue;
using ProjectCook.Inventory;
using ProjectCook.Items;

namespace ProjectCook.Quests
{
    /// <summary>เจ้าของเควสทั้งหมดของผู้เล่น: เควสที่รับอยู่ เควสที่จบแล้ว ความคืบหน้าจากข่าวในเกม และการจ่ายรางวัล</summary>
    public class QuestLog : SceneSingleton<QuestLog>
    {
        [Tooltip("เควสที่เริ่มเองเมื่อหยิบไอเทมหรือเกิดเหตุการณ์ตามที่ตั้งไว้ในเควส")]
        [SerializeField] private List<QuestSO> triggeredQuests = new List<QuestSO>();

        private readonly List<ActiveQuest> active = new List<ActiveQuest>();
        private readonly HashSet<string> completed = new HashSet<string>();

        public IReadOnlyList<ActiveQuest> Active => active;

        /// <summary>มีเควสเริ่ม เดินหน้า หรือจบ</summary>
        public event Action Changed;

        private void OnEnable()
        {
            MessageBus<ItemCollected>.Subscribe(HandleItemCollected);
            MessageBus<WorldEventTriggered>.Subscribe(HandleWorldEvent);
            MessageBus<DialogueStarted>.Subscribe(HandleDialogueStarted);
        }

        private void OnDisable()
        {
            MessageBus<ItemCollected>.Unsubscribe(HandleItemCollected);
            MessageBus<WorldEventTriggered>.Unsubscribe(HandleWorldEvent);
            MessageBus<DialogueStarted>.Unsubscribe(HandleDialogueStarted);
        }

        public bool IsCompleted(string questId) => !string.IsNullOrEmpty(questId) && completed.Contains(questId);

        /// <summary>เริ่มเควสนี้ได้ไหม เควสทำซ้ำได้เริ่มได้เสมอ เควสครั้งเดียวต้องยังไม่รับและยังไม่จบ</summary>
        public bool CanStart(IQuestDefinition definition)
        {
            if (string.IsNullOrEmpty(definition.QuestId)) return true;
            return !IsCompleted(definition.QuestId) && !active.Exists(q => q.Definition.QuestId == definition.QuestId);
        }

        public ActiveQuest Find(string id) => active.Find(q => q.Id == id);

        public ActiveQuest FindByGiver(string giverId) => active.Find(q => q.GiverId == giverId);

        /// <summary>เพิ่มเควสที่ผู้ให้สร้างไว้ ผู้ให้สร้างก่อนเพื่อใช้รหัสของมันทำโน้ตได้</summary>
        public void Begin(ActiveQuest quest)
        {
            active.Add(quest);
            Changed?.Invoke();
        }

        /// <summary>เควสแรกที่รอให้ผู้รับคนนี้รับอาหาร</summary>
        public bool TryFindDelivery(string recipientId, out ActiveQuest quest)
        {
            foreach (ActiveQuest candidate in active)
            {
                if (!candidate.TryGetPendingDelivery(recipientId, out _)) continue;

                quest = candidate;
                return true;
            }

            quest = null;
            return false;
        }

        /// <summary>ส่งอาหารให้ผู้รับ ผ่าน (ไม่ใช่ F) = เป้าหมายส่งอาหารข้อนั้นครบ ผู้เรียกเป็นคนเอาอาหารออกจากจาน</summary>
        public DishScore Deliver(ActiveQuest quest, string recipientId, IReadOnlyList<IngredientSnapshot> dish)
        {
            if (!quest.TryGetPendingDelivery(recipientId, out int index)) return new DishScore(0f, QualityGrade.F);

            DishScore score = ((IDeliveryObjective)quest.Definition.Objectives[index]).Evaluate(dish);
            if (score.Grade == QualityGrade.F) return score;

            quest.CompleteObjective(index);
            OnProgress(quest);
            return score;
        }

        private void HandleItemCollected(ItemCollected message)
        {
            AdvanceAll(objective => objective.OnItemCollected(message.Item));

            // เริ่มหลังนับ ไอเทมที่ทำให้เควสเริ่มจึงไม่ถูกนับเป็นความคืบหน้าของเควสนั้นเอง
            StartTriggered(quest => quest.StartItem != null && quest.StartItem == message.Item.Definition);
        }

        private void HandleWorldEvent(WorldEventTriggered message)
        {
            AdvanceAll(objective => objective.OnWorldEvent(message.EventId));
            StartTriggered(quest => !string.IsNullOrEmpty(quest.StartEvent) && quest.StartEvent == message.EventId);
        }

        private void HandleDialogueStarted(DialogueStarted message) =>
            AdvanceAll(objective => objective.OnTalkedTo(message.SpeakerId));

        private void AdvanceAll(Func<IQuestObjective, int> gainOf)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].Advance(gainOf)) OnProgress(active[i]);
            }
        }

        private void StartTriggered(Predicate<QuestSO> startsOn)
        {
            foreach (QuestSO quest in triggeredQuests)
            {
                if (quest != null && startsOn(quest) && CanStart(quest)) Begin(new ActiveQuest(quest, null, null));
            }
        }

        private void OnProgress(ActiveQuest quest)
        {
            if (quest.IsComplete)
            {
                Complete(quest);
                return;
            }
            Changed?.Invoke();
        }

        private void Complete(ActiveQuest quest)
        {
            active.Remove(quest);
            if (!string.IsNullOrEmpty(quest.Definition.QuestId)) completed.Add(quest.Definition.QuestId);

            if (PlayerWallet.Instance != null) PlayerWallet.Instance.Earn(quest.Definition.Reward);
            QuestNote.Discard(quest.Id);
            Changed?.Invoke();
        }
    }
}
