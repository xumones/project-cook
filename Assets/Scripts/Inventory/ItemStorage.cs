using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Inventory
{
    /// <summary>ที่เก็บของหนึ่งชุด (ตัวผู้เล่น ตู้เย็น ร้านค้า) ถือตารางย่อยหลายตาราง และเป็นทางเดียวที่ใส่ เอาออก หรือย้ายของระหว่างที่เก็บ</summary>
    public class ItemStorage
    {
        // เรียงตามลำดับที่ของไหลลงตอนใส่แบบหาที่ว่างเอง
        private readonly List<ItemGrid> grids = new List<ItemGrid>();

        // null = ของเข้าออกได้อิสระ
        private readonly IStorageRules rules;

        public IReadOnlyList<ItemGrid> Grids => grids;

        /// <summary>ของในตารางใดตารางหนึ่งเปลี่ยน หรือชุดตารางถูกเปลี่ยน</summary>
        public event Action Changed;

        /// <summary>มีคนขอเปิดหน้าต่างของที่เก็บนี้คู่กับกระเป๋าผู้เล่น (ตู้เย็น ร้าน) UI เป็นคนเปิด ส่งชื่อหัวหน้าต่างมาด้วย</summary>
        public static event Action<ItemStorage, string> OpenRequested;

        public ItemStorage(IEnumerable<ItemGrid> initial, IStorageRules rules = null)
        {
            grids.AddRange(initial);
            this.rules = rules;
        }

        public void RequestOpen(string title) => OpenRequested?.Invoke(this, title);

        public bool Owns(ItemGrid grid) => grids.Contains(grid);

        /// <summary>ของชิ้นนี้ออกจากที่เก็บนี้ไปที่อื่นหรือลงฉากได้ไหม</summary>
        public bool CanRelease(InventoryItem item) => rules == null || rules.CanRelease(item);

        /// <summary>ราคาที่ต้องจ่ายถ้าเอาของชิ้นนี้ออก 0 = ฟรี</summary>
        public int PriceOf(InventoryItem item) => rules != null ? rules.PriceOf(item) : 0;

        /// <summary>กฎของทั้งสองที่เก็บยอมให้ย้ายของชิ้นนี้ไหม ไม่ได้เช็คที่ว่าง</summary>
        public static bool CanMove(InventoryItem item, ItemStorage from, ItemStorage to) =>
            from == to || (from.CanRelease(item) && (to.rules == null || to.rules.CanAccept(item)));

        /// <summary>เปลี่ยนชุดตารางทั้งหมด เช่น ผู้เล่นสวมหรือถอดกระเป๋า</summary>
        public void SetGrids(IEnumerable<ItemGrid> next)
        {
            grids.Clear();
            grids.AddRange(next);
            Changed?.Invoke();
        }

        /// <summary>วางของลงที่ว่างแรกตามลำดับตาราง คืน false ถ้าเต็มทุกตาราง</summary>
        public bool TryAdd(InventoryItem item)
        {
            foreach (ItemGrid grid in grids)
            {
                if (!grid.TryAdd(item)) continue;

                Changed?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>เอาของออกไปที่ที่ไม่ใช่ที่เก็บ (ลงฉาก สวมกระเป๋า) ต้องผ่านกฎของที่เก็บ</summary>
        public bool Remove(InventoryItem item)
        {
            ItemGrid source = GridOf(item);
            if (source == null || !CanRelease(item)) return false;

            source.Remove(item);
            rules?.OnReleased(item);
            Changed?.Invoke();
            return true;
        }

        public void Clear()
        {
            foreach (ItemGrid grid in grids)
            {
                for (int i = grid.Items.Count - 1; i >= 0; i--)
                {
                    grid.Remove(grid.Items[i]);
                }
            }
            Changed?.Invoke();
        }

        /// <summary>ย้ายของไปตำแหน่งที่ระบุ ในที่เก็บเดิมหรือที่เก็บอื่น</summary>
        public static bool TryMove(InventoryItem item, ItemStorage from, ItemStorage to, ItemGrid target, Vector2Int position, bool rotated)
        {
            ItemGrid source = from.GridOf(item);
            if (source == null || !to.Owns(target) || !CanMove(item, from, to) || !target.CanPlace(item, position, rotated)) return false;

            if (source != target)
            {
                source.Remove(item);
            }
            target.TryPlace(item, position, rotated);
            if (to != from) from.rules?.OnReleased(item);
            from.Changed?.Invoke();
            if (to != from) to.Changed?.Invoke();
            return true;
        }

        /// <summary>ย้ายของไปที่ว่างแรกของอีกที่เก็บ ถ้าไม่มีที่ว่าง ของอยู่ที่เดิม</summary>
        public static bool TryTransfer(InventoryItem item, ItemStorage from, ItemStorage to)
        {
            ItemGrid source = from.GridOf(item);
            if (source == null || from == to || !CanMove(item, from, to)) return false;

            // ต้องเอาออกก่อนหาที่ใหม่ เพราะการหาที่ใหม่เขียนทับตำแหน่งที่ตารางเดิมใช้ลบช่อง
            Vector2Int position = item.Position;
            bool rotated = item.IsRotated;
            source.Remove(item);

            if (!to.TryAdd(item))
            {
                source.TryPlace(item, position, rotated);
                return false;
            }

            from.rules?.OnReleased(item);
            from.Changed?.Invoke();
            return true;
        }

        private ItemGrid GridOf(InventoryItem item)
        {
            foreach (ItemGrid grid in grids)
            {
                if (grid.Contains(item)) return grid;
            }
            return null;
        }
    }
}
