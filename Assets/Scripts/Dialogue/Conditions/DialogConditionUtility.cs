using System.Collections.Generic;

namespace ProjectCook.Dialogue.Conditions
{
    /// <summary>
    /// Utility Class สำหรับตรวจสอบรายการเงื่อนไข DialogConditionSO แบบรวมศูนย์
    /// (ใช้ร่วมกันระหว่าง DialogChoice และ ConditionalDialogEntry เพื่อไม่ให้ Logic ซ้ำกัน)
    /// </summary>
    public static class DialogConditionUtility
    {
        /// <summary>
        /// เช็คว่าเงื่อนไขทุกข้อในลิสต์ผ่านหรือไม่ (ลิสต์ว่างหรือ null ถือว่าผ่าน)
        /// </summary>
        public static bool AreAllMet(List<DialogConditionSO> conditions)
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
