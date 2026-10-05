using System;
using System.Collections.Generic;
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

        /// <summary>ชื่อวัตถุดิบตามภาษาที่เลือก คั่นด้วยจุลภาค</summary>
        public static string Describe(IReadOnlyList<IngredientRequirement> requirements)
        {
            var names = new string[requirements.Count];
            for (int i = 0; i < requirements.Count; i++)
            {
                names[i] = requirements[i].ingredient != null ? requirements[i].ingredient.GetDisplayName() : "?";
            }
            return string.Join(", ", names);
        }
    }
}
