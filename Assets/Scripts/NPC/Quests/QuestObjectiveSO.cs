using UnityEngine;
using ProjectCook.Cooking;

namespace ProjectCook.NPC
{
    /// <summary>เงื่อนไขของเควสหนึ่งชนิด: เตรียมอะไรตอนรับ ประเมินของที่ส่งยังไง และเอาออกจากผู้เล่นตอนไหน ต้องไม่เก็บ state ของตัวเอง</summary>
    public abstract class QuestObjectiveSO : ScriptableObject
    {
        public abstract void OnAccepted();

        public abstract DishScore Evaluate(ServingFoodContainer dish);

        public abstract void Consume(ServingFoodContainer dish);
    }
}
