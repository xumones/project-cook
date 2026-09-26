using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>ภาชนะที่แผ่ความร้อนให้วัตถุดิบ (กระทะ, หม้อต้ม, เตาอบ, หม้อทอด) ความร้อนค่อยๆ ไล่ตามค่าที่แหล่งความร้อนป้อนให้</summary>
    public abstract class HeatingFoodContainer : FoodContainer
    {
        [Header("Cooking Heat Settings")]
        [Tooltip("ตัวคูณความเร็วการสะสมความร้อน")]
        [SerializeField] protected float heatRateMultiplier = 1.0f;

        [Tooltip("เวลาที่ใช้ร้อนจาก 0 จนสุด (วินาที)")]
        [SerializeField] private float heatUpDuration = 3f;

        [Tooltip("เวลาที่ใช้เย็นจากร้อนสุดจนเหลือ 0 (วินาที)")]
        [SerializeField] private float coolDownDuration = 6f;

        private float heat;
        private float heatInput;

        /// <summary>ความร้อนที่แหล่งความร้อนป้อนให้ 0-1</summary>
        public float HeatInput
        {
            get => heatInput;
            set => heatInput = Mathf.Clamp01(value);
        }

        public override float Heat => heat;

        protected override void FixedUpdate()
        {
            float duration = heatInput > heat ? heatUpDuration : coolDownDuration;
            heat = Mathf.MoveTowards(heat, heatInput, Time.fixedDeltaTime / Mathf.Max(duration, 0.01f));

            base.FixedUpdate();
        }

        protected override void ProcessIngredient(ContainedIngredient entry)
        {
            if (heat <= 0f || entry.Ingredient.IsBurnt) return;

            ApplyHeat(entry);
        }

        /// <summary>ภาชนะให้ความร้อนแต่ละชนิดกำหนดเงื่อนไขการส่งผ่านความร้อนของตัวเอง</summary>
        protected abstract void ApplyHeat(ContainedIngredient entry);
    }
}
