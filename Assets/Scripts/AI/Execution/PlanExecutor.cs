using System.Collections.Generic;

namespace ProjectCook.AI
{
    public enum PlanStatus
    {
        Idle,
        Running,
        Finished,
        Failed
    }

    /// <summary>ทำตามแผนทีละขั้น หยุดชั่วคราวแล้วทำขั้นเดิมต่อได้ ขั้นไหนล้มเหลวทิ้งแผนทั้งแผน</summary>
    public class PlanExecutor
    {
        private readonly List<AIActionSO> steps = new List<AIActionSO>();
        private AgentContext context;
        private ActionRun run;
        private int index;

        public bool HasPlan => index < steps.Count;
        public bool IsPaused => HasPlan && run == null;

        public string Describe() => run != null ? run.Describe() : null;

        public void Begin(IEnumerable<AIActionSO> plan, AgentContext agentContext)
        {
            Abort();
            steps.AddRange(plan);
            context = agentContext;
            if (HasPlan) run = steps[0].Begin(context);
        }

        public PlanStatus Tick(float deltaTime)
        {
            if (run == null) return PlanStatus.Idle;

            ActionStatus status = run.Tick(deltaTime);
            if (status == ActionStatus.Running) return PlanStatus.Running;
            if (status == ActionStatus.Failed)
            {
                Abort();
                return PlanStatus.Failed;
            }

            index++;
            if (!HasPlan)
            {
                Abort();
                return PlanStatus.Finished;
            }

            run = steps[index].Begin(context);
            return PlanStatus.Running;
        }

        /// <summary>หยุดขั้นที่ทำอยู่แต่จำไว้ว่าถึงขั้นไหน Resume แล้วเริ่มขั้นนั้นใหม่</summary>
        public void Pause()
        {
            run?.Stop();
            run = null;
        }

        public void Resume()
        {
            if (IsPaused) run = steps[index].Begin(context);
        }

        public void Abort()
        {
            run?.Stop();
            run = null;
            steps.Clear();
            index = 0;
        }
    }
}
