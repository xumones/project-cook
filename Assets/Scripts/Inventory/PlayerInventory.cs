using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Inventory
{
    /// <summary>ของที่ผู้เล่นเก็บไว้เป็นข้อมูล: ช่องบนตัว กระเป๋าที่สวม และตารางของกระเป๋า</summary>
    public class PlayerInventory : SceneSingleton<PlayerInventory>
    {
        [Header("Body")]
        [Tooltip("ช่องย่อยบนตัวผู้เล่น มีอยู่เสมอไม่ว่าจะสวมกระเป๋าหรือไม่")]
        [SerializeField] private CompartmentLayout[] bodyCompartments =
        {
            new CompartmentLayout { size = new Vector2Int(4, 1) }
        };

        private List<ItemGrid> bodyGrids;
        private List<ItemGrid> bagGrids = new List<ItemGrid>();

        // บนตัวก่อนแล้วจึงกระเป๋า เป็นลำดับที่ของไหลลงตอนเก็บ
        private readonly List<ItemGrid> grids = new List<ItemGrid>();

        public IReadOnlyList<ItemGrid> BodyGrids => bodyGrids;

        /// <summary>ตารางของกระเป๋าที่สวมอยู่ ว่างเมื่อไม่ได้สวม เปลี่ยนเป็นชุดใหม่ทุกครั้งที่สวมหรือถอด</summary>
        public IReadOnlyList<ItemGrid> BagGrids => bagGrids;

        public BagDefinitionSO WornBag { get; private set; }

        /// <summary>ของในตารางใดตารางหนึ่งเปลี่ยน หรือสวม/ถอดกระเป๋า</summary>
        public event Action Changed;

        protected override void Awake()
        {
            base.Awake();

            bodyGrids = ItemGrid.FromLayouts(bodyCompartments);
            RebuildGridOrder();
        }

        /// <summary>วางของลงที่ว่างแรก บนตัวก่อนแล้วจึงกระเป๋า คืน false ถ้าเต็มทุกตาราง</summary>
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

        /// <summary>ย้ายของไปตำแหน่งใหม่ ในตารางเดิมหรือตารางอื่นของผู้เล่น</summary>
        public bool TryMove(InventoryItem item, ItemGrid target, Vector2Int position, bool rotated)
        {
            ItemGrid source = GridOf(item);
            if (source == null || !grids.Contains(target) || !target.CanPlace(item, position, rotated)) return false;

            if (source != target)
            {
                source.Remove(item);
            }
            target.TryPlace(item, position, rotated);
            Changed?.Invoke();
            return true;
        }

        /// <summary>สวมกระเป๋าพร้อมของข้างใน ผู้เรียกต้องเช็คก่อนว่ายังไม่ได้สวมใบไหน</summary>
        public void WearBag(BagDefinitionSO bag, List<ItemGrid> contents)
        {
            WornBag = bag;
            bagGrids = contents;
            RebuildGridOrder();
            Changed?.Invoke();
        }

        /// <summary>ถอดกระเป๋า คืนตารางพร้อมของข้างในให้ผู้เรียกฝากไว้กับกระเป๋าในฉาก</summary>
        public List<ItemGrid> TakeOffBag()
        {
            List<ItemGrid> contents = bagGrids;
            WornBag = null;
            bagGrids = new List<ItemGrid>();
            RebuildGridOrder();
            Changed?.Invoke();
            return contents;
        }

        private void RebuildGridOrder()
        {
            grids.Clear();
            grids.AddRange(bodyGrids);
            grids.AddRange(bagGrids);
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
