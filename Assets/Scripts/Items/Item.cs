using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Items
{
    /// <summary>ไอเทมชิ้นนี้อยู่ที่ไหนในตอนนี้</summary>
    public enum ItemLocation
    {
        World,
        Hand
    }

    /// <summary>ร่างในโลกของไอเทมหนึ่งชิ้น ถือ ItemInstance ที่เป็นตัวตนของมัน และจด/คืนสถานะของ component ให้ตัวตนนั้น</summary>
    [DisallowMultipleComponent]
    public class Item : MonoBehaviour
    {
        [Tooltip("ข้อมูลประจำชนิดของไอเทมนี้")]
        [SerializeField] private ItemDefinitionSO definition;

        private ItemInstance instance;

        // ที่มาดูจากฉากที่เกิด ไม่ใช่ที่ที่ถูกหยิบ ของที่พาออกไปเองจึงไม่กลายเป็นของที่หาได้ข้างนอก
        private bool bornOutside;

        public ItemDefinitionSO Definition => definition;

        /// <summary>ตัวตนของไอเทม ของที่ไม่ได้สร้างจากที่เก็บได้ตัวตนใหม่ตอนถูกถามครั้งแรก</summary>
        public ItemInstance Instance => instance ??= CreateInstance();

        /// <summary>ไอเทมชิ้นนี้อยู่ที่ไหนในตอนนี้</summary>
        public ItemLocation Location { get; private set; } = ItemLocation.World;

        private void Awake()
        {
            bornOutside = LocationLoader.Instance != null && LocationLoader.Instance.IsOutside(gameObject.scene);
        }

        private ItemInstance CreateInstance()
        {
            var created = new ItemInstance(definition);
            if (bornOutside) created.AddFlag(ItemFlags.FoundOutside);
            return created;
        }

        /// <summary>เรียกจาก EquippableItem เท่านั้น เมื่อไอเทมย้ายที่อยู่</summary>
        public void SetLocation(ItemLocation next)
        {
            Location = next;
        }

        /// <summary>สร้างวัตถุของไอเทมชิ้นนี้จาก prefab ของชนิดมัน แล้วคืนตัวตนกับสถานะที่จดไว้ให้ null ถ้าชนิดนี้ไม่มี prefab</summary>
        public static Item Spawn(ItemInstance existing, Vector3 position, Quaternion rotation)
        {
            if (existing.Definition.Prefab == null)
            {
                Debug.LogWarning($"[Item] {existing.Definition.ItemId} ยังไม่ได้ตั้ง Prefab สร้างวัตถุไม่ได้", existing.Definition);
                return null;
            }

            Item spawned = Instantiate(existing.Definition.Prefab, position, rotation).GetComponent<Item>();
            spawned.Restore(existing);
            return spawned;
        }

        /// <summary>จดสถานะของ component ทุกตัวลงตัวตน แล้วคืนตัวตนนั้น ใช้ก่อนไอเทมกลายเป็นข้อมูล</summary>
        public ItemInstance Capture()
        {
            var seen = new HashSet<string>();
            foreach (ISavableState savable in GetComponentsInChildren<ISavableState>(true))
            {
                if (!seen.Add(savable.StateKey))
                {
                    Debug.LogWarning($"[Item] {name} มีสถานะรหัส '{savable.StateKey}' ซ้ำ ตัวหลังจะถูกข้าม", this);
                    continue;
                }

                Instance.SetState(savable.StateKey, savable.CaptureState());
            }
            return Instance;
        }

        /// <summary>รับตัวตนเดิมคืนให้วัตถุที่เพิ่งสร้างจาก prefab แล้วคืนสถานะให้ component ที่มีรหัสตรงกัน</summary>
        public void Restore(ItemInstance existing)
        {
            instance = existing;
            foreach (ISavableState savable in GetComponentsInChildren<ISavableState>(true))
            {
                if (existing.TryGetState(savable.StateKey, out string data)) savable.RestoreState(data);
            }
        }
    }
}
