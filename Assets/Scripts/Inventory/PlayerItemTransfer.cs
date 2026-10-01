using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;
using ProjectCook.Items;

namespace ProjectCook.Inventory
{
    /// <summary>จุดเดียวที่ย้ายของระหว่างฉากกับตัวผู้เล่น: หยิบ (เก็บ สวมกระเป๋า ถือ) และวางลงฉาก</summary>
    // ของที่เก็บเป็นข้อมูล ของในฉากและในมือเป็นวัตถุจริง ข้ามกันได้ผ่าน CaptureStates กับ Spawn ในไฟล์นี้เท่านั้น
    public class PlayerItemTransfer : SceneSingleton<PlayerItemTransfer>, IPickUpHandler
    {
        public bool TryPickUp(Collider target)
        {
            StorableItem storable = target.GetComponentInParent<StorableItem>();
            if (storable != null) return TryStore(storable);

            BagItem bag = target.GetComponentInParent<BagItem>();
            if (bag != null) return TryWearBag(bag);

            // กระเป๋าเต็มไม่ถอยมาถือแทน ถือได้เฉพาะของที่เก็บไม่ได้
            EquippableItem equippable = target.GetComponentInParent<EquippableItem>();
            return equippable != null && EquipmentManager.Instance != null && EquipmentManager.Instance.TryEquip(equippable);
        }

        /// <summary>เอาของออกจากที่เก็บใดก็ได้ (ตัวผู้เล่น ตู้เย็น) วางลงฉากตรงจุดที่เล็ง</summary>
        public bool DropFromStorage(ItemStorage storage, InventoryItem item)
        {
            if (!TryGetPlacePose(out Vector3 position, out Quaternion rotation) || !HasPrefab(item.Definition)) return false;
            if (!storage.Remove(item)) return false;

            Spawn(item.Definition, item.States, position, rotation);
            return true;
        }

        /// <summary>ถอดกระเป๋าที่สวมอยู่วางลงฉาก ของข้างในติดไปกับกระเป๋า</summary>
        public bool DropBag()
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            BagDefinitionSO bag = inventory.WornBag;
            if (bag == null || !TryGetPlacePose(out Vector3 position, out Quaternion rotation) || !HasPrefab(bag)) return false;

            List<ItemGrid> contents = inventory.TakeOffBag();
            Spawn(bag, null, position, rotation).GetComponent<BagItem>().PutGrids(contents);
            return true;
        }

        public bool DropFromHand(HandSide side)
        {
            EquipmentManager equipment = EquipmentManager.Instance;
            EquippableItem held = equipment != null ? equipment.GetHeld(side) : null;
            if (held == null || !TryGetPlacePose(out Vector3 position, out Quaternion rotation)) return false;

            return equipment.Release(held, position, rotation);
        }

        private bool TryStore(StorableItem storable)
        {
            ItemDefinitionSO definition = storable.Definition;
            if (definition == null)
            {
                Debug.LogWarning($"[PlayerItemTransfer] {storable.name} ยังไม่ได้ตั้ง Definition ของ Item เก็บไม่ได้", storable);
                return false;
            }

            if (!PlayerInventory.Instance.Storage.TryAdd(new InventoryItem(definition, storable.CaptureStates())))
            {
                Debug.Log($"[PlayerItemTransfer] กระเป๋าเต็ม เก็บ {definition.ItemId} ไม่ได้");
                return false;
            }

            Destroy(storable.gameObject);
            return true;
        }

        private bool TryWearBag(BagItem bag)
        {
            PlayerInventory inventory = PlayerInventory.Instance;

            // เช็คก่อนรับตาราง ไม่งั้นของในกระเป๋าใบนี้จะหลุดมือไปโดยไม่ได้สวม
            if (inventory.WornBag != null)
            {
                Debug.Log("[PlayerItemTransfer] สวมกระเป๋าอยู่แล้ว ต้องถอดใบเดิมก่อน");
                return false;
            }

            BagDefinitionSO definition = bag.Definition;
            if (definition == null)
            {
                Debug.LogWarning($"[PlayerItemTransfer] {bag.name} ต้องตั้ง Definition ของ Item เป็น BagDefinitionSO", bag);
                return false;
            }

            inventory.WearBag(definition, bag.TakeGrids());
            Destroy(bag.gameObject);
            return true;
        }

        /// <summary>สร้างวัตถุจากข้อมูล แล้วคืนสถานะที่จดไว้ (ถ้ามี)</summary>
        private static GameObject Spawn(ItemDefinitionSO definition, IReadOnlyList<ComponentState> states, Vector3 position, Quaternion rotation)
        {
            GameObject spawned = Instantiate(definition.Prefab, position, rotation);
            if (states != null && spawned.TryGetComponent(out StorableItem storable))
            {
                storable.RestoreStates(states);
            }
            return spawned;
        }

        private static bool TryGetPlacePose(out Vector3 position, out Quaternion rotation)
        {
            if (PlayerAiming.Instance == null)
            {
                position = default;
                rotation = default;
                return false;
            }

            PlayerAiming.Instance.GetPlacePose(out position, out rotation);
            return true;
        }

        private static bool HasPrefab(ItemDefinitionSO definition)
        {
            if (definition.Prefab != null) return true;

            Debug.LogWarning($"[PlayerItemTransfer] {definition.ItemId} ยังไม่ได้ตั้ง Prefab วางลงฉากไม่ได้", definition);
            return false;
        }
    }
}
