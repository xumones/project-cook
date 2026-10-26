using System;
using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>ความไม่สมบูรณ์ของตัวละครหนึ่งตัว ค่าทุกช่องคือผลตอนความไม่สมบูรณ์ที่ใช้จริงเต็ม 1</summary>
    [Serializable]
    public class ImperfectionSettings
    {
        [Tooltip("ค่าตั้งต้นของตัวละครนี้ 0 = ไม่พลาดเลย")]
        [Range(0f, 1f)]
        public float baseValue = 0.2f;

        [Tooltip("ตัวคูณตอนความมั่นคง 0 (แย่สุด)")]
        [Min(0f)]
        public float lowWellbeingMultiplier = 2f;

        [Tooltip("ตัวคูณตอนความมั่นคง 100 (ดีสุด)")]
        [Min(0f)]
        public float highWellbeingMultiplier = 0.5f;

        [Header("Satisficing")]
        [Tooltip("ช่วงคะแนนจากอันดับหนึ่งที่ยังถูกสุ่มเลือกได้ (คะแนนเต็ม 1)")]
        [Range(0f, 1f)]
        public float maxScoreBand = 0.3f;

        [Header("Mistaken Belief")]
        [Tooltip("โอกาสที่เห็นแล้วไม่ทันสังเกตว่าเปลี่ยนไป ความเชื่อเดิมจึงค้างอยู่")]
        [Range(0f, 1f)]
        public float missChance = 0.5f;

        [Min(0f)]
        public float missCooldownSeconds = 30f;

        [Header("Lapse")]
        [Tooltip("ลืมว่ากำลังทำอะไรกี่ครั้งต่อนาที")]
        [Min(0f)]
        public float lapsesPerMinute = 0.5f;

        [Min(0f)]
        public float lapseCooldownSeconds = 120f;
    }
}
