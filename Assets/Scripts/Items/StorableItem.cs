using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Items
{
    /// <summary>ทำให้ไอเทมเก็บเข้าที่เก็บได้ ตอนเก็บตัววัตถุถูกทำลาย เหลือแค่ตัวตน (ItemInstance) ของมัน</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Item))]
    public class StorableItem : MonoBehaviour, IAimHintProvider
    {
        public Item Item => GetComponent<Item>();

        public AimHint GetAimHint() => AimHint.PickUp;
    }
}
