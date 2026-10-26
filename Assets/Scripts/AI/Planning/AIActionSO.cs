using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>การกระทำหนึ่งอย่างที่ตัววางแผนต่อเป็นแผนได้: ทำได้เมื่อเงื่อนไขครบ ทำแล้วข้อเท็จจริงเปลี่ยนตามผล</summary>
    // ราคาใช้บอกความชอบ ไม่ใช่เวลาจริง ของที่เฉพาะกว่าตั้งให้ถูกกว่าของทั่วไป แผนจะเลือกเองถ้าทำได้
    [CreateAssetMenu(fileName = "Action_", menuName = "ProjectCook/AI/Action")]
    public class AIActionSO : ScriptableObject
    {
        [SerializeField] private List<FactCondition> preconditions = new List<FactCondition>();

        [SerializeField] private List<FactCondition> effects = new List<FactCondition>();

        [Min(0.01f)]
        [SerializeField] private float baseCost = 1f;

        public IReadOnlyList<FactCondition> Preconditions => preconditions;
        public IReadOnlyList<FactCondition> Effects => effects;
        public float BaseCost => baseCost;
    }
}
