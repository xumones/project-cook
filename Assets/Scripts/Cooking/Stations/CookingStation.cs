using UnityEngine;
using ProjectCook.Core;
using ProjectCook.Items;
using ProjectCook.Stations;

namespace ProjectCook.Cooking
{
    /// <summary>สถานีทำอาหาร (กระทะ หม้อ เตาอบ ฯลฯ) เข้าได้เฉพาะตอนมือว่าง</summary>
    public abstract class CookingStation : Station
    {
        // เงื่อนไขเดียวกับ Interact: มือไม่ว่าง = เข้าไม่ได้ จึงไม่ต้องบอกว่าใช้ได้
        public override AimHint GetAimHint() => IsHoldingAnything ? AimHint.None : AimHint.Use;

        public override void Interact()
        {
            // ของในมือใช้ในสถานีไม่ได้และบังกล้อง ต้องวางก่อน (โหมด Station ปิดการหยิบอยู่แล้ว จึงกันแค่ตอนเข้าพอ)
            if (IsHoldingAnything)
            {
                Debug.Log($"[CookingStation] เข้า {name} ไม่ได้: ต้องวางของในมือก่อน");
                return;
            }

            EnterStation();
        }

        private static bool IsHoldingAnything
        {
            get
            {
                EquipmentManager equipment = EquipmentManager.Instance;
                return equipment != null && equipment.IsHoldingAnything;
            }
        }
    }
}
