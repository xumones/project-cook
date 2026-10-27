using NUnit.Framework;
using ProjectCook.AI;

namespace ProjectCook.Tests
{
    public class WantFrustrationTests
    {
        private WantSO craving;
        private FeelingSO frustration;
        private MeterSet meters;
        private WantFrustration wants;

        [SetUp]
        public void SetUp()
        {
            craving = MeterTests.Create<WantSO>("craving");
            frustration = MeterTests.Create<FeelingSO>("frustration");
            meters = new MeterSet();
            meters.Add(craving, 60f);
            meters.Add(frustration, 0f);
            wants = new WantFrustration(frustration, 2f, 40f, 20f);
        }

        [Test]
        public void UnmetWant_RaisesFrustrationFloor_UpToCap()
        {
            wants.Advance(meters, 5f);
            Assert.AreEqual(10f, meters.Get(frustration), 0.001f);

            wants.Advance(meters, 100f);
            Assert.AreEqual(40f, meters.Get(frustration), 0.001f);
        }

        [Test]
        public void Frustration_CannotFadeBelowFloor_WhileStillWanting()
        {
            wants.Advance(meters, 5f);
            meters.Set(frustration, 0f);

            wants.Advance(meters, 0f);

            Assert.AreEqual(10f, meters.Get(frustration), 0.001f);
        }

        [Test]
        public void NoWant_LeavesFrustrationAlone()
        {
            meters.Set(craving, 0f);

            wants.Advance(meters, 5f);

            Assert.IsFalse(WantFrustration.IsWanting(meters));
            Assert.AreEqual(0f, meters.Get(frustration));
        }

        [Test]
        public void Fulfill_ClearsWants_AndRelievesFrustration()
        {
            meters.Set(frustration, 30f);

            wants.Fulfill(meters);

            Assert.AreEqual(0f, meters.Get(craving));
            Assert.AreEqual(10f, meters.Get(frustration), 0.001f);
            Assert.IsFalse(WantFrustration.IsWanting(meters));
        }
    }
}
