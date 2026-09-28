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

    /// <summary>ช่องของที่ติดตัว (มือ กระเป๋าที่สวม): แสดงรหัสของในช่อง และลากออกนอกหน้าต่างเพื่อวางลงฉาก</summary>
    // ต้องมี Graphic ที่ Raycast Target ไม่งั้นเริ่มลากจากช่องนี้ไม่ได้
    public class ItemSlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private EquipSlot slot;

        [Tooltip("ข้อความแสดงรหัสของในช่อง")]
        [SerializeField] private TMP_Text itemLabel;

        private ItemDragController dragController;

        private HandSide Hand => slot == EquipSlot.LeftHand ? HandSide.Left : HandSide.Right;

        private void Awake()
        {
            dragController = GetComponentInParent<ItemDragController>();
        }

        public void Refresh()
        {
            itemLabel.text = CurrentItemId() ?? string.Empty;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            string id = CurrentItemId();
            if (id == null) return;

            dragController.BeginDropOnlyDrag(id, ((RectTransform)transform).rect.size, Drop, eventData);
        }

        public void OnDrag(PointerEventData eventData) => dragController.Drag(eventData);

        public void OnEndDrag(PointerEventData eventData) => dragController.EndDrag(eventData);

        /// <summary>รหัสของในช่อง null เมื่อช่องว่าง</summary>
        private string CurrentItemId()
        {
            if (slot == EquipSlot.Bag)
            {
                BagDefinitionSO bag = PlayerInventory.Instance != null ? PlayerInventory.Instance.WornBag : null;
                return bag != null ? bag.ItemId : null;
            }

            EquippableItem held = EquipmentManager.Instance != null ? EquipmentManager.Instance.GetHeld(Hand) : null;
            if (held == null) return null;

            // ของที่ยังไม่ได้ตั้ง Definition แสดงชื่อ GameObject แทน จะได้ยังเห็นว่าถืออะไรอยู่
            ItemDefinitionSO definition = held.GetComponent<Item>().Definition;
            return definition != null ? definition.ItemId : held.name;
        }

        private void Drop()
        {
            if (slot == EquipSlot.Bag)
            {
                PlayerItemTransfer.Instance.DropBag();
            }
            else
            {
                PlayerItemTransfer.Instance.DropFromHand(Hand);
            }
        }
    }
}
