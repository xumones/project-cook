using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Cooking;

namespace ProjectCook.Quests
{
    /// <summary>เสิร์ฟอาหารที่มีวัตถุดิบหลักตามที่ขอ ให้เกรดตามความสุกของวัตถุดิบเหล่านั้น ของอื่นในจานไม่มีผล</summary>
    [CreateAssetMenu(fileName = "Objective_Serve_", menuName = "ProjectCook/Quests/Serve Dish Objective")]
    public class ServeDishObjectiveSO : QuestObjectiveSO, IServeObjective
    {
        [Tooltip("วัตถุดิบหลักที่ต้องมีในจานหนึ่งใบ ใส่ซ้ำได้ถ้าต้องการหลายชิ้น")]
        [SerializeField] private List<IngredientRequirement> requirements = new List<IngredientRequirement>();

        [Tooltip("จำนวนจานแบบนี้ที่ต้องเสิร์ฟพร้อมกันในครั้งเดียว")]
        [Min(1)]
        [SerializeField] private int dishCount = 1;

        [Tooltip("รหัส NPC ที่ต้องเอาไปเสิร์ฟ ว่าง = ผู้ให้เควส")]
        [SerializeField] private string recipientId;

        public override int Required => dishCount;

        public string RecipientId => recipientId;

        public DishScore Evaluate(IReadOnlyList<IReadOnlyList<IngredientSnapshot>> dishes) =>
            DishEvaluator.EvaluateBest(requirements, dishes, dishCount);

        public override string Describe() => IngredientRequirement.Describe(requirements);
    }
}
