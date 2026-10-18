using System.Collections.Generic;
using ProjectCook.Core;

namespace ProjectCook.Cooking
{
    /// <summary>ผลการประเมินจาน: คะแนน 0–1 และเกรดที่แปลงแล้ว</summary>
    public readonly struct DishScore
    {
        public readonly float Score;
        public readonly QualityGrade Grade;

        public DishScore(float score, QualityGrade grade)
        {
            Score = score;
            Grade = grade;
        }
    }

    /// <summary>ให้คะแนนของในจานเทียบกับวัตถุดิบหลักที่ต้องการ ตอนนี้วัดจากความสุกอย่างเดียว ของเกินไม่นับ</summary>
    public static class DishEvaluator
    {
        // เกณฑ์เกรด (คะแนนขั้นต่ำ) เรียงจากสูงไปต่ำ
        private static readonly (float minScore, QualityGrade grade)[] Thresholds =
        {
            (0.95f, QualityGrade.S),
            (0.80f, QualityGrade.A),
            (0.65f, QualityGrade.B),
            (0.50f, QualityGrade.C),
            (0.30f, QualityGrade.D)
        };

        private const float BurntWhenCookedWanted = 0.25f;
        private const float CookedWhenRawWanted = 0.25f;
        private const float CookingWhenRawWanted = 0.5f;

        private static readonly List<bool> usedBuffer = new List<bool>();
        private static readonly List<float> dishScoreBuffer = new List<float>();

        /// <summary>ให้คะแนนจานที่ส่งมาพร้อมกัน: เอาจานที่ได้คะแนนสูงสุด count ใบมาเฉลี่ย จานที่ขาดนับเป็น 0 จานที่เกินไม่มีผล</summary>
        public static DishScore EvaluateBest(IReadOnlyList<IngredientRequirement> requirements, IReadOnlyList<IReadOnlyList<IngredientSnapshot>> dishes, int count)
        {
            if (count <= 0) return new DishScore(0f, QualityGrade.F);

            dishScoreBuffer.Clear();
            for (int i = 0; i < dishes.Count; i++) dishScoreBuffer.Add(Evaluate(requirements, dishes[i]).Score);
            dishScoreBuffer.Sort((a, b) => b.CompareTo(a));

            float total = 0f;
            for (int i = 0; i < count && i < dishScoreBuffer.Count; i++) total += dishScoreBuffer[i];

            float average = total / count;
            return new DishScore(average, ToGrade(average));
        }

        public static DishScore Evaluate(IReadOnlyList<IngredientRequirement> requirements, IReadOnlyList<IngredientSnapshot> ingredients)
        {
            if (requirements == null || requirements.Count == 0) return new DishScore(0f, QualityGrade.F);

            usedBuffer.Clear();
            for (int i = 0; i < ingredients.Count; i++) usedBuffer.Add(false);

            // แต่ละ requirement จับคู่กับชิ้นที่ให้คะแนนสูงสุดที่ยังไม่ถูกใช้ ไม่มีเลย = 0
            float total = 0f;
            for (int r = 0; r < requirements.Count; r++)
            {
                int best = -1;
                float bestScore = 0f;
                for (int i = 0; i < ingredients.Count; i++)
                {
                    if (usedBuffer[i] || ingredients[i].Data != requirements[r].ingredient) continue;

                    float score = ScoreDoneness(ingredients[i], requirements[r].idealState);
                    if (best < 0 || score > bestScore)
                    {
                        best = i;
                        bestScore = score;
                    }
                }

                if (best >= 0)
                {
                    usedBuffer[best] = true;
                    total += bestScore;
                }
            }

            float average = total / requirements.Count;
            return new DishScore(average, ToGrade(average));
        }

        private static float ScoreDoneness(IngredientSnapshot ingredient, CookingState ideal)
        {
            CookingState state = ingredient.State;
            if (state == ideal) return 1f;

            if (ideal == CookingState.Raw)
            {
                switch (state)
                {
                    case CookingState.Cooking: return CookingWhenRawWanted;
                    case CookingState.Cooked: return CookedWhenRawWanted;
                    default: return 0f;
                }
            }

            // ideal = Cooked (Cooking/Burnt ไม่ใช้เป็น ideal)
            switch (state)
            {
                case CookingState.Cooking: return ingredient.Progress * 0.5f;
                case CookingState.Burnt: return BurntWhenCookedWanted;
                default: return 0f;
            }
        }

        private static QualityGrade ToGrade(float score)
        {
            for (int i = 0; i < Thresholds.Length; i++)
            {
                if (score >= Thresholds[i].minScore) return Thresholds[i].grade;
            }
            return QualityGrade.F;
        }
    }
}
