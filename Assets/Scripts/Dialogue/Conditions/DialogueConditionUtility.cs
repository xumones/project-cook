using System.Collections.Generic;

namespace ProjectCook.Dialogue.Conditions
{
    /// <summary>
    /// Utility Class สำหรับตรวจสอบรายการเงื่อนไข DialogueConditionSO แบบรวมศูนย์
    /// (ใช้ร่วมกันระหว่าง DialogueChoice และ ConditionalDialogueEntry เพื่อไม่ให้ Logic ซ้ำกัน)
    /// </summary>
    public static class DialogueConditionUtility
    {
        /// <summary>
        /// เช็คว่าเงื่อนไขทุกข้อในลิสต์ผ่านหรือไม่ (ลิสต์ว่างหรือ null ถือว่าผ่าน)
        /// </summary>
        public static bool AreAllMet(List<DialogueConditionSO> conditions)
        {
            if (conditions == null || conditions.Count == 0) return true;

            foreach (var cond in conditions)
            {
                if (cond != null && !cond.IsMet())
                {
                    return false;
                }
            }

            return true;
        }
    }
}
