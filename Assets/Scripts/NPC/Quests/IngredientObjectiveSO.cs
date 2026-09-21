using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;
using ProjectCook.Cooking;

namespace ProjectCook.NPC
{
    /// <summary>เควสที่ขอวัตถุดิบหลัก: ปลดล็อกให้ขอได้ตอนรับ และให้เกรดจานที่ส่งตามความสุกของวัตถุดิบเหล่านั้น</summary>
    [CreateAssetMenu(fileName = "Objective_", menuName = "ProjectCook/NPC/Ingredient Objective")]
    public class IngredientObjectiveSO : QuestObjectiveSO
    {
        [Tooltip("วัตถุดิบหลักที่ต้องมี ใส่ซ้ำได้ถ้าต้องการหลายชิ้น ของอื่นในจานไม่มีผล")]
        [SerializeField] private List<IngredientRequirement> requirements = new List<IngredientRequirement>();

        private static readonly List<IngredientInstance> ingredientBuffer = new List<IngredientInstance>();

        public override void OnAccepted()
        {
            GameProgress progress = GameProgress.Instance;
            for (int i = 0; i < requirements.Count; i++)
            {
                if (requirements[i].ingredient != null)
                {
                    progress.UnlockIngredient(requirements[i].ingredient.IngredientId);
                }
            }
        }

        public override DishScore Evaluate(ServingFoodContainer dish)
        {
            dish.GetIngredients(ingredientBuffer);
            return DishEvaluator.Evaluate(requirements, ingredientBuffer);
        }

        public override void Consume(ServingFoodContainer dish) => dish.ConsumeAll();
    }
}
