using UnityEngine;
using ProjectCook.Items;

namespace ProjectCook.Inventory
{
    /// <summary>ตำแหน่งของไอเทมหนึ่งชิ้นในตารางที่เก็บ ตัวไอเทมคือ Instance ตำแหน่งและการหมุนตั้งได้จาก ItemGrid เท่านั้น</summary>
    public class InventoryItem
    {
        public ItemInstance Instance { get; }

        public ItemDefinitionSO Definition => Instance.Definition;

        /// <summary>ช่องมุมซ้ายบนที่ของชิ้นนี้วางอยู่</summary>
        public Vector2Int Position { get; internal set; }

        public bool IsRotated { get; internal set; }

        /// <summary>จำนวนช่องที่กินจริงหลังคิดการหมุนแล้ว</summary>
        public Vector2Int Size => SizeWhen(IsRotated);

        public InventoryItem(ItemInstance instance)
        {
            Instance = instance;
        }

        public Vector2Int SizeWhen(bool rotated)
        {
            Vector2Int size = Definition.GridSize;
            return rotated ? new Vector2Int(size.y, size.x) : size;
        }
    }
}
