using System;
using UnityEngine;
using Random = System.Random;

namespace ProjectCook.AI
{
    /// <summary>ทอยว่าตัวละครพลาดแบบไหนตอนไหน ใช้ตัวสุ่มของตัวเองที่ตั้งเมล็ดได้ เทสต์และดีบักจึงได้ผลซ้ำเดิม</summary>
    // ความไม่สมบูรณ์ที่ใช้จริง = ค่าตั้งต้น × ตัวคูณตามความมั่นคง ยิ่งชีวิตแย่ยิ่งพลาดง่าย
    // แต่ละแบบมีช่วงพักของตัวเอง ความผิดพลาดจึงเป็นสีสัน ไม่ใช่ความวุ่นวาย
    public class ImperfectionRoller
    {
        private readonly ImperfectionSettings settings;
        private readonly Random random;
        private float nextMissAt = float.NegativeInfinity;
        private float nextLapseAt = float.NegativeInfinity;

        public ImperfectionRoller(ImperfectionSettings settings, int seed)
        {
            this.settings = settings;
            random = new Random(seed);
        }

        public float Effective(float wellbeing)
        {
            float multiplier = Mathf.Lerp(settings.lowWellbeingMultiplier, settings.highWellbeingMultiplier, wellbeing / MeterSO.Max);
            return Mathf.Clamp01(settings.baseValue * multiplier);
        }

        /// <summary>เป้าหมายที่คะแนนต่ำกว่าอันดับหนึ่งไม่เกินสัดส่วนนี้ยังถูกเลือกได้ (เลือกแค่พอใช้ได้)</summary>
        public float ScoreBand(float wellbeing) => settings.maxScoreBand * Effective(wellbeing);

        /// <summary>เลข 0 ถึงน้อยกว่า count สำหรับสุ่มเลือกในกลุ่ม</summary>
        public int Pick(int count) => random.Next(count);

        /// <summary>เห็นของแล้วแต่ไม่ทันสังเกตว่าเปลี่ยนไป</summary>
        public bool RollMiss(float wellbeing, float now)
        {
            if (now < nextMissAt || !Chance(settings.missChance * Effective(wellbeing))) return false;

            nextMissAt = now + settings.missCooldownSeconds;
            return true;
        }

        /// <summary>ลืมกลางคันว่ากำลังทำอะไร minutes คือเวลาที่ผ่านไปตั้งแต่ทอยครั้งก่อน</summary>
        public bool RollLapse(float wellbeing, float minutes, float now)
        {
            if (now < nextLapseAt || !Chance(settings.lapsesPerMinute * minutes * Effective(wellbeing))) return false;

            nextLapseAt = now + settings.lapseCooldownSeconds;
            return true;
        }

        private bool Chance(float probability) => probability > 0f && random.NextDouble() < probability;
    }
}
