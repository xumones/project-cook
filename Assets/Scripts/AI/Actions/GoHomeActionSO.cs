using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>กลับไปจุดประจำแล้วหันตามทิศของจุดนั้น</summary>
    [CreateAssetMenu(fileName = "Action_", menuName = "ProjectCook/AI/Actions/Go Home")]
    public class GoHomeActionSO : AIActionSO
    {
        public override bool CanRun(AgentContext context) => context.HomeSpot != null;

        public override ActionRun Begin(AgentContext context) => new Run(context);

        private class Run : ActionRun
        {
            private readonly AgentContext context;
            private readonly bool started;

            public Run(AgentContext context)
            {
                this.context = context;
                started = context.HomeSpot != null && context.Mover.TryMoveTo(context.HomeSpot.position);
            }

            public override ActionStatus Tick(float deltaTime)
            {
                if (!started) return ActionStatus.Failed;

                switch (context.Mover.Status)
                {
                    case MoveStatus.Moving:
                        return ActionStatus.Running;
                    case MoveStatus.Arrived:
                        return TurnToward(context.Body, context.HomeSpot.rotation, deltaTime) ? ActionStatus.Succeeded : ActionStatus.Running;
                    default:
                        return ActionStatus.Failed;
                }
            }

            public override void Stop() => context.Mover.Stop();

            public override string Describe() => "กลับจุดประจำ";
        }
    }
}
