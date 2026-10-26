using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.AI
{
    [Serializable]
    public class Consideration
    {
        public MeterSO meter;

        [Tooltip("ยิ่งมากยิ่งไม่สนตอนแรงกดดันน้อย แต่เร่งด่วนมากตอนแรงกดดันสูง (1 = เส้นตรง)")]
        [Min(0.1f)]
        public float power = 3f;

        public float Score(MeterSet meters) => Mathf.Pow(meter.Pressure(meters.Get(meter)), power);
    }

    /// <summary>เป้าหมาย: สภาพที่ตัวละครอยากให้เป็น คะแนนบอกว่าอยากแค่ไหนตอนนี้ ชั้นสูงกว่าชนะเสมอแม้คะแนนต่ำกว่า</summary>
    // คะแนน = น้ำหนัก × ผลคูณของทุกปัจจัย ปัจจัยใหม่ (เช่น ช่วงเวลาในวัน) เพิ่มเป็นปัจจัยอีกตัวได้โดยไม่แก้เป้าหมายเดิม
    [CreateAssetMenu(fileName = "Goal_", menuName = "ProjectCook/AI/Goal")]
    public class AIGoalSO : ScriptableObject
    {
        [Tooltip("ชื่อที่แสดงในหน้า debug")]
        [SerializeField] private string label;

        [Tooltip("ชั้นความสำคัญ ดูชั้นสูงสุดที่มีเป้าหมายทำได้ก่อน แล้วค่อยเทียบคะแนนในชั้นนั้น (หน้าที่ > ความอยาก)")]
        [SerializeField] private int rank;

        [Min(0f)]
        [SerializeField] private float weight = 1f;

        [SerializeField] private List<Consideration> considerations = new List<Consideration>();

        [Tooltip("ของที่จะใช้ต้องประกาศว่าช่วยมาตรวัดนี้ ว่าง = เป้าหมายนี้ไม่ต้องใช้ของ")]
        [SerializeField] private MeterSO targetMeter;

        [Tooltip("สภาพที่ทำให้เป้าหมายนี้สำเร็จ")]
        [SerializeField] private List<FactCondition> desiredState = new List<FactCondition>();

        public string Label => label;
        public int Rank => rank;
        public MeterSO TargetMeter => targetMeter;
        public IReadOnlyList<FactCondition> DesiredState => desiredState;
        public IReadOnlyList<Consideration> Considerations => considerations;

        public float Score(MeterSet meters)
        {
            float score = weight;
            foreach (Consideration consideration in considerations)
            {
                if (consideration.meter != null) score *= consideration.Score(meters);
            }
            return score;
        }
    }
}
