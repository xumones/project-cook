using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>ภาชนะที่แผ่ความร้อนให้วัตถุดิบ (กระทะ, หม้อต้ม, เตาอบ, หม้อทอด)</summary>
    public abstract class HeatingFoodContainer : FoodContainer
    {
        [Header("Cooking Heat Settings")]
        [Tooltip("เปิดใช้งานการแผ่ความร้อนทอด/ต้มใส่อาหารในภาชนะ")]
        [SerializeField] protected bool isHeating = true;

        [Tooltip("ตัวคูณความเร็วการสะสมความร้อน")]
        [SerializeField] protected float heatRateMultiplier = 1.0f;

        public override bool IsHeating => isHeating;

        protected override void ProcessIngredient(ContainedIngredient entry)
        {
            if (!isHeating || entry.Ingredient.IsBurnt) return;

            ApplyHeat(entry);
        }

        /// <summary>ภาชนะให้ความร้อนแต่ละชนิดกำหนดเงื่อนไขการส่งผ่านความร้อนของตัวเอง</summary>
        protected abstract void ApplyHeat(ContainedIngredient entry);
    }
}
