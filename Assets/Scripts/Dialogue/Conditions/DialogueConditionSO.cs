using UnityEngine;

namespace ProjectCook.Dialogue.Conditions
{
    /// <summary>
    /// Abstract Base Class สำหรับ ScriptableObject ตรวจสอบเงื่อนไขบทสนทนาและตัวเลือกคำตอบ
    /// </summary>
    public abstract class DialogueConditionSO : ScriptableObject
    {
        /// <summary>
        /// ตรวจสอบว่าเงื่อนไขนี้ผ่านหรือไม่
        /// </summary>
        public abstract bool IsMet();
    }
}
