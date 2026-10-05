using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Core;
using ProjectCook.Cooking;

namespace ProjectCook.NPC
{
    /// <summary>เควสสุ่มหนึ่งรายการ: วัตถุดิบไม่ซ้ำกันที่ราคารวมน้อยกว่างบของผู้เล่น ทำซ้ำได้ ประโยคมาจาก NPC ที่ให้</summary>
    public class IngredientOrder : IQuestRequest
    {
        private const int MinIngredients = 2;
        private const int MaxIngredients = 4;
        private const float RewardMultiplier = 1.5f;

        private static readonly List<IngredientDataSO> shuffled = new List<IngredientDataSO>();
        private static readonly List<IngredientInstance> ingredientBuffer = new List<IngredientInstance>();

        private readonly List<IngredientRequirement> requirements;
        private readonly NPCProfileSO profile;

        public int Reward { get; }

        private IngredientOrder(List<IngredientRequirement> requirements, int reward, NPCProfileSO profile)
        {
            this.requirements = requirements;
            this.profile = profile;
            Reward = reward;
        }

        /// <summary>สุ่มจาก pool ให้ราคารวมน้อยกว่า budget คืน null ถ้าจัดได้ไม่ถึงจำนวนต่ำสุด</summary>
        public static IngredientOrder TryCreate(IReadOnlyList<IngredientDataSO> pool, int budget, NPCProfileSO profile)
        {
            shuffled.Clear();
            foreach (IngredientDataSO ingredient in pool)
            {
                if (ingredient != null && !shuffled.Contains(ingredient)) shuffled.Add(ingredient);
            }
            Shuffle(shuffled);

            int target = Random.Range(MinIngredients, MaxIngredients + 1);
            var picked = new List<IngredientRequirement>(target);
            int total = 0;
            foreach (IngredientDataSO ingredient in shuffled)
            {
                if (picked.Count == target) break;

                // ชิ้นแรกต้องเหลืองบให้ชิ้นที่ถูกสุดที่เหลือ ไม่งั้นอาจตันอยู่ชิ้นเดียวทั้งที่มีคู่ที่ซื้อไหว
                int limit = picked.Count == 0 ? budget - CheapestOther(ingredient) : budget;
                if (total + ingredient.Price >= limit) continue;

                picked.Add(new IngredientRequirement { ingredient = ingredient, idealState = ingredient.ServedState });
                total += ingredient.Price;
            }

            return picked.Count >= MinIngredients
                ? new IngredientOrder(picked, Mathf.CeilToInt(total * RewardMultiplier), profile)
                : null;
        }

        public LocalizedString OfferText => Line(profile.OrderOffer);

        public LocalizedString ReminderText => Line(profile.OrderReminder);

        public DishScore Evaluate(ServingFoodContainer dish)
        {
            dish.GetIngredients(ingredientBuffer);
            return DishEvaluator.Evaluate(requirements, ingredientBuffer);
        }

        public void Consume(ServingFoodContainer dish) => dish.ConsumeAll();

        public string Describe() => IngredientRequirement.Describe(requirements);

        public LocalizedString ResultText(QualityGrade grade) => Line(grade switch
        {
            QualityGrade.F => profile.OrderFail,
            QualityGrade.S => profile.OrderExcellent,
            _ => profile.OrderPass
        });

        // ทำซ้ำได้ จึงไม่มีอะไรต้องจด
        public void OnCompleted() { }

        // สร้างสำเนาใหม่ทุกครั้ง ไม่แก้ LocalizedString ที่อยู่ใน profile ซึ่งเป็นข้อมูลของ asset
        private LocalizedString Line(NPCProfileSO.LineSet lines)
        {
            if (!lines.TryPick(out LocalizedString line)) return null;

            return new LocalizedString(line.TableReference, line.TableEntryReference)
            {
                Arguments = new object[] { IngredientNames() }
            };
        }

        private List<string> IngredientNames()
        {
            var names = new List<string>(requirements.Count);
            foreach (IngredientRequirement requirement in requirements)
            {
                names.Add(requirement.ingredient.GetDisplayName());
            }
            return names;
        }

        private static int CheapestOther(IngredientDataSO except)
        {
            int cheapest = int.MaxValue;
            foreach (IngredientDataSO other in shuffled)
            {
                if (other != except && other.Price < cheapest) cheapest = other.Price;
            }
            return cheapest;
        }

        private static void Shuffle(List<IngredientDataSO> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
