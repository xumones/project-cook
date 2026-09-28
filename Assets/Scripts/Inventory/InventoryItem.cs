using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Items;

namespace ProjectCook.Inventory
{
    /// <summary>ของหนึ่งชิ้นที่อยู่ในตารางกระเป๋า เก็บเป็นข้อมูลล้วน ตำแหน่งและการหมุนตั้งได้จาก ItemGrid เท่านั้น</summary>
    public class InventoryItem
    {
        public ItemDefinitionSO Definition { get; }

        /// <summary>สถานะที่จดจากวัตถุตอนถูกเก็บ ใช้คืนให้วัตถุใหม่ตอนเอาออก</summary>
        public IReadOnlyList<ComponentState> States { get; }

        /// <summary>ช่องมุมซ้ายบนที่ของชิ้นนี้วางอยู่</summary>
        public Vector2Int Position { get; internal set; }

        public bool IsRotated { get; internal set; }

        /// <summary>จำนวนช่องที่กินจริงหลังคิดการหมุนแล้ว</summary>
        public Vector2Int Size => SizeWhen(IsRotated);

        public InventoryItem(ItemDefinitionSO definition, IReadOnlyList<ComponentState> states)
        {
            Definition = definition;
            States = states;
        }

        public Vector2Int SizeWhen(bool rotated)
        {
            Vector2Int size = Definition.GridSize;
            return rotated ? new Vector2Int(size.y, size.x) : size;
        }
    }
}
