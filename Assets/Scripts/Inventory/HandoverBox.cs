using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Items;

namespace ProjectCook.Inventory
{
    /// <summary>กล่องให้ของ: ผู้เล่นใส่ของอะไรก็ได้ (รวมของในมือ) แล้วกดยืนยัน ปิดโดยไม่ยืนยัน = ของกลับไปหาผู้เล่น</summary>
    [DisallowMultipleComponent]
    public class HandoverBox : MonoBehaviour, IStorageRules
    {
        [Tooltip("ชื่อบนหัวหน้าต่าง")]
        [SerializeField] private string title = "GIVE";

        [SerializeField] private CompartmentLayout[] compartments =
        {
            new CompartmentLayout { size = new Vector2Int(4, 4) }
        };

        private ItemStorage storage;
        private Action<List<ItemInstance>> confirmed;
        private Action closed;

        private void Awake()
        {
            storage = new ItemStorage(ItemGrid.FromLayouts(compartments), this);
        }

        /// <summary>เปิดกล่องคู่กับกระเป๋าผู้เล่น กดยืนยันแล้ว onConfirmed ได้ของทั้งหมดในกล่อง onClosed ถูกเรียกเสมอหลังหน้าต่างปิด</summary>
        public void Open(Action<List<ItemInstance>> onConfirmed, Action onClosed)
        {
            confirmed = onConfirmed;
            closed = onClosed;
            storage.RequestOpen(title, Confirm, Close);
        }

        private void Confirm() => confirmed?.Invoke(TakeAll());

        // ยืนยันแล้วกล่องว่าง ที่เหลือคือของที่ผู้เล่นใส่แล้วเปลี่ยนใจ
        private void Close()
        {
            foreach (ItemInstance item in TakeAll()) PlayerItemTransfer.Instance.GiveBack(item);

            Action onClosed = closed;
            confirmed = null;
            closed = null;
            onClosed?.Invoke();
        }

        private List<ItemInstance> TakeAll()
        {
            var items = new List<ItemInstance>();
            foreach (ItemGrid grid in storage.Grids)
            {
                foreach (InventoryItem item in grid.Items) items.Add(item.Instance);
            }
            storage.Clear();
            return items;
        }

        public bool CanRelease(InventoryItem item) => true;

        public bool CanAccept(InventoryItem item) => true;

        public bool AcceptsHeldItems => true;

        public int PriceOf(InventoryItem item) => 0;

        public void OnReleased(InventoryItem item)
        {
        }
    }
}
