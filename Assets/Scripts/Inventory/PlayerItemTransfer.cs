using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;
using ProjectCook.Items;

namespace ProjectCook.Inventory
{
    /// <summary>จุดเดียวที่ย้ายของระหว่างฉากกับตัวผู้เล่น: หยิบ (เก็บ สวมกระเป๋า ถือ) และวางลงฉาก</summary>
    // ตัวตนของไอเทม (ItemInstance) เป็นตัวเดิมตลอดทาง ข้ามระหว่างวัตถุในฉากกับข้อมูลในที่เก็บผ่าน Item.Capture กับ Spawn ในไฟล์นี้เท่านั้น
    public class PlayerItemTransfer : SceneSingleton<PlayerItemTransfer>, IPickUpHandler
    {
        [Header("Placing")]
        [Tooltip("ระยะวางของสูงสุดจากตาผู้เล่น (เมตร)")]
        [SerializeField] private float placeDistance = 1f;

        [Tooltip("ความเร็วสูงสุด (เมตร/วินาที) ที่ฟิสิกส์ใช้ดันของที่วางแล้วทับสิ่งอื่นให้แยกออก ยิ่งต่ำยิ่งค่อยๆ ไหลออกแทนการดีดกระเด็น")]
        [SerializeField] private float maxDepenetrationVelocity = 2f;

        public bool TryPickUp(Collider target)
        {
            Item item = target.GetComponentInParent<Item>();
            if (item == null || !TryTake(target)) return false;

            MessageBus<ItemCollected>.Publish(new ItemCollected(item.Instance));
            return true;
        }

        private bool TryTake(Collider target)
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
            if (!HasPrefab(item.Definition) || !TryGetPlacePose(item.Definition.Prefab, out Vector3 position, out Quaternion rotation, out RaycastHit surface)) return false;
            if (!storage.Remove(item)) return false;

            FinishPlacing(Spawn(item.Instance, position, rotation), surface);
            return true;
        }

        /// <summary>กระเป๋าในที่เก็บชิ้นนี้สวมได้ไหม (ยังไม่ได้สวมใบไหน และที่เก็บยอมให้เอาออก เช่น ร้านต้องมีเงินพอ)</summary>
        public static bool CanWearFromStorage(ItemStorage storage, InventoryItem item) =>
            item.Definition is BagDefinitionSO
            && PlayerInventory.Instance != null && PlayerInventory.Instance.WornBag == null
            && storage.CanRelease(item);

        /// <summary>สวมกระเป๋าที่อยู่ในที่เก็บ กระเป๋าในที่เก็บข้างในว่างเสมอ จึงสร้างช่องใหม่ตามแบบ</summary>
        public bool TryWearFromStorage(ItemStorage storage, InventoryItem item)
        {
            if (!CanWearFromStorage(storage, item) || !storage.Remove(item)) return false;

            var bag = (BagDefinitionSO)item.Definition;
            PlayerInventory.Instance.WearBag(item.Instance, ItemGrid.FromLayouts(bag.Compartments));
            return true;
        }

        /// <summary>ถอดกระเป๋าที่สวมอยู่วางลงฉาก ของข้างในติดไปกับกระเป๋า</summary>
        public bool DropBag()
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            ItemInstance bag = inventory.WornBag;
            if (bag == null || !HasPrefab(bag.Definition) || !TryGetPlacePose(bag.Definition.Prefab, out Vector3 position, out Quaternion rotation, out RaycastHit surface)) return false;

            List<ItemGrid> contents = inventory.TakeOffBag();
            GameObject spawned = Spawn(bag, position, rotation);
            spawned.GetComponent<BagItem>().PutGrids(contents);
            FinishPlacing(spawned, surface);
            return true;
        }

        public bool DropFromHand(HandSide side)
        {
            EquipmentManager equipment = EquipmentManager.Instance;
            EquippableItem held = equipment != null ? equipment.GetHeld(side) : null;
            if (held == null || !TryGetPlacePose(held.gameObject, out Vector3 position, out Quaternion rotation, out RaycastHit surface)) return false;
            if (!equipment.Release(held, position, rotation)) return false;

            FinishPlacing(held.gameObject, surface);
            return true;
        }

        private bool TryStore(StorableItem storable)
        {
            Item item = storable.Item;
            if (item.Definition == null)
            {
                Debug.LogWarning($"[PlayerItemTransfer] {storable.name} ยังไม่ได้ตั้ง Definition ของ Item เก็บไม่ได้", storable);
                return false;
            }

            if (!PlayerInventory.Instance.Storage.TryAdd(new InventoryItem(item.Capture())))
            {
                Debug.Log($"[PlayerItemTransfer] กระเป๋าเต็ม เก็บ {item.Definition.ItemId} ไม่ได้");
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

            if (bag.Definition == null)
            {
                Debug.LogWarning($"[PlayerItemTransfer] {bag.name} ต้องตั้ง Definition ของ Item เป็น BagDefinitionSO", bag);
                return false;
            }

            inventory.WearBag(bag.GetComponent<Item>().Capture(), bag.TakeGrids());
            Destroy(bag.gameObject);
            return true;
        }

        /// <summary>สร้างวัตถุของไอเทมชิ้นนี้ แล้วคืนตัวตนกับสถานะที่จดไว้ให้</summary>
        private GameObject Spawn(ItemInstance instance, Vector3 position, Quaternion rotation)
        {
            GameObject spawned = Instantiate(instance.Definition.Prefab, position, rotation);
            spawned.GetComponent<Item>().Restore(instance);
            return spawned;
        }

        /// <summary>หาท่าวางตรงที่เล็งที่ของรูปทรงเดียวกับ shape ลงได้โดยไม่ทับอะไร false = ไม่มีที่ว่าง</summary>
        private bool TryGetPlacePose(GameObject shape, out Vector3 position, out Quaternion rotation, out RaycastHit surface)
        {
            if (PlayerAiming.Instance == null)
            {
                position = default;
                rotation = default;
                surface = default;
                return false;
            }

            if (ItemPlacement.TryFindPose(shape, PlayerAiming.Instance.AimRay, placeDistance, out position, out rotation, out surface)) return true;

            Debug.Log($"[PlayerItemTransfer] ไม่มีที่ว่างพอวาง {shape.name} ตรงที่เล็ง");
            return false;
        }

        private void FinishPlacing(GameObject placed, RaycastHit surface)
        {
            // กันพลาดชั้นสอง: ถ้ายังทับอะไรอยู่นิดหน่อย ให้ค่อยๆ ไหลออกแทนการดีดกระเด็น
            foreach (Rigidbody body in placed.GetComponentsInChildren<Rigidbody>())
            {
                body.maxDepenetrationVelocity = maxDepenetrationVelocity;
            }

            foreach (IPlacementReceiver receiver in placed.GetComponentsInChildren<IPlacementReceiver>())
            {
                receiver.OnPlaced(surface);
            }
        }

        private static bool HasPrefab(ItemDefinitionSO definition)
        {
            if (definition.Prefab != null) return true;

            Debug.LogWarning($"[PlayerItemTransfer] {definition.ItemId} ยังไม่ได้ตั้ง Prefab วางลงฉากไม่ได้", definition);
            return false;
        }
    }
}
