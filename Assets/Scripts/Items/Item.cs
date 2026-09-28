using UnityEngine;

namespace ProjectCook.Items
{
    /// <summary>ไอเทมชิ้นนี้อยู่ที่ไหนในตอนนี้</summary>
    public enum ItemLocation
    {
        World,
        Hand
    }

    [DisallowMultipleComponent]
    public class Item : MonoBehaviour
    {
        [Tooltip("ข้อมูลประจำชนิดของไอเทมนี้")]
        [SerializeField] private ItemDefinitionSO definition;

        public ItemDefinitionSO Definition => definition;

        /// <summary>ไอเทมชิ้นนี้อยู่ที่ไหนในตอนนี้</summary>
        public ItemLocation Location { get; private set; } = ItemLocation.World;

        /// <summary>เรียกจาก EquippableItem เท่านั้น เมื่อไอเทมย้ายที่อยู่</summary>
        public void SetLocation(ItemLocation next)
        {
            Location = next;
        }
    }
}
