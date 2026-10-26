using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>ทุกอย่างที่การกระทำต้องใช้ตอนลงมือ: ตัวละคร ขา มาตรวัด และเป้าหมายที่สมองเลือกไว้ให้</summary>
    public class AgentContext
    {
        public AgentContext(object owner, Transform body, AgentMover mover, MeterSet meters)
        {
            Owner = owner;
            Body = body;
            Mover = mover;
            Meters = meters;
        }

        /// <summary>ตัวตนที่ใช้จอง SmartObject</summary>
        public object Owner { get; }

        public Transform Body { get; }
        public AgentMover Mover { get; }
        public MeterSet Meters { get; }

        /// <summary>ของที่เป้าหมายตอนนี้ต้องใช้ null = เป้าหมายไม่ต้องใช้ของ</summary>
        public SmartObject Target { get; set; }

        public Transform HomeSpot { get; set; }

        /// <summary>ตัวคูณเวลาใช้ของ ผ่อนคลาย = ใช้นานกว่าปกติ</summary>
        public float UseTimeMultiplier { get; set; } = 1f;
    }
}
