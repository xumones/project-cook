using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>ชั้นวางแผน: เปลี่ยนเป้าหมายเป็นแผน เลือกของที่จะใช้ วางแผนด้วย GOAP แล้วจองของไว้</summary>
    // เลือกของจากสิ่งที่ "เชื่อ" ไม่ใช่ความจริง ของที่เชื่อว่าไม่ว่างหรือเพิ่งใช้พลาดจะถูกข้าม
    public class GoalPlanner
    {
        // ใกล้กว่านี้ถือว่าอยู่ที่จุดแล้ว
        private const float NearDistance = 0.6f;

        // ของที่ไกลขึ้นทุก 5 เมตรดูน่าใช้ลดลงเท่าตัว
        private const float DistancePenaltyPerMeter = 0.2f;

        private readonly AIProfileSO profile;
        private readonly AgentContext context;
        private readonly AgentMemory memory;
        private readonly FactTable facts = new FactTable();
        private readonly Dictionary<AIActionSO, (WorldState preconditions, WorldState effects)> actionStates = new Dictionary<AIActionSO, (WorldState, WorldState)>();
        private readonly Dictionary<AIGoalSO, WorldState> goalStates = new Dictionary<AIGoalSO, WorldState>();
        private readonly List<AIActionSO> usable = new List<AIActionSO>();
        private readonly List<PlannerAction> plannerActions = new List<PlannerAction>();
        private readonly List<int> planIndices = new List<int>();
        private readonly List<AIActionSO> plan = new List<AIActionSO>();

        public GoalPlanner(AIProfileSO profile, AgentContext context, AgentMemory memory)
        {
            this.profile = profile;
            this.context = context;
            this.memory = memory;

            foreach (AIActionSO action in profile.Actions)
            {
                if (action != null) actionStates[action] = (facts.Build(action.Preconditions), facts.Build(action.Effects));
            }
            foreach (AIGoalSO goal in profile.Goals)
            {
                if (goal != null) goalStates[goal] = facts.Build(goal.DesiredState);
            }
        }

        /// <summary>แผนที่วางสำเร็จครั้งล่าสุด</summary>
        public IReadOnlyList<AIActionSO> Plan => plan;

        /// <summary>สำเร็จ = ได้แผนใหม่และจองของแล้ว (context.Target เป็นของชิ้นใหม่) ไม่สำเร็จ = ทุกอย่างคงเดิม</summary>
        public bool TryPlan(AIGoalSO goal, float now)
        {
            if (!goalStates.TryGetValue(goal, out WorldState desired)) return false;

            SmartObject previous = context.Target;
            SmartObject target = null;
            if (goal.TargetMeter != null)
            {
                target = ChooseTarget(goal.TargetMeter, now);
                if (target == null) return false;
            }

            context.Target = target;
            usable.Clear();
            plannerActions.Clear();
            foreach (KeyValuePair<AIActionSO, (WorldState preconditions, WorldState effects)> entry in actionStates)
            {
                if (!entry.Key.CanRun(context)) continue;

                usable.Add(entry.Key);
                plannerActions.Add(new PlannerAction(entry.Value.preconditions, entry.Value.effects, entry.Key.BaseCost));
            }

            if (!GoapPlanner.TryPlan(CurrentState(), desired, plannerActions, planIndices)
                || (target != null && !target.TryReserve(context.Owner)))
            {
                context.Target = previous;
                return false;
            }

            if (previous != null && previous != target) previous.Release(context.Owner);
            plan.Clear();
            foreach (int index in planIndices) plan.Add(usable[index]);
            return true;
        }

        /// <summary>ทิ้งของที่จองไว้ ใช้ตอนเลิกแผน</summary>
        public void ReleaseTarget()
        {
            context.Target?.Release(context.Owner);
            context.Target = null;
        }

        /// <summary>ของที่กำลังจะไปใช้ยังเชื่อว่าว่างอยู่ (ไม่มีของ = ไม่มีอะไรขวาง)</summary>
        public bool TargetStillBelievedFree(float now)
        {
            SmartObject target = context.Target;
            return target == null || (!target.IsReservedByOther(context.Owner) && !BelievesBusy(target, now));
        }

        // ของที่ช่วยมาตรวัดนี้ คุ้มที่สุดเมื่อเทียบระยะ เฉพาะตัวที่เชื่อว่าว่างและไม่ได้เพิ่งพลาดมา (ยังไม่เคยเห็นถือว่าว่าง)
        private SmartObject ChooseTarget(MeterSO meter, float now)
        {
            SmartObject best = null;
            float bestScore = 0f;
            foreach (SmartObject smartObject in SmartObjectRegistry.All)
            {
                float promise = smartObject.Promise(meter);
                if (promise <= 0f || smartObject.IsReservedByOther(context.Owner) || memory.HasFailedRecently(smartObject, now)) continue;
                if (BelievesBusy(smartObject, now)) continue;

                float distance = Vector3.Distance(context.Body.position, smartObject.UsePosition);
                float score = promise / (1f + distance * DistancePenaltyPerMeter);
                if (score <= bestScore) continue;

                best = smartObject;
                bestScore = score;
            }
            return best;
        }

        // เห็นว่าไม่ว่างเมื่อไม่นานมานี้ เห็นนานแล้วถือว่าอาจว่างแล้ว ยอมเดินไปดูใหม่
        private bool BelievesBusy(SmartObject smartObject, float now) =>
            memory.TryRecall(smartObject, now, out bool free, out float age) && !free && age < profile.RecheckBusySeconds;

        private WorldState CurrentState()
        {
            var state = new WorldState();
            if (profile.AtHomeFact != null)
            {
                state = state.With(facts.IndexOf(profile.AtHomeFact), context.HomeSpot != null && IsNear(context.HomeSpot.position));
            }
            if (profile.AtTargetFact != null)
            {
                state = state.With(facts.IndexOf(profile.AtTargetFact), context.Target != null && IsNear(context.Target.UsePosition));
            }
            return state;
        }

        private bool IsNear(Vector3 point)
        {
            Vector3 offset = point - context.Body.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= NearDistance * NearDistance;
        }
    }
}
