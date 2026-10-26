using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectCook.AI;

namespace ProjectCook.Tests
{
    public class SmartObjectTests
    {
        private GameObject host;

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
        }

        private SmartObject CreateSofa(NeedSO energy, float amount, float seconds)
        {
            host = new GameObject("Sofa");
            var smartObject = host.AddComponent<SmartObject>();
            var so = new SerializedObject(smartObject);
            so.FindProperty("useSeconds").floatValue = seconds;
            SerializedProperty ads = so.FindProperty("advertisements");
            ads.arraySize = 1;
            ads.GetArrayElementAtIndex(0).FindPropertyRelative("meter").objectReferenceValue = energy;
            ads.GetArrayElementAtIndex(0).FindPropertyRelative("amount").floatValue = amount;
            so.ApplyModifiedPropertiesWithoutUndo();
            return smartObject;
        }

        [Test]
        public void Reserve_BlocksOthers_UntilOwnerReleases()
        {
            var sofa = CreateSofa(MeterTests.Create<NeedSO>("energy"), 50f, 20f);
            object npcA = new object();
            object npcB = new object();

            Assert.IsTrue(sofa.TryReserve(npcA));
            Assert.IsTrue(sofa.TryReserve(npcA));
            Assert.IsFalse(sofa.TryReserve(npcB));

            sofa.Release(npcB);
            Assert.IsFalse(sofa.IsAvailableTo(npcB));

            sofa.Release(npcA);
            Assert.IsTrue(sofa.TryReserve(npcB));
        }

        [Test]
        public void ApplyUse_GivesShareOfPromise_ForTimeUsed()
        {
            var energy = MeterTests.Create<NeedSO>("energy");
            var sofa = CreateSofa(energy, 50f, 20f);
            var meters = new MeterSet();
            meters.Add(energy, 10f);

            sofa.ApplyUse(meters, 10f);

            Assert.AreEqual(50f, sofa.Promise(energy));
            Assert.AreEqual(35f, meters.Get(energy), 0.001f);
        }
    }
}
