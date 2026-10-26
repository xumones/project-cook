using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>ความอยาก: เพิ่มขึ้นเองตามเวลา ตัวละครทำให้หายเองไม่ได้ ต้องมีคนอื่นมาเติมให้ (เช่น ผู้เล่นทำอาหารให้)</summary>
    [CreateAssetMenu(fileName = "Want_", menuName = "ProjectCook/AI/Want")]
    public class WantSO : MeterSO
    {
        [Tooltip("เพิ่มขึ้นกี่หน่วยต่อนาที")]
        [Min(0f)]
        [SerializeField] private float risePerMinute = 5f;

        [Tooltip("ถึงค่านี้แล้วถือว่า \"อยาก\" ระบบที่ขึ้นกับความอยากเริ่มทำงาน")]
        [Range(0f, Max)]
        [SerializeField] private float threshold = 60f;

        public bool IsActive(float value) => value >= threshold;

        public override float Advance(float value, float minutes) => Mathf.Clamp(value + risePerMinute * minutes, 0f, Max);

        public override string Describe(float value) => IsActive(value) ? "อยาก" : "ยังไม่อยาก";
    }
}
