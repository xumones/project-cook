using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>ความต้องการพื้นฐาน (อิ่ม พลังงาน สนุก): 100 = เต็มที่ ลดลงเองตามเวลา ตัวละครเติมเองได้ด้วยการใช้ของ</summary>
    // ผลต่อความมั่นคงเป็นช่วงแบบ RimWorld: ช่วงกลางไม่มีผล ต่ำกว่าเกณฑ์ติดลบมากขึ้นเรื่อย ๆ สูงกว่าเกณฑ์ได้โบนัส
    [CreateAssetMenu(fileName = "Need_", menuName = "ProjectCook/AI/Need")]
    public class NeedSO : MeterSO
    {
        [Tooltip("ลดลงกี่หน่วยต่อนาที")]
        [Min(0f)]
        [SerializeField] private float decayPerMinute = 8f;

        [Header("Wellbeing")]
        [Tooltip("ต่ำกว่านี้ถือว่า \"ต่ำ\" และเริ่มดึงความมั่นคงลง")]
        [Range(0f, Max)]
        [SerializeField] private float lowBelow = 30f;

        [Tooltip("สูงกว่านี้ถือว่า \"สูง\" และได้โบนัสความมั่นคง")]
        [Range(0f, Max)]
        [SerializeField] private float highAbove = 85f;

        [Tooltip("ความมั่นคงที่หายไปตอนค่าเหลือ 0 (ไล่ลดจาก 0 ที่เกณฑ์ต่ำ)")]
        [Min(0f)]
        [SerializeField] private float lowPenalty = 20f;

        [Tooltip("ความมั่นคงที่ได้เพิ่มตอนค่าสูงกว่าเกณฑ์สูง")]
        [Min(0f)]
        [SerializeField] private float highBonus = 5f;

        public override float Advance(float value, float minutes) => Mathf.Clamp(value - decayPerMinute * minutes, 0f, Max);

        public override float WellbeingOffset(float value)
        {
            if (value < lowBelow) return -lowPenalty * (1f - value / lowBelow);
            return value > highAbove ? highBonus : 0f;
        }
    }
}
