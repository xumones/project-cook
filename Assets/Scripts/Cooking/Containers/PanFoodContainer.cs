using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>กระทะ: ส่งความร้อนตามสัดส่วนชิ้นที่แตะผิวกระทะ และบอกทิศ up ให้วัตถุดิบตัดสินด้านที่โดนทอด</summary>
    public class PanFoodContainer : HeatingFoodContainer
    {
        [Header("Pan Specific References")]
        [Tooltip("Transform ของกระทะ (หากไม่ใส่จะอ้างอิงจาก Transform ตัวเอง)")]
        [SerializeField] private Transform panTransform;

        private const float SurfaceContactDistance = 0.03f;

        private static readonly RaycastHit[] raycastHitBuffer = new RaycastHit[8];

        private struct ContactSample
        {
            public int Frame;
            public float Fraction;
        }

        // ทั้งการทอดและเสียงถามค่านี้ในเฟรมเดียวกัน จึงยิง Raycast ครั้งเดียวต่อวัตถุดิบต่อเฟรม
        private readonly Dictionary<IngredientInstance, ContactSample> contactCache = new Dictionary<IngredientInstance, ContactSample>();

        protected override void Awake()
        {
            if (panTransform == null)
            {
                panTransform = transform;
            }

            base.Awake();
        }

        protected override void ApplyHeat(ContainedIngredient entry)
        {
            float contact = SurfaceContactFraction(entry.Ingredient);
            if (contact <= 0f) return;

            entry.Ingredient.ApplyHeat(Time.fixedDeltaTime * heatRateMultiplier * contact, panTransform.up);
        }

        public override bool IsReceivingHeat(ContainedIngredient entry)
        {
            return SurfaceContactFraction(entry.Ingredient) > 0f;
        }

        protected override void RemoveAt(int index)
        {
            if (index >= 0 && index < contents.Count && contents[index].Ingredient != null)
            {
                contactCache.Remove(contents[index].Ingredient);
            }

            base.RemoveAt(index);
        }

        /// <summary>สัดส่วนชิ้นที่แตะผิวกระทะ (0-1) วัตถุดิบที่วางอยู่เฉย ๆ ถือว่าแตะเต็ม เช็คจริงเฉพาะตอนถูกคีบยกอยู่</summary>
        private float SurfaceContactFraction(IngredientInstance ingredient)
        {
            if (ingredient == null) return 0f;
            if (!ingredient.IsGripped) return 1f;

            if (contactCache.TryGetValue(ingredient, out ContactSample sample) && sample.Frame == Time.frameCount)
            {
                return sample.Fraction;
            }

            IReadOnlyList<Rigidbody> bodies = ingredient.Bodies;
            int touching = 0;
            for (int i = 0; i < bodies.Count; i++)
            {
                if (IsBodyTouchingSurface(bodies[i], ingredient.transform)) touching++;
            }

            float fraction = bodies.Count > 0 ? (float)touching / bodies.Count : 0f;
            contactCache[ingredient] = new ContactSample { Frame = Time.frameCount, Fraction = fraction };
            return fraction;
        }

        private bool IsBodyTouchingSurface(Rigidbody body, Transform ingredientRoot)
        {
            int hitCount = Physics.RaycastNonAlloc(body.position, -panTransform.up, raycastHitBuffer, SurfaceContactDistance);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = raycastHitBuffer[i].collider;
                if (col == null || col.isTrigger) continue;

                // ชิ้นอื่นของวัตถุดิบเดียวกันไม่นับเป็นพื้นผิว
                if (col.transform.IsChildOf(ingredientRoot)) continue;

                return true;
            }
            return false;
        }
    }
}
