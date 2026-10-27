using NUnit.Framework;
using ProjectCook.AI;

namespace ProjectCook.Tests
{
    public class ImperfectionTests
    {
        [Test]
        public void Effective_GrowsAsWellbeingFalls()
        {
            var roller = new ImperfectionRoller(new ImperfectionSettings { baseValue = 0.2f }, 1);

            Assert.AreEqual(0.4f, roller.Effective(0f), 0.001f);
            Assert.AreEqual(0.1f, roller.Effective(100f), 0.001f);
            Assert.AreEqual(0.7f * 0.4f, roller.ScoreBand(0f), 0.001f);
        }

        [Test]
        public void ZeroBase_NeverLapsesOrMisses()
        {
            var roller = new ImperfectionRoller(new ImperfectionSettings { baseValue = 0f }, 1);

            for (int i = 0; i < 100; i++)
            {
                Assert.IsFalse(roller.RollLapse(0f, 10f, i * 1000f));
                Assert.IsFalse(roller.RollMiss(0f, i * 1000f));
            }
        }

        [Test]
        public void Lapse_WaitsForCooldown_BeforeHappeningAgain()
        {
            // ความไม่สมบูรณ์เต็มและเวลานานมาก โอกาสเกิน 1 จึงลืมทุกครั้งที่ไม่ติดช่วงพัก
            var settings = new ImperfectionSettings { baseValue = 1f, lapsesPerMinute = 10f, lapseCooldownSeconds = 60f };
            var roller = new ImperfectionRoller(settings, 1);

            Assert.IsTrue(roller.RollLapse(50f, 1f, 0f));
            Assert.IsFalse(roller.RollLapse(50f, 1f, 30f));
            Assert.IsTrue(roller.RollLapse(50f, 1f, 60f));
        }

        [Test]
        public void SameSeed_GivesSameRolls()
        {
            var settings = new ImperfectionSettings { baseValue = 0.5f, missCooldownSeconds = 0f };
            var a = new ImperfectionRoller(settings, 42);
            var b = new ImperfectionRoller(settings, 42);

            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(a.RollMiss(50f, i), b.RollMiss(50f, i));
                Assert.AreEqual(a.Pick(5), b.Pick(5));
            }
        }
    }
}
