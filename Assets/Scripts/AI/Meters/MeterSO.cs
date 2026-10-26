using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>มาตรวัดหนึ่งตัวของตัวละคร (0–100) แต่ละชนิดย่อยกำหนดเองว่าค่าเปลี่ยนตามเวลายังไงและมีผลต่อความมั่นคงแค่ไหน</summary>
    public abstract class MeterSO : ScriptableObject
    {
        public const float Max = 100f;

        [Tooltip("รหัสใน save ห้ามซ้ำกับมาตรวัดอื่น และห้ามเปลี่ยนหลังมี save")]
        [SerializeField] private string meterId;

        [Tooltip("ชื่อที่แสดงในหน้า debug")]
        [SerializeField] private string label;

        public string MeterId => meterId;
        public string Label => label;

        /// <summary>ค่าหลังเวลาผ่านไป minutes นาที โดยยังไม่มีอะไรมาเติม</summary>
        public abstract float Advance(float value, float minutes);

        /// <summary>ค่านี้ดึงเป้าหมายความมั่นคงขึ้นหรือลงเท่าไหร่ 0 = ไม่มีผล</summary>
        public virtual float WellbeingOffset(float value) => 0f;
    }
}
