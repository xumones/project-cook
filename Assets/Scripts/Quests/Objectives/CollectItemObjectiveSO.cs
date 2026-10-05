using UnityEngine;
using ProjectCook.Items;

namespace ProjectCook.Quests
{
    /// <summary>หยิบไอเทมชนิดนี้ให้ครบจำนวน นับตอนหยิบ ไม่ใช่จำนวนที่มีอยู่ในกระเป๋า</summary>
    [CreateAssetMenu(fileName = "Objective_Collect_", menuName = "ProjectCook/Quests/Collect Item Objective")]
    public class CollectItemObjectiveSO : QuestObjectiveSO
    {
        [SerializeField] private ItemDefinitionSO item;

        [Min(1)]
        [SerializeField] private int count = 1;

        [Tooltip("นับเฉพาะของที่หาได้ข้างนอกบ้าน ของที่ซื้อหรือพาออกไปเองไม่นับ")]
        [SerializeField] private bool foundOutsideOnly;

        public override int Required => count;

        public override int OnItemCollected(ItemInstance collected)
        {
            if (collected.Definition != item) return 0;
            return !foundOutsideOnly || collected.HasFlag(ItemFlags.FoundOutside) ? 1 : 0;
        }

        public override string Describe() => item != null ? item.GetDisplayName() : "?";
    }
}
