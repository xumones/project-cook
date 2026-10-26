using NUnit.Framework;
using UnityEngine;
using ProjectCook.AI;

namespace ProjectCook.Tests
{
    public class PlanExecutorTests
    {
        private static FakeActionSO Fake(params ActionStatus[] results)
        {
            var action = ScriptableObject.CreateInstance<FakeActionSO>();
            foreach (ActionStatus result in results) action.Results.Enqueue(result);
            return action;
        }

        [Test]
        public void RunsStepsInOrder_ThenFinishes()
        {
            var walk = Fake(ActionStatus.Running, ActionStatus.Succeeded);
            var use = Fake(ActionStatus.Succeeded);
            var executor = new PlanExecutor();

            executor.Begin(new[] { walk, use }, null);

            Assert.AreEqual(PlanStatus.Running, executor.Tick(0.1f));
            Assert.AreEqual(0, use.Begins);
            Assert.AreEqual(PlanStatus.Running, executor.Tick(0.1f));
            Assert.AreEqual(1, use.Begins);
            Assert.AreEqual(PlanStatus.Finished, executor.Tick(0.1f));
            Assert.IsFalse(executor.HasPlan);
            Assert.AreEqual(PlanStatus.Idle, executor.Tick(0.1f));
        }

        [Test]
        public void FailingStep_DropsWholePlan()
        {
            var walk = Fake(ActionStatus.Failed);
            var use = Fake(ActionStatus.Succeeded);
            var executor = new PlanExecutor();

            executor.Begin(new[] { walk, use }, null);

            Assert.AreEqual(PlanStatus.Failed, executor.Tick(0.1f));
            Assert.IsFalse(executor.HasPlan);
            Assert.AreEqual(0, use.Begins);
        }

        [Test]
        public void PauseAndResume_RestartsTheSameStep()
        {
            var walk = Fake();
            var use = Fake();
            var executor = new PlanExecutor();
            executor.Begin(new[] { walk, use }, null);

            executor.Pause();
            Assert.IsTrue(executor.IsPaused);
            Assert.AreEqual(1, walk.Stops);
            Assert.AreEqual(PlanStatus.Idle, executor.Tick(0.1f));

            executor.Resume();
            Assert.AreEqual(2, walk.Begins);
            Assert.AreEqual(0, use.Begins);
        }
    }
}
