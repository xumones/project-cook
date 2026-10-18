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

        /// <summary>ย้ายของในมือเข้าที่เก็บตรงช่องที่ระบุ ของกลายเป็นข้อมูลพร้อมของข้างใน แล้ววัตถุถูกเก็บออกจากฉาก</summary>
        public bool StoreFromHand(HandSide side, ItemStorage storage, ItemGrid grid, Vector2Int position, bool rotated)
        {
            EquipmentManager equipment = EquipmentManager.Instance;
            EquippableItem held = equipment != null ? equipment.GetHeld(side) : null;
            if (held == null) return false;

            Item item = held.GetComponent<Item>();
            var stored = new InventoryItem(item.Instance);
            if (!storage.CanAcceptFromHand(stored) || !grid.CanPlace(stored, position, rotated)) return false;

            // จดตอนยังถืออยู่ ของข้างในยังถูกยึดกับภาชนะ ถ้าวางลงก่อน ฟิสิกส์อาจพาของหลุดออกจากรายการ
            item.Capture();
            if (!storage.TryPlace(stored, grid, position, rotated)) return false;

            equipment.RemoveHeld(held);
            item.Despawn();
            return true;
        }

        /// <summary>คืนไอเทมที่เป็นข้อมูลให้ผู้เล่น: เข้ากระเป๋าถ้าเก็บได้ ไม่งั้นเข้ามือที่ว่าง ไม่งั้นวางลงฉากตรงหน้า</summary>
        public void GiveBack(ItemInstance instance)
        {
            if (instance.Definition.IsStorable && PlayerInventory.Instance.Storage.TryAdd(new InventoryItem(instance))) return;

            EquipmentManager equipment = EquipmentManager.Instance;
            if (equipment != null && equipment.TryGetFreeHand(out HandSide side) && equipment.TrySpawnInHand(instance, side)) return;

            if (!HasPrefab(instance.Definition) || PlayerAiming.Instance == null) return;

            if (TryGetPlacePose(instance.Definition.Prefab, out Vector3 position, out Quaternion rotation, out RaycastHit surface))
            {
                FinishPlacing(Spawn(instance, position, rotation), surface);
                return;
            }

            // ตรงหน้าไม่มีที่ว่าง ปล่อยจากระดับตาให้ตกลงพื้นเอง ของที่วางไม่ชนตัวผู้เล่นอยู่แล้ว (ดู GameLayers)
            Spawn(instance, PlayerAiming.Instance.AimRay.origin, Quaternion.identity);
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

            item.Despawn();
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

            // เอาตารางออกก่อนจดสถานะ ตัวตนของกระเป๋าที่สวมจะได้ไม่พกสำเนาของข้างในที่ PlayerInventory ดูแลอยู่แล้ว
            List<ItemGrid> contents = bag.TakeGrids();
            Item item = bag.GetComponent<Item>();
            inventory.WearBag(item.Capture(), contents);
            item.Despawn();
            return true;
        }

        private static GameObject Spawn(ItemInstance instance, Vector3 position, Quaternion rotation) =>
            Item.Spawn(instance, position, rotation).gameObject;

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
