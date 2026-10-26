using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>อารมณ์ (เช่น หงุดหงิด): เพิ่มขึ้นเมื่อเกิดเหตุการณ์ แล้วค่อย ๆ จางกลับไป 0 เอง</summary>
    [CreateAssetMenu(fileName = "Feeling_", menuName = "ProjectCook/AI/Feeling")]
    public class FeelingSO : MeterSO
    {
        [Tooltip("จางลงกี่หน่วยต่อนาที")]
        [Min(0f)]
        [SerializeField] private float fadePerMinute = 33f;

        [Tooltip("ความมั่นคงที่เปลี่ยนต่อ 1 หน่วยของอารมณ์นี้ ติดลบ = อารมณ์แย่")]
        [SerializeField] private float wellbeingPerPoint = -0.3f;

        public override float Advance(float value, float minutes) => Mathf.Clamp(value - fadePerMinute * minutes, 0f, Max);

        public override float WellbeingOffset(float value) => value * wellbeingPerPoint;

        public override string Describe(float value)
        {
            if (value <= 0f) return "ไม่มี";
            return value < Max * 0.5f ? "เล็กน้อย" : "แรง";
        }
    }
}
