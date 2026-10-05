using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Core;
using ProjectCook.Cooking;
using ProjectCook.Items;
using ProjectCook.Quests;

namespace ProjectCook.NPC
{
    /// <summary>เควสสุ่มหนึ่งรายการ: ส่งจานที่มีวัตถุดิบไม่ซ้ำกันซึ่งราคารวมน้อยกว่างบของผู้เล่น ทำซ้ำได้ ประโยคมาจาก NPC ที่ให้</summary>
    // เป็นทั้งเนื้อหาเควสและเป้าหมายข้อเดียวของมันเอง
    public class IngredientOrder : IQuestDefinition, IDeliveryObjective
    {
        private const int MinIngredients = 2;
        private const int MaxIngredients = 4;
        private const float RewardMultiplier = 1.5f;

        private static readonly List<IngredientDataSO> shuffled = new List<IngredientDataSO>();

        private readonly List<IngredientRequirement> requirements;
        private readonly NPCProfileSO profile;
        private readonly LinePicker lines;
        private readonly IQuestObjective[] objectives;

        public int Reward { get; }

        private IngredientOrder(List<IngredientRequirement> requirements, int reward, NPCProfileSO profile, LinePicker lines)
        {
            this.requirements = requirements;
            this.profile = profile;
            this.lines = lines;
            objectives = new IQuestObjective[] { this };
            Reward = reward;
        }

        /// <summary>สุ่มจาก pool ให้ราคารวมน้อยกว่า budget คืน null ถ้าจัดได้ไม่ถึงจำนวนต่ำสุด</summary>
        public static IngredientOrder TryCreate(IReadOnlyList<IngredientDataSO> pool, int budget, NPCProfileSO profile, LinePicker lines)
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
                ? new IngredientOrder(picked, Mathf.CeilToInt(total * RewardMultiplier), profile, lines)
                : null;
        }

        // ทำซ้ำได้ จึงไม่มีรหัสให้จดว่าจบ
        public string QuestId => string.Empty;

        public IReadOnlyList<IQuestObjective> Objectives => objectives;

        public LocalizedString OfferText => Line(profile.OrderOffer);

        public LocalizedString ReminderText => Line(profile.OrderReminder);

        public LocalizedString ResultText(QualityGrade grade) => Line(grade switch
        {
            QualityGrade.F => profile.OrderFail,
            QualityGrade.S => profile.OrderExcellent,
            _ => profile.OrderPass
        });

        public int Required => 1;

        public string RecipientId => string.Empty;

        public DishScore Evaluate(IReadOnlyList<IngredientSnapshot> dish) => DishEvaluator.Evaluate(requirements, dish);

        public string Describe() => IngredientRequirement.Describe(requirements);

        public int OnItemCollected(ItemInstance item) => 0;

        public int OnWorldEvent(string eventId) => 0;

        public int OnTalkedTo(string speakerId) => 0;

        // สร้างสำเนาใหม่ทุกครั้ง ไม่แก้ LocalizedString ที่อยู่ใน profile ซึ่งเป็นข้อมูลของ asset
        private LocalizedString Line(NPCProfileSO.LineSet set)
        {
            if (!lines.TryPick(set, out LocalizedString line)) return null;

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
