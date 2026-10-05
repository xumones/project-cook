using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Items;

namespace ProjectCook.Inventory
{
    /// <summary>ของในชุดตารางของที่เก็บหนึ่งชุด ในรูปที่เขียนลงไฟล์ save ได้ ขนาดตารางมาจากเจ้าของ ไม่ได้จดไว้</summary>
    [Serializable]
    public class StorageSaveData
    {
        [Serializable]
        private class Slot
        {
            public ItemSaveData item;
            public Vector2Int position;
            public bool rotated;
        }

        [Serializable]
        private class GridData
        {
            public List<Slot> slots = new List<Slot>();
        }

        [SerializeField] private List<GridData> grids = new List<GridData>();

        public static StorageSaveData From(IReadOnlyList<ItemGrid> source)
        {
            var saved = new StorageSaveData();
            foreach (ItemGrid grid in source)
            {
                var data = new GridData();
                foreach (InventoryItem item in grid.Items)
                {
                    data.slots.Add(new Slot { item = ItemSaveData.From(item.Instance), position = item.Position, rotated = item.IsRotated });
                }
                saved.grids.Add(data);
            }
            return saved;
        }

        /// <summary>วางของคืนลงตารางเปล่าที่เจ้าของสร้างไว้ ที่เดิมไม่พอ (ตารางเล็กลง) ให้หาที่ว่างแทน</summary>
        public void RestoreInto(IReadOnlyList<ItemGrid> target)
        {
            for (int i = 0; i < grids.Count && i < target.Count; i++)
            {
                foreach (Slot slot in grids[i].slots)
                {
                    ItemInstance instance = slot.item.ToInstance();
                    if (instance == null) continue;

                    var item = new InventoryItem(instance);
                    if (!target[i].TryPlace(item, slot.position, slot.rotated) && !target[i].TryAdd(item))
                    {
                        Debug.LogWarning($"[StorageSaveData] ไม่มีที่วาง {instance.Definition.ItemId} ตอนโหลด save ข้ามชิ้นนี้");
                    }
                }
            }
        }
    }
}
