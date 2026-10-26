using System.Collections.Generic;
using UnityEngine;
using ProjectCook.AI;
using ProjectCook.Core;
using ProjectCook.Dialogue;
using ProjectCook.Quests;

namespace ProjectCook.NPC
{
    /// <summary>สมองของ NPC: เลือกเป้าหมายจากคะแนน วางแผนด้วย GOAP แล้วทำตามแผน หยุดเมื่อผู้เล่นเล็งหรือคุยด้วย แล้วทำต่อหรือคิดใหม่</summary>
    // ร้านค้า เควส และการให้ของยังเป็นของความสามารถเดิม สมองดูแลแค่ร่างกายกับความตั้งใจของ NPC เอง
    // คิดใหม่ทุกรอบสั้น ๆ แต่วางแผนใหม่เฉพาะตอนเปลี่ยนเป้าหมาย แผนเดิมที่ยังดีอยู่จึงทำต่อได้
    [RequireComponent(typeof(NPCCharacter))]
    [RequireComponent(typeof(AgentMover))]
    [RequireComponent(typeof(AgentVitals))]
    public class NPCBrain : MonoBehaviour, INPCMind
    {
        // ใกล้กว่านี้ถือว่าอยู่ที่จุดแล้ว
        private const float NearDistance = 0.6f;

        // ของที่ไกลขึ้นทุก 5 เมตรดูน่าใช้ลดลงเท่าตัว
        private const float DistancePenaltyPerMeter = 0.2f;

        [SerializeField] private AIProfileSO profile;

        [Tooltip("จุดประจำ ว่างแล้วกลับมาที่นี่ และถูกวางที่นี่ตอนผู้เล่นกลับบ้าน")]
        [SerializeField] private Transform homeSpot;

        [Tooltip("เมล็ดตัวสุ่ม 0 = สุ่มใหม่ทุกครั้งที่เริ่มเกม ตั้งค่าอื่นให้ทุกรอบเหมือนเดิมตอนดีบัก")]
        [SerializeField] private int seed;

        private readonly AgentMemory memory = new AgentMemory();
        private readonly PlanExecutor executor = new PlanExecutor();
        private readonly FactTable facts = new FactTable();
        private readonly Dictionary<AIActionSO, (WorldState preconditions, WorldState effects)> actionStates = new Dictionary<AIActionSO, (WorldState, WorldState)>();
        private readonly Dictionary<AIGoalSO, WorldState> goalStates = new Dictionary<AIGoalSO, WorldState>();
        private readonly List<GoalChoice> choices = new List<GoalChoice>();
        private readonly List<AIActionSO> usable = new List<AIActionSO>();
        private readonly List<PlannerAction> plannerActions = new List<PlannerAction>();
        private readonly List<int> planIndices = new List<int>();
        private readonly List<AIActionSO> plan = new List<AIActionSO>();

        private NPCCharacter character;
        private AgentMover mover;
        private AgentVitals vitals;
        private AgentContext context;
        private ImperfectionRoller imperfection;
        private SmartObjectSensor objectSensor;
        private PlayerAttentionSensor attention;

        private AIGoalSO currentGoal;
        private bool inDialogue;
        private bool held;
        private bool hidden;
        private bool returnHome;
        private float nextThink;
        private float lapseUntil;

        // แคชไว้ตอน subscribe เพราะตอนหยุดเกม Instance อาจคืน null ก่อน OnDestroy
        private GameModeManager gameMode;

        /// <summary>บอกสั้น ๆ ว่ากำลังทำอะไรเพราะอะไร สำหรับ Inspector และหน้า debug</summary>
        public string Summary { get; private set; } = string.Empty;

        public bool IsCraving
        {
            get
            {
                MeterSet meters = vitals.Meters;
                foreach (MeterSO meter in meters.Meters)
                {
                    if (meter is WantSO want && want.IsActive(meters.Get(want))) return true;
                }
                return false;
            }
        }

