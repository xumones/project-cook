using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Core;
using ProjectCook.Cooking;
using ProjectCook.Items;
using ProjectCook.Quests;

namespace ProjectCook.Tests
{
    public class QuestTests
    {
        private readonly List<Object> assets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in assets) Object.DestroyImmediate(asset);
            assets.Clear();
        }

        private T Create<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            assets.Add(asset);
            return asset;
        }

        private CollectItemObjectiveSO CollectObjective(ItemDefinitionSO item, int count, bool foundOutsideOnly)
        {
            var objective = Create<CollectItemObjectiveSO>();
            var serialized = new SerializedObject(objective);
            serialized.FindProperty("item").objectReferenceValue = item;
            serialized.FindProperty("count").intValue = count;
            serialized.FindProperty("foundOutsideOnly").boolValue = foundOutsideOnly;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return objective;
        }

        [Test]
        public void Collect_CountsOnlyMatchingItemsFoundOutside()
        {
            var egg = Create<ItemDefinitionSO>();
            var quest = new ActiveQuest(new FakeQuest(CollectObjective(egg, 2, true)), "npc", null);

            var bought = new ItemInstance(egg);
            var found = new ItemInstance(egg);
            found.AddFlag(ItemFlags.FoundOutside);
            var other = new ItemInstance(Create<ItemDefinitionSO>());
            other.AddFlag(ItemFlags.FoundOutside);

            quest.Advance(o => o.OnItemCollected(bought));
            quest.Advance(o => o.OnItemCollected(other));
            Assert.AreEqual(0, quest.ProgressOf(0));

            quest.Advance(o => o.OnItemCollected(found));
            quest.Advance(o => o.OnItemCollected(found));
            quest.Advance(o => o.OnItemCollected(found));
            Assert.AreEqual(2, quest.ProgressOf(0), "ความคืบหน้าต้องหยุดที่จำนวนที่ต้องการ");
            Assert.IsTrue(quest.IsComplete);
        }

        [Test]
        public void Delivery_WithoutRecipient_GoesToTheGiver()
        {
            var quest = new ActiveQuest(new FakeQuest(new FakeDelivery(string.Empty)), "giver", null);

            Assert.IsTrue(quest.TryGetPendingDelivery("giver", out int index));
            Assert.IsFalse(quest.TryGetPendingDelivery("someone_else", out _));

            quest.CompleteObjective(index);
            Assert.IsFalse(quest.TryGetPendingDelivery("giver", out _));
            Assert.IsTrue(quest.IsComplete);
        }

        [Test]
        public void Delivery_WithRecipient_GoesOnlyToThatRecipient()
        {
            var quest = new ActiveQuest(new FakeQuest(new FakeDelivery("baker")), "giver", null);

            Assert.IsFalse(quest.TryGetPendingDelivery("giver", out _));
            Assert.IsTrue(quest.TryGetPendingDelivery("baker", out _));
        }

        [Test]
        public void Evaluate_PerfectDishScoresS_MissingIngredientFails()
        {
            var steak = Create<IngredientDataSO>();
            var requirements = new[] { new IngredientRequirement { ingredient = steak, idealState = CookingState.Cooked } };

            DishScore perfect = DishEvaluator.Evaluate(requirements, new[] { new IngredientSnapshot(steak, CookingState.Cooked, 1f) });
            DishScore empty = DishEvaluator.Evaluate(requirements, new IngredientSnapshot[0]);

            Assert.AreEqual(QualityGrade.S, perfect.Grade);
            Assert.AreEqual(QualityGrade.F, empty.Grade);
        }

        private class FakeQuest : IQuestDefinition
        {
            private readonly IQuestObjective[] objectives;

            public FakeQuest(params IQuestObjective[] objectives) => this.objectives = objectives;

            public string QuestId => "fake";
            public int Reward => 0;
            public IReadOnlyList<IQuestObjective> Objectives => objectives;
            public LocalizedString OfferText => null;
            public LocalizedString ReminderText => null;
            public LocalizedString ResultText(QualityGrade grade) => null;
        }

        private class FakeDelivery : IDeliveryObjective
        {
            public FakeDelivery(string recipientId) => RecipientId = recipientId;

            public string RecipientId { get; }
            public int Required => 1;
            public string Describe() => "dish";
            public DishScore Evaluate(IReadOnlyList<IngredientSnapshot> dish) => new DishScore(1f, QualityGrade.S);
            public int OnItemCollected(ItemInstance item) => 0;
            public int OnWorldEvent(string eventId) => 0;
            public int OnTalkedTo(string speakerId) => 0;
        }
    }
}
