using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Cooking;

namespace ProjectCook.NPC
{
    /// <summary>เควสที่ขอวัตถุดิบหลัก ให้เกรดจานที่ส่งตามความสุกของวัตถุดิบเหล่านั้น</summary>
    [CreateAssetMenu(fileName = "Objective_", menuName = "ProjectCook/NPC/Ingredient Objective")]
    public class IngredientObjectiveSO : QuestObjectiveSO
    {
        [Tooltip("วัตถุดิบหลักที่ต้องมี ใส่ซ้ำได้ถ้าต้องการหลายชิ้น ของอื่นในจานไม่มีผล")]
        [SerializeField] private List<IngredientRequirement> requirements = new List<IngredientRequirement>();

        private static readonly List<IngredientInstance> ingredientBuffer = new List<IngredientInstance>();

        public override DishScore Evaluate(ServingFoodContainer dish)
        {
            dish.GetIngredients(ingredientBuffer);
            return DishEvaluator.Evaluate(requirements, ingredientBuffer);
        }

        public override void Consume(ServingFoodContainer dish) => dish.ConsumeAll();

        public override string Describe() => IngredientRequirement.Describe(requirements);
    }
}
