using NUnit.Framework;
using ProjectCook.AI;

namespace ProjectCook.Tests
{
    public class AgentMemoryTests
    {
        private class TestSensor : Sensor
        {
            public int Runs;

            public TestSensor(float interval) : base(interval) { }

            protected override bool Sense(float now)
            {
                Runs++;
                return true;
            }
        }

        [Test]
        public void Belief_KeepsLastSeenValue_UntilObservedAgain()
        {
            var memory = new AgentMemory();
            object disc = new object();

            Assert.IsFalse(memory.TryRecall(disc, 0f, out _, out _));

            memory.Observe(disc, true, 10f);
            Assert.IsTrue(memory.TryRecall(disc, 25f, out bool value, out float age));
            Assert.IsTrue(value);
            Assert.AreEqual(15f, age);

            memory.Observe(disc, false, 30f);
            memory.TryRecall(disc, 30f, out value, out _);
            Assert.IsFalse(value);
        }

        [Test]
        public void Failure_IsAvoided_UntilItExpires()
        {
            var memory = new AgentMemory();
            object sofa = new object();

            memory.RememberFailure(sofa, 100f, 30f);

            Assert.IsTrue(memory.HasFailedRecently(sofa, 120f));
            Assert.IsFalse(memory.HasFailedRecently(sofa, 130f));
            Assert.IsFalse(memory.HasFailedRecently(new object(), 120f));
        }

        [Test]
        public void Sensor_RunsOnItsInterval()
        {
            var sensor = new TestSensor(2f);

            Assert.IsTrue(sensor.Update(0f));
            Assert.IsFalse(sensor.Update(1f));
            Assert.IsTrue(sensor.Update(2f));
            Assert.AreEqual(2, sensor.Runs);
        }
    }
}
