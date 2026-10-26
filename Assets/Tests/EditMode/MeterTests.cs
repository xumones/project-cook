using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectCook.AI;

namespace ProjectCook.Tests
{
    public class MeterTests
    {
        // ค่าที่ใช้ตรงกับค่าตั้งต้นของแต่ละชนิด (ลด 8/นาที, เพิ่ม 5/นาที เกณฑ์ 60, จาง 33/นาที -0.3 ต่อหน่วย)
        public static T Create<T>(string id) where T : MeterSO
        {
            var meter = ScriptableObject.CreateInstance<T>();
            var so = new SerializedObject(meter);
            so.FindProperty("meterId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return meter;
        }

        [Test]
        public void Need_DecaysOverTime_AndStopsAtZero()
        {
            var need = Create<NeedSO>("satiety");

            Assert.AreEqual(60f, need.Advance(100f, 5f), 0.001f);
            Assert.AreEqual(0f, need.Advance(10f, 5f));
        }

        [Test]
        public void Need_Wellbeing_NeutralInMiddle_PenaltyWhenLow_BonusWhenHigh()
        {
            var need = Create<NeedSO>("satiety");

            Assert.AreEqual(0f, need.WellbeingOffset(50f));
            Assert.AreEqual(-10f, need.WellbeingOffset(15f), 0.001f);
            Assert.AreEqual(-20f, need.WellbeingOffset(0f), 0.001f);
            Assert.AreEqual(5f, need.WellbeingOffset(90f));
        }

        [Test]
        public void Want_RisesOverTime_AndBecomesActiveAtThreshold()
        {
            var want = Create<WantSO>("craving");

            float value = want.Advance(50f, 2f);
            Assert.AreEqual(60f, value, 0.001f);
            Assert.IsTrue(want.IsActive(value));
            Assert.IsFalse(want.IsActive(59f));
        }

        [Test]
        public void Feeling_FadesBackToZero_AndPullsWellbeingDown()
        {
            var feeling = Create<FeelingSO>("frustration");

            Assert.AreEqual(7f, feeling.Advance(40f, 1f), 0.001f);
            Assert.AreEqual(0f, feeling.Advance(10f, 1f));
            Assert.AreEqual(-30f, feeling.WellbeingOffset(100f), 0.001f);
        }

        [Test]
        public void MeterSet_ClampsChanges_AndIgnoresMetersItDoesNotHave()
        {
            var satiety = Create<NeedSO>("satiety");
            var other = Create<NeedSO>("energy");
            var set = new MeterSet();
            set.Add(satiety, 90f);

            set.Change(satiety, 50f);
            set.Change(other, 50f);

            Assert.AreEqual(100f, set.Get(satiety));
            Assert.IsFalse(set.Contains(other));
            Assert.AreEqual(0f, set.Get(other));
        }

        [Test]
        public void MeterSet_RestoresById_AndSkipsUnknownIds()
        {
            var satiety = Create<NeedSO>("satiety");
            var fun = Create<NeedSO>("fun");
            var original = new MeterSet();
            original.Add(satiety, 25f);
            original.Add(fun, 75f);

            var saved = original.Capture();
            saved.Add(new SavedMeter { id = "removed", value = 10f });
            var restored = new MeterSet();
            restored.Add(satiety, 100f);
            restored.Add(fun, 100f);
            restored.Restore(saved);

            Assert.AreEqual(25f, restored.Get(satiety));
            Assert.AreEqual(75f, restored.Get(fun));
        }

        [Test]
        public void Wellbeing_DriftsTowardTarget_AtCappedRates()
        {
            var frustration = Create<FeelingSO>("frustration");
            var meters = new MeterSet();
            meters.Add(frustration, 100f);
            var wellbeing = new Wellbeing(new WellbeingSettings());

            // หงุดหงิด 100 = -30 เป้าหมายจึงเป็น 30 แต่ลงได้แค่ 10 ต่อนาที
            wellbeing.Advance(meters, 1f);
            Assert.AreEqual(30f, wellbeing.Target, 0.001f);
            Assert.AreEqual(50f, wellbeing.Value, 0.001f);

            // หายหงุดหงิดแล้วเป้าหมายกลับ 60 ขึ้นได้ 20 ต่อนาที
            meters.Set(frustration, 0f);
            wellbeing.Advance(meters, 0.25f);
            Assert.AreEqual(55f, wellbeing.Value, 0.001f);
        }
    }
}
