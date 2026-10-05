using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Cooking;

namespace ProjectCook.Quests
{
    /// <summary>ส่งจานที่มีวัตถุดิบหลักตามที่ขอ ให้เกรดตามความสุกของวัตถุดิบเหล่านั้น ของอื่นในจานไม่มีผล</summary>
    [CreateAssetMenu(fileName = "Objective_Deliver_", menuName = "ProjectCook/Quests/Deliver Dish Objective")]
    public class DeliverDishObjectiveSO : QuestObjectiveSO, IDeliveryObjective
    {
        [Tooltip("วัตถุดิบหลักที่ต้องมี ใส่ซ้ำได้ถ้าต้องการหลายชิ้น")]
        [SerializeField] private List<IngredientRequirement> requirements = new List<IngredientRequirement>();

        [Tooltip("รหัส NPC ที่ต้องเอาไปส่ง ว่าง = ผู้ให้เควส")]
        [SerializeField] private string recipientId;

        public string RecipientId => recipientId;

        public DishScore Evaluate(IReadOnlyList<IngredientSnapshot> dish) => DishEvaluator.Evaluate(requirements, dish);

        public override string Describe() => IngredientRequirement.Describe(requirements);
    }
}
