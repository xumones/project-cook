using UnityEngine;
using ProjectCook.Cooking;

namespace ProjectCook.NPC
{
    /// <summary>เงื่อนไขของเควสหนึ่งชนิด: ประเมินของที่ส่งยังไง และเอาออกจากผู้เล่นตอนไหน ต้องไม่เก็บ state ของตัวเอง</summary>
    public abstract class QuestObjectiveSO : ScriptableObject
    {
        public abstract DishScore Evaluate(ServingFoodContainer dish);

        /// <summary>สิ่งที่ขอแบบสั้น ใช้เขียนบนโน้ต</summary>
        public abstract string Describe();

        public abstract void Consume(ServingFoodContainer dish);
    }
}
