using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectCook.Core;
using ProjectCook.Cooking;

namespace ProjectCook.Tests
{
    public class CookingTests
    {
        private readonly List<Object> assets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in assets) Object.DestroyImmediate(asset);
            assets.Clear();
        }

        private IngredientDataSO Ingredient(float cookTime, float burnTime, bool twoSided)
        {
            var data = ScriptableObject.CreateInstance<IngredientDataSO>();
            var serialized = new SerializedObject(data);
            serialized.FindProperty("cookTime").floatValue = cookTime;
            serialized.FindProperty("burnTime").floatValue = burnTime;
            serialized.FindProperty("isTwoSidedCooking").boolValue = twoSided;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            assets.Add(data);
            return data;
        }

        [Test]
        public void Omni_GoesRawCookingCookedBurnt_AndStopsAtBurnt()
        {
            var progress = new CookingProgress(Ingredient(5f, 5f, false));
            Assert.AreEqual(CookingState.Raw, progress.State);

            Assert.IsTrue(progress.AddHeat(4f, CookingSide.Omni));
            Assert.AreEqual(CookingState.Cooking, progress.State);

            Assert.IsTrue(progress.AddHeat(1f, CookingSide.Omni));
            Assert.AreEqual(CookingState.Cooked, progress.State);
            Assert.AreEqual(1f, progress.Progress, 0.0001f);

            Assert.IsTrue(progress.AddHeat(5f, CookingSide.Omni));
            Assert.AreEqual(CookingState.Burnt, progress.State);

            Assert.IsFalse(progress.AddHeat(5f, CookingSide.Omni), "ไหม้แล้วต้องไม่เปลี่ยนอีก");
            Assert.AreEqual(10f, progress.OmniTime, 0.0001f);
        }

        [Test]
        public void TwoSided_NeedsBothSidesCooked()
        {
            var progress = new CookingProgress(Ingredient(5f, 5f, true));

            progress.AddHeat(5f, CookingSide.SideA);
            Assert.AreEqual(CookingState.Cooking, progress.State, "สุกด้านเดียวยังไม่นับว่าสุก");

            progress.AddHeat(5f, CookingSide.SideB);
            Assert.AreEqual(CookingState.Cooked, progress.State);
        }

        [Test]
        public void TwoSided_OnItsEdge_SplitsHeatBetweenSides()
        {
            var progress = new CookingProgress(Ingredient(5f, 5f, true));

            progress.AddHeat(4f, CookingSide.Omni);

            Assert.AreEqual(2f, progress.SideATime, 0.0001f);
            Assert.AreEqual(2f, progress.SideBTime, 0.0001f);
        }

        [Test]
        public void TwoSided_OneSideOverBurnTime_Burns()
        {
            var progress = new CookingProgress(Ingredient(5f, 5f, true));

            progress.AddHeat(10f, CookingSide.SideA);

            Assert.AreEqual(CookingState.Burnt, progress.State);
        }

        [Test]
        public void Restore_RecomputesStateFromTimes()
        {
            var progress = new CookingProgress(Ingredient(5f, 5f, true));

            progress.Restore(0f, 6f, 5f);

            Assert.AreEqual(CookingState.Cooked, progress.State);
        }

        [Test]
        public void Evaluate_UndercookedScoresLowerThanCooked()
        {
            IngredientDataSO steak = Ingredient(5f, 5f, false);
            var requirements = new[] { new IngredientRequirement { ingredient = steak, idealState = CookingState.Cooked } };

            DishScore cooked = DishEvaluator.Evaluate(requirements, new[] { new IngredientSnapshot(steak, CookingState.Cooked, 1f) });
            DishScore half = DishEvaluator.Evaluate(requirements, new[] { new IngredientSnapshot(steak, CookingState.Cooking, 0.5f) });

            Assert.Greater(cooked.Score, half.Score);
            Assert.AreEqual(QualityGrade.F, half.Grade);
        }
    }
}