        private void Awake()
        {
            character = GetComponent<NPCCharacter>();
            mover = GetComponent<AgentMover>();
            vitals = GetComponent<AgentVitals>();
            if (profile == null)
            {
                Debug.LogError($"[NPCBrain] {name} ยังไม่ได้ตั้ง Profile", this);
                enabled = false;
                return;
            }

            context = new AgentContext(this, transform, mover, vitals.Meters) { HomeSpot = homeSpot };
            imperfection = new ImperfectionRoller(profile.Imperfection, seed != 0 ? seed : Random.Range(int.MinValue, int.MaxValue));
            objectSensor = new SmartObjectSensor(transform, memory, imperfection, () => vitals.Wellbeing.Value, () => context.Target);
            attention = new PlayerAttentionSensor(character);

            foreach (AIActionSO action in profile.Actions)
            {
                if (action != null) actionStates[action] = (facts.Build(action.Preconditions), facts.Build(action.Effects));
            }
            foreach (AIGoalSO goal in profile.Goals)
            {
                if (goal != null) goalStates[goal] = facts.Build(goal.DesiredState);
            }
        }

        private void OnEnable()
        {
            MessageBus<DialogueStarted>.Subscribe(HandleDialogueStarted);
            MessageBus<QuestCompleted>.Subscribe(HandleQuestCompleted);

            // กลับมาจากฉากถูกซ่อน (ผู้เล่นออกไปข้างนอก) รอ NavMeshAgent เปิดก่อนจึงย้ายในเฟรมถัดไป
            if (hidden) returnHome = true;
            hidden = false;
        }

        private void OnDisable()
        {
            MessageBus<DialogueStarted>.Unsubscribe(HandleDialogueStarted);
            MessageBus<QuestCompleted>.Unsubscribe(HandleQuestCompleted);
            if (context == null) return;

            DropPlan();
            inDialogue = false;
            held = false;
            hidden = true;
        }

        private void Start()
        {
            gameMode = GameModeManager.Instance;
            if (gameMode != null) gameMode.ModeChanged += HandleModeChanged;
        }

        private void OnDestroy()
        {
            if (gameMode != null) gameMode.ModeChanged -= HandleModeChanged;
        }

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

            FeedUnmetWants(deltaTime / 60f);
            bool rethink = objectSensor.Update(now);
            attention.Update(now);

            if (inDialogue || attention.IsHolding(now))
            {
                Hold();
                return;
            }
            if (held)
            {
                held = false;
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

            // เลือกแค่พอใช้ได้เฉพาะตอนเลือกใหม่ ระหว่างทำแผนอยู่ไม่สุ่มเปลี่ยนใจ
            float band = executor.HasPlan ? 0f : imperfection.ScoreBand(wellbeing);
            GoalRanking.Rank(profile.Goals, vitals.Meters, currentGoal, profile.Inertia, band, imperfection, choices);
            foreach (GoalChoice choice in choices)
            {
                if (choice.Goal == currentGoal && executor.HasPlan && TargetStillBelievedFree(now)) return;
                if (TryStart(choice.Goal, now)) return;
            }
        }

        private bool TryStart(AIGoalSO goal, float now)
        {
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

            if (!GoapPlanner.TryPlan(CurrentState(), goalStates[goal], plannerActions, planIndices)
                || (target != null && !target.TryReserve(this)))
            {
                context.Target = previous;
                return false;
            }

            if (previous != null && previous != target) previous.Release(this);
            plan.Clear();
            foreach (int index in planIndices) plan.Add(usable[index]);
            executor.Begin(plan, context);
            currentGoal = goal;
            return true;
        }

        // ของที่ช่วยมาตรวัดนี้ คุ้มที่สุดเมื่อเทียบระยะ เฉพาะตัวที่เชื่อว่าว่างและไม่ได้เพิ่งพลาดมา (ยังไม่เคยเห็นถือว่าว่าง)
        private SmartObject ChooseTarget(MeterSO meter, float now)
        {
            SmartObject best = null;
            float bestScore = 0f;
            foreach (SmartObject smartObject in SmartObjectRegistry.All)
            {
                float promise = smartObject.Promise(meter);
                if (promise <= 0f || smartObject.IsReservedByOther(this) || memory.HasFailedRecently(smartObject, now)) continue;
                if (BelievesBusy(smartObject, now)) continue;

                float distance = Vector3.Distance(transform.position, smartObject.UsePosition);
                float score = promise / (1f + distance * DistancePenaltyPerMeter);
                if (score <= bestScore) continue;

                best = smartObject;
                bestScore = score;
            }
            return best;
        }

