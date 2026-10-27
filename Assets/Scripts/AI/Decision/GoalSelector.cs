using System.Collections.Generic;

namespace ProjectCook.AI
{
    /// <summary>ชั้นตัดสินใจ: เรียงเป้าหมายที่ควรลองวางแผนก่อน และบอกว่าเป้าหมายไหนแทรกแผนที่ทำอยู่ได้</summary>
    // เลือกแค่พอใช้ได้ (สุ่มในช่วงคะแนนใกล้กัน) เฉพาะตอนเลือกใหม่ ระหว่างทำแผนอยู่ไม่สุ่มเปลี่ยนใจ
    public class GoalSelector
    {
        private readonly IReadOnlyList<AIGoalSO> goals;
        private readonly float inertia;
        private readonly ImperfectionRoller imperfection;
        private readonly List<GoalChoice> choices = new List<GoalChoice>();

        public GoalSelector(IReadOnlyList<AIGoalSO> goals, float inertia, ImperfectionRoller imperfection)
        {
            this.goals = goals;
            this.inertia = inertia;
            this.imperfection = imperfection;
        }

        public IReadOnlyList<GoalChoice> Rank(MeterSet meters, AIGoalSO current, bool hasPlan, float wellbeing)
        {
            float band = hasPlan ? 0f : imperfection.ScoreBand(wellbeing);
            GoalRanking.Rank(goals, meters, current, inertia, band, imperfection, choices);
            return choices;
        }

        /// <summary>เริ่มแล้วทำให้จบ (กินไม่เลิกกลางจานเพราะเริ่มอิ่ม) ระหว่างทำแผนที่ยังดีอยู่ แทรกได้เฉพาะเป้าหมายชั้นสูงกว่า</summary>
        public static bool MayReplace(AIGoalSO candidate, AIGoalSO current, bool committed) =>
            !committed || current == null || candidate.Rank > current.Rank;
    }
}
