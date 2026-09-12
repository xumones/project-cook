using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectCook.Cooking
{
    /// <summary>ผลรวมของการส่งมอบเทียบกับสูตร ใช้เลือกคำตอบของ NPC (ส่งได้เฉพาะตอนครบขั้นต่ำ จึงเหลือแค่พอดีกับเกิน)</summary>
    public enum RecipeOutcome
    {
        Exact,
        Extra
    }

    /// <summary>รายละเอียดว่าของในจานตรงกับสูตรแค่ไหน นับเป็นจำนวนชิ้น</summary>
    public readonly struct RecipeMatch
    {
        /// <summary>ชนิดตรงและถึงขั้นต่ำที่สูตรกำหนด</summary>
        public readonly int Matched;

        /// <summary>ชนิดตรงแต่ยังไม่ถึงขั้นต่ำ (เช่น ยังดิบทั้งที่สูตรขอผ่านความร้อนแล้ว)</summary>
        public readonly int Undercooked;

        /// <summary>สูตรต้องการแต่ในจานไม่มี</summary>
        public readonly int Missing;

        /// <summary>ในจานมีแต่สูตรไม่ต้องการ</summary>
        public readonly int Extra;

        public RecipeMatch(int matched, int undercooked, int missing, int extra)
        {
            Matched = matched;
            Undercooked = undercooked;
            Missing = missing;
            Extra = extra;
        }

        /// <summary>ส่งมอบได้เมื่อของที่สูตรต้องการมีครบและถึงขั้นต่ำทุกชิ้น ของเกินไม่ขัด</summary>
        public bool IsDeliverable => Missing == 0 && Undercooked == 0;

        public RecipeOutcome Outcome => Extra > 0 ? RecipeOutcome.Extra : RecipeOutcome.Exact;
    }

    /// <summary>สูตรอาหารหนึ่งจาน: ต้องมีวัตถุดิบอะไร และแต่ละชิ้นต้องผ่านความร้อนมาอย่างน้อยแค่ไหน</summary>
    [CreateAssetMenu(fileName = "Recipe_", menuName = "ProjectCook/Cooking/Recipe")]
    public class RecipeSO : ScriptableObject
    {
        [Serializable]
        public struct Requirement
        {
            public IngredientDataSO ingredient;

            [Tooltip("สถานะต่ำสุดที่รับได้ ตามลำดับ Raw < Cooking < Cooked < Burnt (ไหม้นับว่าผ่าน)")]
            [FormerlySerializedAs("state")]
            public CookingState minimumState;
        }

        [Tooltip("รหัสสูตร ใช้เป็นชื่อ flag ความคืบหน้า (cooked_<id>)")]
        [SerializeField] private string recipeId;

        [Tooltip("ชื่อที่แสดงใน UI/บทสนทนา")]
        [SerializeField] private string displayName;

        [Tooltip("วัตถุดิบที่ต้องมี ใส่ซ้ำได้ถ้าต้องการหลายชิ้น")]
        [SerializeField] private List<Requirement> requirements = new List<Requirement>();

        public string RecipeId => recipeId;
        public string DisplayName => displayName;
        public IReadOnlyList<Requirement> Requirements => requirements;

        // จับคู่ทีละข้อกำหนด เลือกชิ้นที่ถึงขั้นต่ำก่อน ถ้าไม่มีค่อยนับชิ้นที่ชนิดตรงแต่ยังดิบไปเป็น Undercooked
        /// <summary>เทียบของในจานกับสูตรนี้</summary>
        public RecipeMatch Evaluate(IReadOnlyList<IngredientInstance> ingredients)
        {
            bool[] used = new bool[ingredients.Count];
            int matched = 0;
            int undercooked = 0;
            int missing = 0;

            for (int r = 0; r < requirements.Count; r++)
            {
                Requirement requirement = requirements[r];
                int ready = FindUnused(ingredients, used, requirement.ingredient, requirement.minimumState);
                if (ready >= 0)
                {
                    used[ready] = true;
                    matched++;
                    continue;
                }

                int anyState = FindUnused(ingredients, used, requirement.ingredient, CookingState.Raw);
                if (anyState >= 0)
                {
                    used[anyState] = true;
                    undercooked++;
                    continue;
                }

                missing++;
            }

            int extra = 0;
            for (int i = 0; i < used.Length; i++)
            {
                if (!used[i]) extra++;
            }

            return new RecipeMatch(matched, undercooked, missing, extra);
        }

        private static int FindUnused(IReadOnlyList<IngredientInstance> ingredients, bool[] used,
                                      IngredientDataSO data, CookingState minimumState)
        {
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (used[i] || ingredients[i] == null || ingredients[i].Data != data) continue;
                if (ingredients[i].CurrentCookingState < minimumState) continue;
                return i;
            }
            return -1;
        }
    }
}
