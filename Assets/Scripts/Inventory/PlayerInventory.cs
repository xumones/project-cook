using System.Collections.Generic;
using System.Linq;
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

        /// <summary>ทุกตารางของผู้เล่น บนตัวก่อนแล้วจึงกระเป๋า เป็นลำดับที่ของไหลลงตอนเก็บ</summary>
        public ItemStorage Storage { get; private set; }

        public IReadOnlyList<ItemGrid> BodyGrids => bodyGrids;

        /// <summary>ตารางของกระเป๋าที่สวมอยู่ ว่างเมื่อไม่ได้สวม เปลี่ยนเป็นชุดใหม่ทุกครั้งที่สวมหรือถอด</summary>
        public IReadOnlyList<ItemGrid> BagGrids => bagGrids;

        public BagDefinitionSO WornBag { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            bodyGrids = ItemGrid.FromLayouts(bodyCompartments);
            Storage = new ItemStorage(bodyGrids);
        }

        /// <summary>สวมกระเป๋าพร้อมของข้างใน ผู้เรียกต้องเช็คก่อนว่ายังไม่ได้สวมใบไหน</summary>
        public void WearBag(BagDefinitionSO bag, List<ItemGrid> contents)
        {
            WornBag = bag;
            bagGrids = contents;
            Storage.SetGrids(bodyGrids.Concat(bagGrids));
        }

        /// <summary>ถอดกระเป๋า คืนตารางพร้อมของข้างในให้ผู้เรียกฝากไว้กับกระเป๋าในฉาก</summary>
        public List<ItemGrid> TakeOffBag()
        {
            List<ItemGrid> contents = bagGrids;
            WornBag = null;
            bagGrids = new List<ItemGrid>();
            Storage.SetGrids(bodyGrids);
            return contents;
        }
    }
}
