using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>กระทะ: ส่งความร้อนตามสัดส่วนชิ้นที่แตะผิวกระทะ และบอกทิศ up ให้วัตถุดิบตัดสินด้านที่โดนทอด</summary>
    public class PanFoodContainer : HeatingFoodContainer
    {
        private const float SurfaceContactDistance = 0.03f;

        private static readonly RaycastHit[] raycastHitBuffer = new RaycastHit[8];

        private struct ContactSample
        {
            public int Frame;
            public float Fraction;
        }

        // ทั้งการทอดและเสียงถามค่านี้ในเฟรมเดียวกัน จึงยิง Raycast ครั้งเดียวต่อวัตถุดิบต่อเฟรม
        private readonly Dictionary<IngredientInstance, ContactSample> contactCache = new Dictionary<IngredientInstance, ContactSample>();

        protected override void ApplyHeat(ContainedIngredient entry)
        {
            float contact = SurfaceContactFraction(entry);
            if (contact <= 0f) return;

            entry.Ingredient.ApplyHeat(Time.fixedDeltaTime * heatRateMultiplier * Heat * contact, transform.up);
        }

        public override bool IsReceivingHeat(ContainedIngredient entry)
        {
            return SurfaceContactFraction(entry) > 0f;
        }

        protected override void RemoveAt(int index)
        {
            if (index >= 0 && index < contents.Count && contents[index].Ingredient != null)
            {
                contactCache.Remove(contents[index].Ingredient);
            }

            base.RemoveAt(index);
        }

        /// <summary>สัดส่วนชิ้นที่แตะผิวกระทะ (0-1) นับเฉพาะชิ้นที่อยู่ใน trigger เทียบกับจำนวนชิ้นทั้งหมด ชิ้นที่ยังอยู่นอกกระทะจึงไม่ทำให้เกิดเสียงหรือความร้อน</summary>
        private float SurfaceContactFraction(ContainedIngredient entry)
        {
            IngredientInstance ingredient = entry.Ingredient;
            if (ingredient == null) return 0f;

            if (contactCache.TryGetValue(ingredient, out ContactSample sample) && sample.Frame == Time.frameCount)
            {
                return sample.Fraction;
            }

            int touching = 0;
            foreach (Rigidbody body in entry.BodiesInside)
            {
                if (IsBodyTouchingSurface(body, ingredient.transform)) touching++;
            }

            int totalBodies = ingredient.Bodies.Count;
            float fraction = totalBodies > 0 ? (float)touching / totalBodies : 0f;
            contactCache[ingredient] = new ContactSample { Frame = Time.frameCount, Fraction = fraction };
            return fraction;
        }

        private bool IsBodyTouchingSurface(Rigidbody body, Transform ingredientRoot)
        {
            int hitCount = Physics.RaycastNonAlloc(body.position, -transform.up, raycastHitBuffer, SurfaceContactDistance);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = raycastHitBuffer[i].collider;
                if (col == null || col.isTrigger) continue;

                if (col.transform.IsChildOf(transform)) return true;

                // ชิ้นอื่นของวัตถุดิบเดียวกันไม่นับเป็นพื้นผิว แต่วัตถุดิบตัวอื่นที่อยู่ในกระทะนับ (ของซ้อนกันยังโดนทอด)
                if (col.transform.IsChildOf(ingredientRoot)) continue;
                if (IsBodyInside(col.attachedRigidbody)) return true;
            }
            return false;
        }

        private bool IsBodyInside(Rigidbody body)
        {
            if (body == null) return false;

            for (int i = 0; i < contents.Count; i++)
            {
                if (contents[i].BodiesInside.Contains(body)) return true;
            }
            return false;
        }
    }
}
