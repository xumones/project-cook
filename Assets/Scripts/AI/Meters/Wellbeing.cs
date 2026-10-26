using System;
using UnityEngine;

namespace ProjectCook.AI
{
    [Serializable]
    public class WellbeingSettings
    {
        [Tooltip("ความมั่นคงเมื่อไม่มีอะไรดึงขึ้นหรือลง")]
        [Range(0f, MeterSO.Max)]
        public float baseValue = 60f;

        [Tooltip("ขึ้นได้เร็วสุดกี่หน่วยต่อนาที")]
        [Min(0f)]
        public float risePerMinute = 20f;

        [Tooltip("ลงได้เร็วสุดกี่หน่วยต่อนาที")]
        [Min(0f)]
        public float fallPerMinute = 10f;
    }

    /// <summary>ความมั่นคง: ค่ากลางว่าชีวิตตอนนี้โอเคแค่ไหน ใช้ปรับความไม่สมบูรณ์และจังหวะชีวิต ไม่ใช้เลือกเป้าหมาย</summary>
    // แบบ mood ของ RimWorld: เป้าหมายเปลี่ยนทันทีตามมาตรวัด แต่ค่าจริงค่อย ๆ ไหลเข้าหาด้วยความเร็วที่จำกัด
    public class Wellbeing
    {
        private readonly WellbeingSettings settings;

        public float Value { get; private set; }
        public float Target { get; private set; }

        public Wellbeing(WellbeingSettings settings)
        {
            this.settings = settings;
            Value = Target = settings.baseValue;
        }

        public void Advance(MeterSet meters, float minutes)
        {
            Target = Mathf.Clamp(settings.baseValue + meters.WellbeingOffset(), 0f, MeterSO.Max);
            float rate = Target > Value ? settings.risePerMinute : settings.fallPerMinute;
            Value = Mathf.MoveTowards(Value, Target, rate * minutes);
        }

        public void Restore(float value) => Value = Mathf.Clamp(value, 0f, MeterSO.Max);
    }
}
