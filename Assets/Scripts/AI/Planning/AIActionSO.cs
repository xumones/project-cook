using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>การกระทำหนึ่งอย่างที่ตัววางแผนต่อเป็นแผนได้: ทำได้เมื่อเงื่อนไขครบ ทำแล้วข้อเท็จจริงเปลี่ยนตามผล ชนิดย่อยบอกว่าลงมือจริงยังไง</summary>
    // ราคาใช้บอกความชอบ ไม่ใช่เวลาจริง ของที่เฉพาะกว่าตั้งให้ถูกกว่าของทั่วไป แผนจะเลือกเองถ้าทำได้
    public abstract class AIActionSO : ScriptableObject
    {
        [SerializeField] private List<FactCondition> preconditions = new List<FactCondition>();

        [SerializeField] private List<FactCondition> effects = new List<FactCondition>();

        [Min(0.01f)]
        [SerializeField] private float baseCost = 1f;

        public IReadOnlyList<FactCondition> Preconditions => preconditions;
        public IReadOnlyList<FactCondition> Effects => effects;
        public float BaseCost => baseCost;

        /// <summary>เงื่อนไขที่บอกด้วยข้อเท็จจริงไม่ได้ เช่น ต้องมีของเป้าหมายอยู่ ตัวที่ทำไม่ได้ตอนนี้ไม่ถูกนำไปวางแผน</summary>
        public abstract bool CanRun(AgentContext context);

        public abstract ActionRun Begin(AgentContext context);
    }
}
