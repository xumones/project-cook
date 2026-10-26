using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectCook.AI;

namespace ProjectCook.Tests
{
    public class GoalRankingTests
    {
        private NeedSO satiety, energy;
        private MeterSet meters;
        private ImperfectionRoller roller;

        [SetUp]
        public void SetUp()
        {
            satiety = MeterTests.Create<NeedSO>("satiety");
            energy = MeterTests.Create<NeedSO>("energy");
            meters = new MeterSet();
            meters.Add(satiety, 100f);
            meters.Add(energy, 100f);
            roller = new ImperfectionRoller(new ImperfectionSettings(), 7);
        }

        private static AIGoalSO Goal(string label, int rank, float weight, MeterSO meter)
        {
            var goal = ScriptableObject.CreateInstance<AIGoalSO>();
            var so = new SerializedObject(goal);
            so.FindProperty("label").stringValue = label;
            so.FindProperty("rank").intValue = rank;
            so.FindProperty("weight").floatValue = weight;
            if (meter != null)
            {
                SerializedProperty considerations = so.FindProperty("considerations");
                considerations.arraySize = 1;
                considerations.GetArrayElementAtIndex(0).FindPropertyRelative("meter").objectReferenceValue = meter;
                considerations.GetArrayElementAtIndex(0).FindPropertyRelative("power").floatValue = 1f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return goal;
        }

        [Test]
        public void Score_RisesAsNeedEmpties()
        {
            var eat = Goal("eat", 0, 1f, satiety);

            meters.Set(satiety, 80f);
            Assert.AreEqual(0.2f, eat.Score(meters), 0.001f);
            meters.Set(satiety, 20f);
            Assert.AreEqual(0.8f, eat.Score(meters), 0.001f);
        }

        [Test]
        public void HigherRank_ComesFirst_EvenWithLowerScore()
        {
            var eat = Goal("eat", 0, 1f, satiety);
            var duty = Goal("duty", 1, 0.1f, null);
            meters.Set(satiety, 0f);
            var result = new List<GoalChoice>();

            GoalRanking.Rank(new[] { eat, duty }, meters, null, 0f, 0f, roller, result);

            Assert.AreSame(duty, result[0].Goal);
            Assert.AreSame(eat, result[1].Goal);
        }

        [Test]
        public void CurrentGoal_KeepsLead_WhenScoresAreClose()
        {
            var eat = Goal("eat", 0, 1f, satiety);
            var rest = Goal("rest", 0, 1f, energy);
            meters.Set(satiety, 50f);
            meters.Set(energy, 45f);
            var result = new List<GoalChoice>();

            GoalRanking.Rank(new[] { eat, rest }, meters, null, 0.1f, 0f, roller, result);
            Assert.AreSame(rest, result[0].Goal);

            GoalRanking.Rank(new[] { eat, rest }, meters, eat, 0.1f, 0f, roller, result);
            Assert.AreSame(eat, result[0].Goal);
        }

        [Test]
        public void ZeroScoreGoals_AreLeftOut()
        {
            var eat = Goal("eat", 0, 1f, satiety);
            var result = new List<GoalChoice>();

            GoalRanking.Rank(new[] { eat }, meters, null, 0f, 0f, roller, result);

            Assert.IsEmpty(result);
        }

        [Test]
        public void Satisficing_SometimesPicksAGoalInsideTheBand_ButNeverOutside()
        {
            var eat = Goal("eat", 0, 1f, satiety);
            var rest = Goal("rest", 0, 1f, energy);
            var idle = Goal("idle", 0, 0.05f, null);
            meters.Set(satiety, 40f);
            meters.Set(energy, 45f);
            var result = new List<GoalChoice>();
            var firsts = new HashSet<AIGoalSO>();

            for (int i = 0; i < 50; i++)
            {
                GoalRanking.Rank(new[] { eat, rest, idle }, meters, null, 0f, 0.1f, roller, result);
                firsts.Add(result[0].Goal);
            }

            CollectionAssert.AreEquivalent(new[] { eat, rest }, firsts);
        }
    }
}
