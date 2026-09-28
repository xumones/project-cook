using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;
using ProjectCook.Items;

namespace ProjectCook.Inventory
{
    /// <summary>กระเป๋าที่วางอยู่ในฉาก สวมได้ด้วยปุ่มหยิบ และเก็บของข้างในไว้กับตัวระหว่างไม่ได้สวม</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Item))]
    public class BagItem : MonoBehaviour, IAimHintProvider
    {
        // null = ยังไม่เคยถูกสวม จะสร้างตารางเปล่าตามแบบตอนสวมครั้งแรก
        private List<ItemGrid> grids;

        public BagDefinitionSO Definition => GetComponent<Item>().Definition as BagDefinitionSO;

        // สวมได้ทีละใบ ใบที่สองจึงไม่บอกว่าหยิบได้
        public AimHint GetAimHint()
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            return inventory != null && inventory.WornBag == null ? AimHint.PickUp : AimHint.None;
        }

        /// <summary>ส่งตารางพร้อมของข้างในให้ผู้เล่นตอนสวม</summary>
        public List<ItemGrid> TakeGrids()
        {
            List<ItemGrid> taken = grids ?? ItemGrid.FromLayouts(Definition.Compartments);
            grids = null;
            return taken;
        }

        /// <summary>รับตารางพร้อมของข้างในคืนตอนผู้เล่นถอดวางลงฉาก</summary>
        public void PutGrids(List<ItemGrid> contents)
        {
            grids = contents;
        }
    }
}
