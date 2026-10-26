using System.Collections.Generic;
using ProjectCook.AI;

namespace ProjectCook.Tests
{
    /// <summary>การกระทำปลอมสำหรับเทสต์: คืนสถานะตามลำดับที่ตั้งไว้ทีละเฟรม และนับว่าถูกเริ่ม/หยุดกี่ครั้ง</summary>
    public class FakeActionSO : AIActionSO
    {
        public readonly Queue<ActionStatus> Results = new Queue<ActionStatus>();
        public int Begins;
        public int Stops;

        public override bool CanRun(AgentContext context) => true;

        public override ActionRun Begin(AgentContext context)
        {
            Begins++;
            return new Run(this);
        }

        private class Run : ActionRun
        {
            private readonly FakeActionSO owner;

            public Run(FakeActionSO owner)
            {
                this.owner = owner;
            }

            public override ActionStatus Tick(float deltaTime) =>
                owner.Results.Count > 0 ? owner.Results.Dequeue() : ActionStatus.Running;

            public override void Stop() => owner.Stops++;

            public override string Describe() => owner.name;
        }
    }
}
