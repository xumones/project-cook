using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>ใช้ของเป้าหมายจนครบเวลา ได้ผลทีละนิดตามที่ของประกาศ มีคนอื่นใช้อยู่ตอนถึงหรือระหว่างใช้ถือว่าล้มเหลว</summary>
    // ตรงนี้คือจุดที่ตัวละครเห็นความจริง ถ้าเชื่อผิดมาตลอดทาง จะรู้ตัวตอนนี้
    [CreateAssetMenu(fileName = "Action_", menuName = "ProjectCook/AI/Actions/Use Target")]
    public class UseTargetActionSO : AIActionSO
    {
        public override bool CanRun(AgentContext context) => context.Target != null;

        public override ActionRun Begin(AgentContext context) => new Run(context);

        private class Run : ActionRun
        {
            private readonly AgentContext context;
            private readonly SmartObject target;
            private readonly float duration;
            private float elapsed;

            public Run(AgentContext context)
            {
                this.context = context;
                target = context.Target;
                if (target != null) duration = target.UseSeconds * context.UseTimeMultiplier;
            }

            public override ActionStatus Tick(float deltaTime)
            {
                if (target == null || !target.isActiveAndEnabled || !target.IsAvailableTo(context.Owner)) return ActionStatus.Failed;

                TurnToward(context.Body, target.UseRotation, deltaTime);

                // ใช้นานหรือสั้นกว่าปกติก็ได้ผลรวมเท่าที่ของประกาศไว้
                float step = Mathf.Min(deltaTime, duration - elapsed);
                target.ApplyUse(context.Meters, step / duration * target.UseSeconds);
                elapsed += step;
                return elapsed >= duration ? ActionStatus.Succeeded : ActionStatus.Running;
            }

            public override string Describe() => "Using " + (target != null ? target.Label : "?");
        }
    }
}
