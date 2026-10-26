using System.Collections.Generic;

namespace ProjectCook.AI
{
    public readonly struct GoalChoice
    {
        public readonly AIGoalSO Goal;
        public readonly float Score;

        public GoalChoice(AIGoalSO goal, float score)
        {
            Goal = goal;
            Score = score;
        }
    }

    /// <summary>เรียงเป้าหมายตามที่ควรลองวางแผนก่อน: ชั้นสูงก่อน แล้วคะแนนสูงก่อน (แบบ Dual Utility)</summary>
    // เป้าหมายที่ทำอยู่ได้แต้มต่อ จะได้ไม่สลับไปมาตอนคะแนนสูสี
    // ความไม่สมบูรณ์ "เลือกแค่พอใช้ได้": ตัวที่คะแนนห่างอันดับหนึ่งไม่เกิน band ในชั้นเดียวกันถูกสุ่มขึ้นมาเป็นอันดับหนึ่งได้
    public static class GoalRanking
    {
        public static void Rank(IReadOnlyList<AIGoalSO> goals, MeterSet meters, AIGoalSO current, float inertia,
            float band, ImperfectionRoller roller, List<GoalChoice> result)
        {
            result.Clear();
            foreach (AIGoalSO goal in goals)
            {
                if (goal == null) continue;

                float score = goal.Score(meters) + (goal == current ? inertia : 0f);
                if (score > 0f) result.Add(new GoalChoice(goal, score));
            }

            // List.Sort ไม่คงลำดับเดิมตอนเท่ากัน จึงเรียงแบบแทรกซึ่งคงลำดับใน profile ไว้
            for (int i = 1; i < result.Count; i++)
            {
                GoalChoice item = result[i];
                int j = i - 1;
                while (j >= 0 && Comes(item, result[j]))
                {
                    result[j + 1] = result[j];
                    j--;
                }
                result[j + 1] = item;
            }

            if (result.Count < 2 || band <= 0f) return;

            int close = 1;
            while (close < result.Count && result[close].Goal.Rank == result[0].Goal.Rank && result[0].Score - result[close].Score <= band)
            {
                close++;
            }
            if (close < 2) return;

            int picked = roller.Pick(close);
            GoalChoice chosen = result[picked];
            result.RemoveAt(picked);
            result.Insert(0, chosen);
        }

        private static bool Comes(GoalChoice a, GoalChoice b) =>
            a.Goal.Rank > b.Goal.Rank || (a.Goal.Rank == b.Goal.Rank && a.Score > b.Score);
    }
}
