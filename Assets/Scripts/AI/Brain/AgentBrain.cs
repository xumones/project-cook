using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>สมองกลางของตัวละคร AI: รับรู้ ทำแผนที่มีต่อ แล้วคิดใหม่เมื่อถึงเวลา</summary>
    // แต่ละชั้นเป็นของตัวเอง: ตัดสินใจ (GoalSelector) วางแผน (GoalPlanner) ลงมือ (PlanExecutor)
    // ร่างกาย (AgentVitals, AgentMover, ความจำ, เซนเซอร์) สมองนี้แค่ร้อยชั้นเข้าด้วยกัน
    // ไม่รู้จัก NPC เควส หรือบทสนทนา ส่วนนั้นสั่งผ่าน Hold, Release และ FulfillWants
    [RequireComponent(typeof(AgentMover))]
    [RequireComponent(typeof(AgentVitals))]
    public class AgentBrain : MonoBehaviour
    {
        [SerializeField] private AIProfileSO profile;

        [Tooltip("จุดประจำ ว่างแล้วกลับมาที่นี่ และถูกวางที่นี่ตอนฉากกลับมาหลังถูกซ่อน")]
        [SerializeField] private Transform homeSpot;

        [Tooltip("เมล็ดตัวสุ่ม 0 = สุ่มใหม่ทุกครั้งที่เริ่มเกม ตั้งค่าอื่นให้ทุกรอบเหมือนเดิมตอนดีบัก")]
        [SerializeField] private int seed;

        private readonly AgentMemory memory = new AgentMemory();
        private readonly PlanExecutor executor = new PlanExecutor();

        private AgentMover mover;
        private AgentVitals vitals;
        private AgentContext context;
        private ImperfectionRoller imperfection;
        private SmartObjectSensor objectSensor;
        private GoalSelector selector;
        private GoalPlanner planner;
        private WantFrustration wants;

        private AIGoalSO currentGoal;
        private string holdReason;
        private float holdUntil;
        private bool paused;
        private bool hidden;
        private bool returnHome;
        private float nextThink;
        private float lapseUntil;

        /// <summary>บอกสั้น ๆ ว่ากำลังทำอะไรเพราะอะไร สำหรับ Inspector และหน้า debug</summary>
        public string Summary { get; private set; } = string.Empty;

        public bool IsWanting => WantFrustration.IsWanting(vitals.Meters);

        private void Awake()
        {
            mover = GetComponent<AgentMover>();
            vitals = GetComponent<AgentVitals>();
            if (profile == null)
            {
                Debug.LogError($"[AgentBrain] {name} ยังไม่ได้ตั้ง Profile", this);
                enabled = false;
                return;
            }

            context = new AgentContext(this, transform, mover, vitals.Meters) { HomeSpot = homeSpot };
            imperfection = new ImperfectionRoller(profile.Imperfection, seed != 0 ? seed : Random.Range(int.MinValue, int.MaxValue));
            objectSensor = new SmartObjectSensor(transform, memory, imperfection, () => vitals.Wellbeing.Value, () => context.Target);
            selector = new GoalSelector(profile.Goals, profile.Inertia, imperfection);
            planner = new GoalPlanner(profile, context, memory);
            wants = new WantFrustration(profile.Frustration, profile.UnmetWantFrustrationPerMinute,
                profile.UnmetWantFrustrationCap, profile.FulfilledWantRelief);
        }

        private void OnEnable()
        {
            // กลับมาจากฉากถูกซ่อน (ผู้เล่นออกไปข้างนอก) รอ NavMeshAgent เปิดก่อนจึงย้ายในเฟรมถัดไป
            if (hidden) returnHome = true;
            hidden = false;
        }

        private void OnDisable()
        {
            if (context == null) return;

            DropPlan();
            holdUntil = 0f;
            paused = false;
            hidden = true;
        }

        /// <summary>หยุดทุกอย่างไว้จนกว่าจะเรียก Release เช่น ระหว่างคุยกับผู้เล่น แผนที่ทำค้างยังจำไว้</summary>
        public void Hold(string reason)
        {
            holdReason = reason;
            holdUntil = float.PositiveInfinity;
        }

        /// <summary>เลิกหยุดหลังรออีก waitSeconds วินาที (0 = ทันที) แล้วทำแผนเดิมต่อหรือคิดใหม่</summary>
        public void Release(float waitSeconds, string waitReason)
        {
            holdReason = waitReason;
            holdUntil = Time.time + waitSeconds;
        }

        /// <summary>มีคนเติมสิ่งที่อยากให้แล้ว</summary>
        public void FulfillWants() => wants.Fulfill(vitals.Meters);

        private void Update()
        {
            float now = Time.time;
            float deltaTime = Time.deltaTime;
            float wellbeing = vitals.Wellbeing.Value;

            if (returnHome)
            {
                returnHome = false;
                if (homeSpot != null) mover.Warp(homeSpot.position, homeSpot.rotation);
                memory.Clear();
            }

            wants.Advance(vitals.Meters, deltaTime / 60f);
            bool rethink = objectSensor.Update(now);

            if (now < holdUntil)
            {
                Pause();
                return;
            }
            if (paused)
            {
                paused = false;
                executor.Resume();
                rethink = true;
            }
            if (now < lapseUntil) return;

            mover.SetSpeedMultiplier(profile.SpeedMultiplier(wellbeing));
            context.UseTimeMultiplier = profile.UseTimeMultiplier(wellbeing);

            PlanStatus status = executor.Tick(deltaTime);
            if (status == PlanStatus.Failed) OnPlanFailed(now);
            if (status == PlanStatus.Finished) DropPlan();
            rethink |= status == PlanStatus.Failed || status == PlanStatus.Finished;

            if (executor.HasPlan && imperfection.RollLapse(wellbeing, deltaTime / 60f, now))
            {
                Lapse(now);
                return;
            }

            if (rethink || now >= nextThink) Think(now, wellbeing);
            UpdateSummary();
        }

        private void Think(float now, float wellbeing)
        {
            nextThink = now + profile.ThinkIntervalAt(wellbeing);

            // เปลี่ยนใจกลางแผนได้ก็ต่อเมื่อเป็นเรื่องที่สำคัญกว่า หรือของที่จะใช้ไม่ว่างแล้ว
            bool committed = executor.HasPlan && planner.TargetStillBelievedFree(now);
            foreach (GoalChoice choice in selector.Rank(vitals.Meters, currentGoal, executor.HasPlan, wellbeing))
            {
                if (!GoalSelector.MayReplace(choice.Goal, currentGoal, committed)) continue;
                if (TryStart(choice.Goal, now)) return;
            }
        }

        private bool TryStart(AIGoalSO goal, float now)
        {
            if (!planner.TryPlan(goal, now)) return false;

            executor.Begin(planner.Plan, context);
            currentGoal = goal;
            return true;
        }

        private void Pause()
        {
            if (!paused) executor.Pause();
            paused = true;
            mover.Stop();
            Summary = holdReason;
        }

        private void Lapse(float now)
        {
            string forgotten = executor.Describe();
            DropPlan();
            lapseUntil = now + profile.LapsePauseSeconds;
            Summary = $"Forgot what it was doing (was {forgotten})";
        }

        private void OnPlanFailed(float now)
        {
            if (context.Target != null) memory.RememberFailure(context.Target, now, profile.FailureMemorySeconds);
            if (profile.Frustration != null) vitals.Meters.Change(profile.Frustration, profile.FrustrationPerFailure);
            DropPlan();
        }

        private void DropPlan()
        {
            executor.Abort();
            planner.ReleaseTarget();
            currentGoal = null;
        }

        private void UpdateSummary()
        {
            string doing = executor.Describe();
            if (doing != null)
            {
                Summary = $"{doing} because {Reason(currentGoal)}";
            }
            else if (Time.time >= lapseUntil)
            {
                Summary = currentGoal != null ? $"{currentGoal.Label} (done)" : "Thinking";
            }
        }

        private string Reason(AIGoalSO goal)
        {
            if (goal == null || goal.Considerations.Count == 0 || goal.Considerations[0].meter == null) return "nothing else to do";

            MeterSO meter = goal.Considerations[0].meter;
            float value = vitals.Meters.Get(meter);
            return $"{meter.Label} {value:0} = {meter.Describe(value)}";
        }
    }
}
