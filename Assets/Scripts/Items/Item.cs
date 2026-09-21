using UnityEngine;

namespace ProjectCook.Items
{
    /// <summary>ประเภทของไอเทม ใช้เป็นตัวตัดสินว่าไอเทมควรไปอยู่มือบทบาทไหนเป็นค่าเริ่มต้น</summary>
    public enum ItemCategory
    {
        Tool,
        Container,
        Ingredient
    }

    /// <summary>ไอเทมชิ้นนี้อยู่ที่ไหนในตอนนี้</summary>
    public enum ItemLocation
    {
        World,
        Hand
    }

    [DisallowMultipleComponent]
    public class Item : MonoBehaviour
    {
        [Tooltip("ประเภทของไอเทม ใช้ตัดสินมือที่จะไปอยู่เป็นค่าเริ่มต้น")]
        [SerializeField] private ItemCategory category = ItemCategory.Tool;

        /// <summary>ไอเทมชิ้นนี้อยู่ที่ไหนในตอนนี้</summary>
        public ItemLocation Location { get; private set; } = ItemLocation.World;

        /// <summary>มือที่ไอเทมประเภทนี้ควรไปอยู่ อุปกรณ์ไปมือถนัด ที่เหลือไปมือรอง</summary>
        public HandRole PreferredHandRole =>
            category == ItemCategory.Tool ? HandRole.Primary : HandRole.Secondary;

        /// <summary>เรียกจาก EquippableItem เท่านั้น เมื่อไอเทมย้ายที่อยู่</summary>
        public void SetLocation(ItemLocation next)
        {
            Location = next;
        }
    }
}
