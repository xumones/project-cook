using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectCook.AI;

namespace ProjectCook.Tests
{
    public class PlannerTests
    {
        private FactTable table;
        private FactSO atSofa, rested, hasRecipe, hasNumber, fed;

        [SetUp]
        public void SetUp()
        {
            table = new FactTable();
            atSofa = ScriptableObject.CreateInstance<FactSO>();
            rested = ScriptableObject.CreateInstance<FactSO>();
            hasRecipe = ScriptableObject.CreateInstance<FactSO>();
            hasNumber = ScriptableObject.CreateInstance<FactSO>();
            fed = ScriptableObject.CreateInstance<FactSO>();
        }

        private WorldState State(params (FactSO fact, bool value)[] facts)
        {
            var conditions = new List<FactCondition>();
            foreach ((FactSO fact, bool value) in facts) conditions.Add(new FactCondition { fact = fact, value = value });
            return table.Build(conditions);
        }

        private PlannerAction Action(WorldState pre, WorldState effects, float cost = 1f) => new PlannerAction(pre, effects, cost);

        [Test]
        public void ChainsActions_ToSatisfyPrecondition()
        {
            var actions = new List<PlannerAction>
            {
                Action(State((atSofa, true)), State((rested, true))),
                Action(State(), State((atSofa, true)))
            };
            var plan = new List<int>();

            Assert.IsTrue(GoapPlanner.TryPlan(State(), State((rested, true)), actions, plan));
            CollectionAssert.AreEqual(new[] { 1, 0 }, plan);
        }

        [Test]
        public void PrefersCheaperPlan_AndFallsBackWhenItsPreconditionIsMissing()
        {
            // ตัวอย่าง Alma ของ Orkin: สั่งพิซซ่าถูกกว่าอบพาย แต่ต้องมีเบอร์โทร
            var actions = new List<PlannerAction>
            {
                Action(State((hasRecipe, true)), State((fed, true)), 8f),
                Action(State((hasNumber, true)), State((fed, true)), 2f)
            };
            var plan = new List<int>();

            Assert.IsTrue(GoapPlanner.TryPlan(State((hasRecipe, true), (hasNumber, true)), State((fed, true)), actions, plan));
            CollectionAssert.AreEqual(new[] { 1 }, plan);

            Assert.IsTrue(GoapPlanner.TryPlan(State((hasRecipe, true)), State((fed, true)), actions, plan));
            CollectionAssert.AreEqual(new[] { 0 }, plan);
        }

        [Test]
        public void ReturnsFalse_WhenGoalIsUnreachable()
        {
            var actions = new List<PlannerAction> { Action(State((hasRecipe, true)), State((fed, true))) };
            var plan = new List<int>();

            Assert.IsFalse(GoapPlanner.TryPlan(State(), State((fed, true)), actions, plan));
            Assert.IsEmpty(plan);
        }

        [Test]
        public void ReturnsEmptyPlan_WhenGoalAlreadyHolds()
        {
            var plan = new List<int> { 99 };

            Assert.IsTrue(GoapPlanner.TryPlan(State((rested, true)), State((rested, true)), new List<PlannerAction>(), plan));
            Assert.IsEmpty(plan);
        }

        [Test]
        public void StopsSearching_PastMaxDepth()
        {
            var actions = new List<PlannerAction>
            {
                Action(State(), State((hasNumber, true))),
                Action(State((hasNumber, true)), State((atSofa, true))),
                Action(State((atSofa, true)), State((rested, true)))
            };
            var plan = new List<int>();

            Assert.IsFalse(GoapPlanner.TryPlan(State(), State((rested, true)), actions, plan, maxDepth: 2));
            Assert.IsTrue(GoapPlanner.TryPlan(State(), State((rested, true)), actions, plan, maxDepth: 3));
            Assert.AreEqual(3, plan.Count);
        }

        [Test]
        public void UnspecifiedFacts_CountAsFalse_InCurrentState()
        {
            Assert.IsTrue(State().Satisfies(State((fed, false))));
            Assert.IsFalse(State().Satisfies(State((fed, true))));
        }
    }
}