        private bool TargetStillBelievedFree(float now)
        {
            SmartObject target = context.Target;
            return target == null || (!target.IsReservedByOther(this) && !BelievesBusy(target, now));
        }

        // เห็นว่าไม่ว่างเมื่อไม่นานมานี้ เห็นนานแล้วถือว่าอาจว่างแล้ว ยอมเดินไปดูใหม่
        private bool BelievesBusy(SmartObject smartObject, float now) =>
            memory.TryRecall(smartObject, now, out bool free, out float age) && !free && age < profile.RecheckBusySeconds;

        private WorldState CurrentState()
        {
            var state = new WorldState();
            if (profile.AtHomeFact != null)
            {
                state = state.With(facts.IndexOf(profile.AtHomeFact), homeSpot != null && IsNear(homeSpot.position));
            }
            if (profile.AtTargetFact != null)
            {
                state = state.With(facts.IndexOf(profile.AtTargetFact), context.Target != null && IsNear(context.Target.UsePosition));
            }
            return state;
        }

        private bool IsNear(Vector3 point)
        {
            Vector3 offset = point - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= NearDistance * NearDistance;
        }

        private void Hold()
        {
            if (!held) executor.Pause();
            held = true;
            mover.Stop();
            Summary = inDialogue ? "คุยกับผู้เล่นอยู่" : "หยุดรอผู้เล่น";
        }

        private void Lapse(float now)
        {
            string forgotten = executor.Describe();
            DropPlan();
            lapseUntil = now + profile.LapsePauseSeconds;
            Summary = $"ลืมว่าจะทำอะไร (ตอน{forgotten})";
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
            context.Target?.Release(this);
            context.Target = null;
            currentGoal = null;
        }

        // ความอยากที่ยังไม่มีใครเติมให้ค่อย ๆ ทำให้หงุดหงิด แต่ไม่เกินเพดาน และไม่มีผลกับผู้เล่น
        private void FeedUnmetWants(float minutes)
        {
            FeelingSO frustration = profile.Frustration;
            if (frustration == null || !IsCraving) return;

            float room = profile.UnmetWantFrustrationCap - vitals.Meters.Get(frustration);
            if (room > 0f) vitals.Meters.Change(frustration, Mathf.Min(room, profile.UnmetWantFrustrationPerMinute * minutes));
        }

        private void UpdateSummary()
        {
            string doing = executor.Describe();
            if (doing != null)
            {
                Summary = $"{doing} เพราะ{Reason(currentGoal)}";
            }
            else if (Time.time >= lapseUntil)
            {
                Summary = currentGoal != null ? $"{currentGoal.Label} (เสร็จแล้ว)" : "กำลังคิด";
            }
        }

        private string Reason(AIGoalSO goal)
        {
            if (goal == null || goal.Considerations.Count == 0 || goal.Considerations[0].meter == null) return "ไม่มีอะไรต้องทำ";

            MeterSO meter = goal.Considerations[0].meter;
            float value = vitals.Meters.Get(meter);
            return $"{meter.Label} {value:0} = {meter.Describe(value)}";
        }

        private void HandleDialogueStarted(DialogueStarted message)
        {
            if (message.SpeakerId == character.SpeakerId) inDialogue = true;
        }

        // ร้านค้าและกล่องให้ของเปิดต่อหลังบทสนทนาปิด จึงรอจนกลับไปเดินสำรวจจริงก่อน
        private void HandleModeChanged(GameMode mode)
        {
            if (mode == GameMode.Exploring) inDialogue = false;
        }

        private void HandleQuestCompleted(QuestCompleted message)
        {
            if (message.GiverId != character.Profile.NpcId) return;

            MeterSet meters = vitals.Meters;
            foreach (MeterSO meter in meters.Meters)
            {
                if (meter is WantSO) meters.Set(meter, 0f);
            }
            if (profile.Frustration != null) meters.Change(profile.Frustration, -profile.FulfilledWantRelief);
        }
    }
}
