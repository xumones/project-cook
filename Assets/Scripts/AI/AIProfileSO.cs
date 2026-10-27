using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>นิสัยของตัวละคร: อยากทำอะไรได้บ้าง (เป้าหมาย) ทำด้วยวิธีไหน (การกระทำ) และพลาดง่ายแค่ไหน</summary>
    // เป้าหมายชุดเดียวกันกับการกระทำคนละชุด = ตัวละครคนละแบบ (แบบ F.E.A.R.)
    [CreateAssetMenu(fileName = "AIProfile_", menuName = "ProjectCook/AI/Profile")]
    public class AIProfileSO : ScriptableObject
    {
        [Header("Behaviour")]
        [Tooltip("เรียงตามความชอบ คะแนนเท่ากันตัวบนได้ก่อน")]
        [SerializeField] private List<AIGoalSO> goals = new List<AIGoalSO>();

        [SerializeField] private List<AIActionSO> actions = new List<AIActionSO>();

        [Tooltip("ข้อเท็จจริง \"อยู่ที่ของเป้าหมายแล้ว\"")]
        [SerializeField] private FactSO atTargetFact;

        [Tooltip("ข้อเท็จจริง \"อยู่ที่จุดประจำแล้ว\"")]
        [SerializeField] private FactSO atHomeFact;

        [Tooltip("คะแนนที่เพิ่มให้เป้าหมายที่ทำอยู่ กันการสลับไปมา")]
        [Min(0f)]
        [SerializeField] private float inertia = 0.1f;

        [Tooltip("คิดใหม่ทุกกี่วินาทีก่อนคูณตามความมั่นคง (ดู Pace)")]
        [Min(0.1f)]
        [SerializeField] private float thinkInterval = 0.5f;

        [Header("Pace")]
        [Tooltip("ตัวคูณความเร็วเดินตอนความมั่นคง 0 (รีบ)")]
        [SerializeField] private float hurriedSpeed = 1.2f;

        [Tooltip("ตัวคูณความเร็วเดินตอนความมั่นคง 100 (สบาย ๆ)")]
        [SerializeField] private float relaxedSpeed = 0.9f;

        [Tooltip("ตัวคูณเวลาใช้ของตอนความมั่นคง 0")]
        [SerializeField] private float hurriedUseTime = 0.8f;

        [Tooltip("ตัวคูณเวลาใช้ของตอนความมั่นคง 100")]
        [SerializeField] private float relaxedUseTime = 1.3f;

        [Tooltip("ตัวคูณรอบคิดตอนความมั่นคง 0 (น้อย = คิดถี่)")]
        [Min(0.1f)]
        [SerializeField] private float hurriedThink = 0.6f;

        [Tooltip("ตัวคูณรอบคิดตอนความมั่นคง 100 (มาก = คิดห่าง ประหยัด CPU)")]
        [Min(0.1f)]
        [SerializeField] private float relaxedThink = 1.4f;

        [Header("Frustration")]
        [SerializeField] private FeelingSO frustration;

        [Min(0f)]
        [SerializeField] private float frustrationPerFailure = 15f;

        [Tooltip("ความอยากที่ยังไม่ได้รับทำให้หงุดหงิดขึ้นกี่หน่วยต่อนาที")]
        [Min(0f)]
        [SerializeField] private float unmetWantFrustrationPerMinute = 2f;

        [Tooltip("ความอยากดันความหงุดหงิดได้สูงสุดแค่นี้")]
        [Range(0f, MeterSO.Max)]
        [SerializeField] private float unmetWantFrustrationCap = 40f;

        [Tooltip("ได้สิ่งที่อยากแล้วหงุดหงิดลดเท่านี้")]
        [Min(0f)]
        [SerializeField] private float fulfilledWantRelief = 20f;

        [Tooltip("หลีกเลี่ยงของที่เพิ่งใช้ไม่สำเร็จกี่วินาที")]
        [Min(0f)]
        [SerializeField] private float failureMemorySeconds = 30f;

        [Tooltip("เห็นว่าของไม่ว่างนานเกินกี่วินาทีแล้วยอมไปดูใหม่")]
        [Min(0f)]
        [SerializeField] private float recheckBusySeconds = 30f;

        [Header("Imperfection")]
        [SerializeField] private ImperfectionSettings imperfection = new ImperfectionSettings();

        [Tooltip("ลืมแล้วยืนนิ่งกี่วินาทีก่อนคิดใหม่")]
        [Min(0f)]
        [SerializeField] private float lapsePauseSeconds = 3f;

        public IReadOnlyList<AIGoalSO> Goals => goals;
        public IReadOnlyList<AIActionSO> Actions => actions;
        public FactSO AtTargetFact => atTargetFact;
        public FactSO AtHomeFact => atHomeFact;
        public float Inertia => inertia;
        public FeelingSO Frustration => frustration;
        public float FrustrationPerFailure => frustrationPerFailure;
        public float UnmetWantFrustrationPerMinute => unmetWantFrustrationPerMinute;
        public float UnmetWantFrustrationCap => unmetWantFrustrationCap;
        public float FulfilledWantRelief => fulfilledWantRelief;
        public float FailureMemorySeconds => failureMemorySeconds;
        public float RecheckBusySeconds => recheckBusySeconds;
        public ImperfectionSettings Imperfection => imperfection;
        public float LapsePauseSeconds => lapsePauseSeconds;

        public float SpeedMultiplier(float wellbeing) => Mathf.Lerp(hurriedSpeed, relaxedSpeed, wellbeing / MeterSO.Max);
        public float UseTimeMultiplier(float wellbeing) => Mathf.Lerp(hurriedUseTime, relaxedUseTime, wellbeing / MeterSO.Max);

        // ชีวิตแย่ คิดถี่ขึ้น ชีวิตดี คิดห่างลง
        public float ThinkIntervalAt(float wellbeing) => thinkInterval * Mathf.Lerp(hurriedThink, relaxedThink, wellbeing / MeterSO.Max);
    }
}
