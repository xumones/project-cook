using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using ProjectCook.Inventory;
using ProjectCook.Items;

namespace ProjectCook.UI
{
    public enum EquipSlot
    {
        LeftHand,
        RightHand,
        Bag
    }

    /// <summary>ช่องของที่ติดตัว (มือ กระเป๋าที่สวม): แสดงชื่อของในช่อง ลากออกนอกหน้าต่างเพื่อวางลงฉาก ของในมือลากลงที่เก็บที่รับได้ และช่องกระเป๋ารับกระเป๋าที่ลากมาเพื่อสวม</summary>
    // ต้องมี Graphic ที่ Raycast Target ไม่งั้นเริ่มลากจากช่องนี้ไม่ได้
    public class ItemSlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private EquipSlot slot;

        [Tooltip("ข้อความแสดงชื่อของในช่อง")]
        [SerializeField] private TMP_Text itemLabel;

        private ItemDragController dragController;

        private HandSide Hand => slot == EquipSlot.LeftHand ? HandSide.Left : HandSide.Right;

        private void Awake()
        {
            dragController = GetComponentInParent<ItemDragController>();
        }

        public void Refresh()
        {
            itemLabel.text = CurrentItemName() ?? string.Empty;
        }

        /// <summary>รับของที่ลากมาจากที่เก็บได้ไหม ตอนนี้มีแค่ช่องกระเป๋าที่รับกระเป๋าไปสวม</summary>
        public bool CanTake(ItemStorage from, InventoryItem item) =>
            slot == EquipSlot.Bag && PlayerItemTransfer.CanWearFromStorage(from, item);

        public bool TryTake(ItemStorage from, InventoryItem item) =>
            slot == EquipSlot.Bag && PlayerItemTransfer.Instance.TryWearFromStorage(from, item);

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            string itemName = CurrentItemName();
            if (itemName == null) return;

            Vector2 size = ((RectTransform)transform).rect.size;
            if (slot == EquipSlot.Bag)
            {
                dragController.BeginDropOnlyDrag(itemName, size, () => PlayerItemTransfer.Instance.DropBag(), eventData);
                return;
            }

            // ของที่ยังไม่ได้ตั้ง Definition ไม่มีขนาดในตาราง ลากได้แค่วางลงฉาก
            Item held = EquipmentManager.Instance.GetHeld(Hand).GetComponent<Item>();
            if (held.Definition == null)
            {
                dragController.BeginDropOnlyDrag(itemName, size, () => PlayerItemTransfer.Instance.DropFromHand(Hand), eventData);
                return;
            }

            dragController.BeginHeldDrag(Hand, new InventoryItem(held.Instance), size, eventData);
        }

        public void OnDrag(PointerEventData eventData) => dragController.Drag(eventData);

        public void OnEndDrag(PointerEventData eventData) => dragController.EndDrag(eventData);

        /// <summary>ชื่อของในช่อง null เมื่อช่องว่าง</summary>
        private string CurrentItemName()
        {
            if (slot == EquipSlot.Bag)
            {
                ItemInstance bag = PlayerInventory.Instance != null ? PlayerInventory.Instance.WornBag : null;
                return bag != null ? bag.Definition.GetDisplayName() : null;
            }

            EquippableItem held = EquipmentManager.Instance != null ? EquipmentManager.Instance.GetHeld(Hand) : null;
            if (held == null) return null;

            // ของที่ยังไม่ได้ตั้ง Definition แสดงชื่อ GameObject แทน จะได้ยังเห็นว่าถืออะไรอยู่
            ItemDefinitionSO definition = held.GetComponent<Item>().Definition;
            return definition != null ? definition.GetDisplayName() : held.name;
        }
    }
}
