using System.Collections.Generic;

namespace ProjectCook.AI
{
    /// <summary>ความจำของตัวละคร: สิ่งที่ "เชื่อ" ว่าเป็นจริงพร้อมเวลาที่เห็นล่าสุด และสิ่งที่เพิ่งทำพลาดซึ่งควรเลี่ยงไปสักพัก</summary>
    // ความเชื่อเปลี่ยนเมื่อเซนเซอร์เห็นเท่านั้น ความจริงเปลี่ยนไปโดยไม่มีใครเห็น ตัวละครก็ยังเชื่อแบบเดิม (เข้าใจผิดได้)
    // เวลาเป็นวินาทีที่ผู้เรียกส่งมาเอง ทดสอบได้โดยไม่ต้องรอเวลาจริง
    public class AgentMemory
    {
        private struct Belief
        {
            public bool Value;
            public float SeenAt;
        }

        private readonly Dictionary<object, Belief> beliefs = new Dictionary<object, Belief>();
        private readonly Dictionary<object, float> failedUntil = new Dictionary<object, float>();

        public void Observe(object subject, bool value, float now) =>
            beliefs[subject] = new Belief { Value = value, SeenAt = now };

        /// <summary>false = ไม่เคยเห็นเลย age คือเห็นล่าสุดกี่วินาทีก่อน</summary>
        public bool TryRecall(object subject, float now, out bool value, out float age)
        {
            if (beliefs.TryGetValue(subject, out Belief belief))
            {
                value = belief.Value;
                age = now - belief.SeenAt;
                return true;
            }

            value = false;
            age = 0f;
            return false;
        }

        public void RememberFailure(object subject, float now, float forSeconds) => failedUntil[subject] = now + forSeconds;

        public bool HasFailedRecently(object subject, float now)
        {
            if (!failedUntil.TryGetValue(subject, out float until)) return false;
            if (now < until) return true;

            failedUntil.Remove(subject);
            return false;
        }

        /// <summary>ลืมทุกอย่าง ใช้ตอนตัวละครถูกย้ายกลับจุดประจำหลังฉากถูกซ่อน</summary>
        public void Clear()
        {
            beliefs.Clear();
            failedUntil.Clear();
        }
    }
}
