using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// สคริปต์จัดการการทอดอาหารในกระทะ (Pan Food Container System)
    /// สืบทอดจาก BaseFoodContainer เพิ่มเติมส่วนตรวจจับพื้นผิวกระทะ 3 ซม. และทอดอาหารแบบ 2 ด้าน
    /// </summary>
    public class PanFoodContainer : BaseFoodContainer
    {
        [Header("Pan Specific References")]
        [Tooltip("Transform ของกระทะ (หากไม่ใส่จะอ้างอิงจาก Transform ตัวเองหรือ Parent)")]
        [SerializeField] private Transform panTransform;

        private static readonly RaycastHit[] raycastHitBuffer = new RaycastHit[8];

        protected override void Awake()
        {
            if (panTransform == null)
            {
                panTransform = transform;
            }

            base.Awake();
        }

        /// <summary>
        /// Abstract Method Implementation: กำหนดการทอดเฉพาะของกระทะ (เช็กระยะ 3 ซม. + ส่ง Vector panUp)
        /// </summary>
        protected override void ApplyHeatToItem(FoodItemData item)
        {
            if (item == null || item.Ingredient == null) return;

            if (isHeating && !item.Ingredient.IsBurnt && IsTouchingPanSurface(item))
            {
                Vector3 panUp = panTransform != null ? panTransform.up : Vector3.up;
                item.Ingredient.ApplyHeat(Time.fixedDeltaTime * heatRateMultiplier, panUp);
            }
        }

        public override bool IsFoodItemActiveInStation(FoodItemData item)
        {
            if (item != null && item.Ingredient != null && item.Ingredient.IsGripped)
            {
                return IsTouchingPanSurface(item);
            }
            return true;
        }

        /// <summary>
        /// ตรวจสอบว่าวัตถุดิบชิ้นที่กำลังคีบจับอยู่ สัมผัสอยู่กับพื้นผิวกระทะจริงหรือไม่ในระยะ 3 ซม. (0.03m) แบบ Zero-GC
        /// </summary>
        public bool IsTouchingPanSurface(FoodItemData item)
        {
            if (item == null || item.Rigidbody == null || item.Ingredient == null) return false;

            // หากไม่ได้ถูกคีบจับอยู่ วัตถุดิบนั้นวางอยู่บนกระทะปกติ ไม่ต้องยิง Raycast ให้เปลือง CPU
            if (!item.Ingredient.IsGripped) return true;

            // หากเคยยิง Raycast ในเฟรมเดียวกันนี้ไปแล้ว ดึงผลลัพธ์จากแคชเดิมใช้งานทันที (ขจัด Raycast ซ้ำซ้อน)
            if (item.LastRaycastFrame == Time.frameCount)
            {
                return item.CachedIsTouchingSurface;
            }

            item.LastRaycastFrame = Time.frameCount;

            Vector3 rayStart = item.Rigidbody.position;
            Vector3 rayDir = panTransform != null ? -panTransform.up : Vector3.down;
            float maxDistance = 0.03f; // ระยะตรวจจับ 3 เซนติเมตร (3 cm)

            int hitCount = Physics.RaycastNonAlloc(rayStart, rayDir, raycastHitBuffer, maxDistance);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = raycastHitBuffer[i].collider;
                if (col == null || col.isTrigger) continue;

                if (col.attachedRigidbody == item.Rigidbody) continue;
                if (col.transform.IsChildOf(item.Ingredient.transform)) continue;

                item.CachedIsTouchingSurface = true;
                return true;
            }

            item.CachedIsTouchingSurface = false;
            return false;
        }
    }
}
