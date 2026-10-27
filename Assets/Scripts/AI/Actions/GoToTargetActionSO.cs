using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>เดินไปจุดใช้ของเป้าหมาย ไปไม่ถึง (ทางตัน หรือของหายไป) ถือว่าล้มเหลว</summary>
    [CreateAssetMenu(fileName = "Action_", menuName = "ProjectCook/AI/Actions/Go To Target")]
    public class GoToTargetActionSO : AIActionSO
    {
        public override bool CanRun(AgentContext context) => context.Target != null;

        public override ActionRun Begin(AgentContext context) => new Run(context);

        private class Run : ActionRun
        {
            private readonly AgentContext context;
            private readonly SmartObject target;
            private readonly bool started;

            public Run(AgentContext context)
            {
                this.context = context;
                target = context.Target;
                started = target != null && context.Mover.TryMoveTo(target.UsePosition);
            }

            public override ActionStatus Tick(float deltaTime)
            {
                if (!started || target == null || !target.isActiveAndEnabled) return ActionStatus.Failed;

                return context.Mover.Status switch
                {
                    MoveStatus.Arrived => ActionStatus.Succeeded,
                    MoveStatus.Moving => ActionStatus.Running,
                    _ => ActionStatus.Failed
                };
            }

            public override void Stop() => context.Mover.Stop();

            public override string Describe() => "Walking to " + (target != null ? target.Label : "?");
        }
    }
}
