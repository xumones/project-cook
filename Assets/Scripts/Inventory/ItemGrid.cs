using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Inventory
{
    /// <summary>ช่องย่อยหนึ่งช่องของที่เก็บของ (ตัวผู้เล่น กระเป๋า)</summary>
    [Serializable]
    public struct CompartmentLayout
    {
        [Tooltip("จำนวนช่อง (กว้าง x สูง)")]
        public Vector2Int size;

        [Tooltip("ตำแหน่งบนจอนับเป็นช่องจากมุมซ้ายบน ใส่ทศนิยมเพื่อเว้นช่องไฟระหว่างช่องย่อยได้")]
        public Vector2 position;
    }

    /// <summary>ตารางช่องเก็บของหนึ่งตาราง ช่อง (0,0) อยู่มุมซ้ายบน x ไปทางขวา y ลงล่าง</summary>
    public class ItemGrid
    {
        private readonly InventoryItem[,] cells;
        private readonly List<InventoryItem> items = new List<InventoryItem>();

        public Vector2Int Size { get; }

        /// <summary>ตำแหน่งของตารางนี้เทียบกับตารางอื่นในที่เก็บของเดียวกัน นับเป็นช่อง</summary>
        public Vector2 LayoutPosition { get; }

        public IReadOnlyList<InventoryItem> Items => items;

        public ItemGrid(Vector2Int size, Vector2 layoutPosition = default)
        {
            Size = Vector2Int.Max(size, Vector2Int.one);
            LayoutPosition = layoutPosition;
            cells = new InventoryItem[Size.x, Size.y];
        }

        public static List<ItemGrid> FromLayouts(IReadOnlyList<CompartmentLayout> layouts)
        {
            var grids = new List<ItemGrid>(layouts.Count);
            foreach (CompartmentLayout layout in layouts)
            {
                grids.Add(new ItemGrid(layout.size, layout.position));
            }
            return grids;
        }

        public InventoryItem GetItemAt(Vector2Int cell) => IsInside(cell) ? cells[cell.x, cell.y] : null;

        public bool Contains(InventoryItem item) => items.Contains(item);

        /// <summary>วางของที่ตำแหน่งนี้ได้หรือไม่ ช่องที่ของชิ้นเดียวกันทับอยู่ไม่นับว่าชน เพื่อให้ย้ายในตารางเดิมได้</summary>
        public bool CanPlace(InventoryItem item, Vector2Int position, bool rotated)
        {
            if (item == null) return false;

            Vector2Int size = item.SizeWhen(rotated);
            if (!IsInside(position) || !IsInside(position + size - Vector2Int.one)) return false;

            for (int y = position.y; y < position.y + size.y; y++)
            {
                for (int x = position.x; x < position.x + size.x; x++)
                {
                    InventoryItem occupant = cells[x, y];
                    if (occupant != null && occupant != item) return false;
                }
            }
            return true;
        }

        /// <summary>วางของลงตำแหน่งที่ระบุ ถ้าของอยู่ในตารางนี้อยู่แล้วถือเป็นการย้าย</summary>
        public bool TryPlace(InventoryItem item, Vector2Int position, bool rotated)
        {
            if (!CanPlace(item, position, rotated)) return false;

            if (items.Contains(item))
            {
                SetCells(item, null);
            }
            else
            {
                items.Add(item);
            }

            item.Position = position;
            item.IsRotated = rotated;
            SetCells(item, item);
            return true;
        }

        /// <summary>วางของลงที่ว่างแรกที่เจอ ไล่จากซ้ายบน ลองแนวปกติก่อนแล้วจึงลองหมุน</summary>
        public bool TryAdd(InventoryItem item)
        {
            if (item == null || items.Contains(item)) return false;

            if (TryFindSpace(item, false, out Vector2Int position)) return TryPlace(item, position, false);
            if (TryFindSpace(item, true, out position)) return TryPlace(item, position, true);
            return false;
        }

        public bool Remove(InventoryItem item)
        {
            if (item == null || !items.Remove(item)) return false;

            SetCells(item, null);
            return true;
        }

        private bool TryFindSpace(InventoryItem item, bool rotated, out Vector2Int position)
        {
            for (int y = 0; y < Size.y; y++)
            {
                for (int x = 0; x < Size.x; x++)
                {
                    position = new Vector2Int(x, y);
                    if (CanPlace(item, position, rotated)) return true;
                }
            }

            position = default;
            return false;
        }

        private bool IsInside(Vector2Int cell) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < Size.x && cell.y < Size.y;

        // ใช้ตำแหน่งและการหมุนปัจจุบันของ item จึงต้องล้างช่องเก่าก่อนเปลี่ยนตำแหน่ง
        private void SetCells(InventoryItem item, InventoryItem value)
        {
            Vector2Int size = item.Size;
            for (int y = item.Position.y; y < item.Position.y + size.y; y++)
            {
                for (int x = item.Position.x; x < item.Position.x + size.x; x++)
                {
                    cells[x, y] = value;
                }
            }
        }
    }
}
