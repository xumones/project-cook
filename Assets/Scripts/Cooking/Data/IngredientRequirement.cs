using System;
using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>วัตถุดิบหลักหนึ่งชิ้นที่ต้องมีในจาน และควรอยู่ในสถานะไหน ใส่ซ้ำได้ถ้าต้องการหลายชิ้น</summary>
    [Serializable]
    public struct IngredientRequirement
    {
        public IngredientDataSO ingredient;

        [Tooltip("สถานะที่ต้องการ เช่น Cooked สำหรับเนื้อ, Raw สำหรับผักสด ห่างจากนี้คะแนนยิ่งลด")]
        public CookingState idealState;
    }
}
