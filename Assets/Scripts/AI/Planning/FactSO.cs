using System;
using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>ข้อเท็จจริงจริง/เท็จหนึ่งข้อ เล่าจากมุมของตัวละครเอง เช่น "อยู่ที่ของแล้ว" "พักแล้ว"</summary>
    // เก็บให้กว้างที่สุด ใครหรืออะไรที่เกี่ยวข้องเป็นหน้าที่ของระบบเลือกเป้า ไม่ใช่ข้อเท็จจริง (แบบ F.E.A.R.)
    [CreateAssetMenu(fileName = "Fact_", menuName = "ProjectCook/AI/Fact")]
    public class FactSO : ScriptableObject
    {
        [Tooltip("ชื่อที่แสดงในหน้า debug")]
        [SerializeField] private string label;

        public string Label => label;
    }

    [Serializable]
    public struct FactCondition
    {
        public FactSO fact;
        public bool value;
    }
}
