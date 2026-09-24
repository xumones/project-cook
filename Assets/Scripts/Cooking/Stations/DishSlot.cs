using UnityEngine;
using ProjectCook.Items;
using ProjectCook.Stations;

namespace ProjectCook.Cooking
{
    /// <summary>จุดวางจานหนึ่งใบบนสถานี (ตำแหน่ง/มุมของ GameObject นี้) ตอนเข้าสถานีจะดึงภาชนะเสิร์ฟจากมือผู้เล่นมาวางถ้าจุดยังว่าง</summary>
    public class DishSlot : MonoBehaviour, IStationModule
    {
        [Tooltip("รัศมีรอบจุดวางที่ถือว่ามีจานวางอยู่แล้ว (เมตร)")]
        [SerializeField] private float occupiedRadius = 0.15f;

        private readonly Collider[] overlapBuffer = new Collider[16];

        /// <summary>จานที่วางอยู่บนจุดนี้ตอนนี้ null ถ้าว่าง</summary>
        public ServingFoodContainer CurrentDish => FindDishOnSlot();

        public void OnStationEnter()
        {
            if (CurrentDish != null) return;
            if (!EquipmentManager.TryGetHeld(out ServingFoodContainer dish)) return;

            EquippableItem equippable = dish.GetComponentInParent<EquippableItem>();
            if (EquipmentManager.Instance.TryPlaceHeld(equippable, transform.position, transform.rotation))
            {
                // ให้ OverlapSphere เห็นจานที่เพิ่งย้ายมาทันที ไม่ต้องรอฟิสิกส์สเต็ปถัดไป
                Physics.SyncTransforms();
            }
        }

        public void OnStationExit()
        {
        }

        // เช็คจากฟิสิกส์ทุกครั้งแทนการจำไว้ ผู้เล่นจะหยิบออกหรือกด G วางคืนเองก็ยังได้ผลถูกเสมอ
        private ServingFoodContainer FindDishOnSlot()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, occupiedRadius, overlapBuffer, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Item item = overlapBuffer[i].GetComponentInParent<Item>();
                if (item == null || item.Location != ItemLocation.World) continue;

                if (FoodContainer.FindFor(item) is ServingFoodContainer dish) return dish;
            }
            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, occupiedRadius);
        }
    }
}
