using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>Abstract Class สำหรับภาชนะที่แผ่ความร้อนให้วัตถุดิบ (กระทะ, หม้อต้ม, เตาอบ, หม้อทอด)</summary>
    public abstract class HeatingFoodContainer : FoodContainer
    {
        [Header("Cooking Heat Settings")]
        [Tooltip("เปิดใช้งานการแผ่ความร้อนทอด/ต้มใส่อาหารในภาชนะ")]
        [SerializeField] protected bool isHeating = true;

        [Tooltip("ตัวคูณความเร็วการสะสมความร้อน")]
        [SerializeField] protected float heatRateMultiplier = 1.0f;

        public override bool IsHeating => isHeating;

        protected override void ProcessItem(FoodItemData item)
        {
            if (!isHeating || item.Ingredient.IsBurnt) return;

            ApplyHeatToItem(item);
        }

        /// <summary>Abstract Method ที่ภาชนะให้ความร้อนแต่ละชนิดต้องกำหนดเงื่อนไขการส่งผ่านความร้อนของตัวเอง</summary>
        protected abstract void ApplyHeatToItem(FoodItemData item);
    }
}
