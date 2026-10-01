using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Inventory
{
    /// <summary>ที่เก็บของหนึ่งชุด (ตัวผู้เล่น ตู้เย็น ลัง) ถือตารางย่อยหลายตาราง และเป็นทางเดียวที่ใส่ เอาออก หรือย้ายของระหว่างที่เก็บ</summary>
    public class ItemStorage
    {
        // เรียงตามลำดับที่ของไหลลงตอนใส่แบบหาที่ว่างเอง
        private readonly List<ItemGrid> grids = new List<ItemGrid>();

        public IReadOnlyList<ItemGrid> Grids => grids;

        /// <summary>ของในตารางใดตารางหนึ่งเปลี่ยน หรือชุดตารางถูกเปลี่ยน</summary>
        public event Action Changed;

        public ItemStorage(IEnumerable<ItemGrid> initial)
        {
            grids.AddRange(initial);
        }

        public bool Owns(ItemGrid grid) => grids.Contains(grid);

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

        public bool Remove(InventoryItem item)
        {
            ItemGrid source = GridOf(item);
            if (source == null) return false;

            source.Remove(item);
            Changed?.Invoke();
            return true;
        }

        /// <summary>ย้ายของไปตำแหน่งที่ระบุ ในที่เก็บเดิมหรือที่เก็บอื่น</summary>
        public static bool TryMove(InventoryItem item, ItemStorage from, ItemStorage to, ItemGrid target, Vector2Int position, bool rotated)
        {
            ItemGrid source = from.GridOf(item);
            if (source == null || !to.Owns(target) || !target.CanPlace(item, position, rotated)) return false;

            if (source != target)
            {
                source.Remove(item);
            }
            target.TryPlace(item, position, rotated);
            from.Changed?.Invoke();
            if (to != from) to.Changed?.Invoke();
            return true;
        }

        /// <summary>ย้ายของไปที่ว่างแรกของอีกที่เก็บ ถ้าไม่มีที่ว่าง ของอยู่ที่เดิม</summary>
        public static bool TryTransfer(InventoryItem item, ItemStorage from, ItemStorage to)
        {
            ItemGrid source = from.GridOf(item);
            if (source == null || from == to) return false;

            // ต้องเอาออกก่อนหาที่ใหม่ เพราะการหาที่ใหม่เขียนทับตำแหน่งที่ตารางเดิมใช้ลบช่อง
            Vector2Int position = item.Position;
            bool rotated = item.IsRotated;
            source.Remove(item);

            if (!to.TryAdd(item))
            {
                source.TryPlace(item, position, rotated);
                return false;
            }

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
