using UnityEngine;
using UnityEngine.Localization;

namespace ProjectCook.Items
{
    /// <summary>ข้อมูลประจำชนิดของไอเทม ของทุกชิ้นที่เป็นชนิดเดียวกันใช้ไฟล์นี้ร่วมกัน</summary>
    [CreateAssetMenu(fileName = "ItemDefinition_", menuName = "ProjectCook/Items/Item Definition")]
    public class ItemDefinitionSO : ScriptableObject
    {
        [Header("General Info")]
        [Tooltip("รหัสอ้างอิงไอเทม ห้ามซ้ำกัน (เช่น pork_chop, plate)")]
        [SerializeField] private string itemId;

        [Tooltip("ชื่อไอเทมที่แสดงใน UI")]
        [SerializeField] private LocalizedString displayName;

        [Tooltip("Prefab ของไอเทมชนิดนี้ ทุกระบบที่สร้างไอเทมต้องอ้างจากตรงนี้ที่เดียว")]
        [SerializeField] private GameObject prefab;

        [Header("Inventory")]
        [Tooltip("จำนวนช่องที่กินในตารางกระเป๋า (กว้าง x สูง) ตอนยังไม่หมุน")]
        [Min(1)]
        [SerializeField] private Vector2Int gridSize = Vector2Int.one;

        [Header("Shop")]
        [Tooltip("ราคาซื้อจากร้าน")]
        [Min(0)]
        [SerializeField] private int price;

        public string ItemId => itemId;
        public LocalizedString DisplayName => displayName;
        public GameObject Prefab => prefab;
        public Vector2Int GridSize => gridSize;
        public int Price => price;

        /// <summary>ชื่อตามภาษาที่เลือกอยู่ ยังไม่ได้ตั้งชื่อ = ใช้รหัสแทน</summary>
        public string GetDisplayName() =>
            displayName != null && !displayName.IsEmpty ? displayName.GetLocalizedString() : itemId;
    }
}
