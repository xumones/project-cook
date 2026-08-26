using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// Abstract Class สำหรับภาชนะที่แผ่ความร้อนให้วัตถุดิบ (กระทะ, หม้อต้ม, เตาอบ, หม้อทอด)
    ///
    /// แยกออกมาจาก FoodContainer เพราะ "การเป็นภาชนะที่เก็บวัตถุดิบ" กับ "การให้ความร้อน"
    /// เป็นคนละเรื่องกัน ภาชนะอย่างจานเสิร์ฟหรือเขียงหั่นสืบทอดจาก FoodContainer ได้โดยตรง
    /// โดยไม่ต้องแบกค่าตั้งค่าความร้อนที่ไม่ได้ใช้ติดไปด้วย
    /// </summary>
    public abstract class HeatingFoodContainer : FoodContainer
    {
        [Header("Cooking Heat Settings")]
        [Tooltip("เปิดใช้งานการแผ่ความร้อนทอด/ต้มใส่อาหารในภาชนะ")]
        [SerializeField] protected bool isHeating = true;

        [Tooltip("ตัวคูณความเร็วการสะสมความร้อน")]
        [SerializeField] protected float heatRateMultiplier = 1.0f;

        public override bool IsHeating => isHeating;

        /// <summary>
        /// เปิด/ปิด การแผ่ความร้อนของภาชนะนี้ (เช่น ปิดเตา)
        /// </summary>
        public void SetHeatingActive(bool active)
        {
            isHeating = active;
        }

        protected override void ProcessItem(FoodItemData item)
        {
            if (!isHeating || item.Ingredient.IsBurnt) return;

            ApplyHeatToItem(item);
        }

        /// <summary>
        /// Abstract Method ที่ภาชนะให้ความร้อนแต่ละชนิดต้องกำหนดเงื่อนไขการส่งผ่านความร้อนของตัวเอง
        /// (เช่น กระทะเช็คการสัมผัสพื้นผิว ส่วนหม้อต้มเช็คการจมอยู่ในน้ำ)
        /// </summary>
        protected abstract void ApplyHeatToItem(FoodItemData item);
    }
}
