using System.Collections.Generic;
using NUnit.Framework;
using ProjectCook.AI;

namespace ProjectCook.Tests
{
    public class GoalSelectorTests
    {
        [Test]
        public void Committed_OnlyHigherRankMayReplace()
        {
            var eat = GoalRankingTests.Goal("eat", 0, 1f, null);
            var rest = GoalRankingTests.Goal("rest", 0, 1f, null);
            var duty = GoalRankingTests.Goal("duty", 1, 1f, null);

            Assert.IsFalse(GoalSelector.MayReplace(rest, eat, true));
            Assert.IsFalse(GoalSelector.MayReplace(eat, eat, true));
            Assert.IsTrue(GoalSelector.MayReplace(duty, eat, true));
        }

        [Test]
        public void NotCommitted_OrNoCurrentGoal_AnyGoalMayReplace()
        {
            var eat = GoalRankingTests.Goal("eat", 0, 1f, null);
            var rest = GoalRankingTests.Goal("rest", 0, 1f, null);

            Assert.IsTrue(GoalSelector.MayReplace(rest, eat, false));
            Assert.IsTrue(GoalSelector.MayReplace(rest, null, true));
        }

        [Test]
        public void WithPlan_NeverSatisfices_EvenAtLowWellbeing()
        {
            var satiety = MeterTests.Create<NeedSO>("satiety");
            var energy = MeterTests.Create<NeedSO>("energy");
            var meters = new MeterSet();
            meters.Add(satiety, 40f);
            meters.Add(energy, 45f);
            var eat = GoalRankingTests.Goal("eat", 0, 1f, satiety);
            var rest = GoalRankingTests.Goal("rest", 0, 1f, energy);
            var selector = new GoalSelector(new[] { eat, rest }, 0f, new ImperfectionRoller(new ImperfectionSettings(), 7));
            var firsts = new HashSet<AIGoalSO>();

            for (int i = 0; i < 50; i++)
            {
                firsts.Add(selector.Rank(meters, null, true, 0f)[0].Goal);
            }

            CollectionAssert.AreEquivalent(new[] { eat }, firsts);
        }
    }
}
