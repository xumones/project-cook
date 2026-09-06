using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Dialogue.Conditions
{
    /// <summary>
    /// Utility Class สำหรับตรวจสอบเงื่อนไขบทสนทนาแบบรวมศูนย์
    /// (ใช้ร่วมกันระหว่าง DialogueChoice, ConditionalDialogueEntry และ GameFlagConditionSO เพื่อไม่ให้ Logic ซ้ำกัน)
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

        /// <summary>
        /// จุดเช็คเดียวสำหรับ Game Flag ทั้งหมด: ใช้ทั้งจากช่อง conditionFlag (string ด่วนสำหรับ JSON)
        /// และจาก GameFlagConditionSO (asset) เพื่อไม่ให้มีสองเส้นทางที่ยิงเข้า GameStateManager แยกกัน
        /// </summary>
        public static bool IsFlagMet(string flagKey, FlagComparison comparison, int targetValue)
        {
            if (GameStateManager.Instance == null)
            {
                Debug.LogWarning("[DialogueConditionUtility] ไม่พบ GameStateManager ใน Scene!");
                return false;
            }

            return GameStateManager.Instance.CheckFlag(flagKey, comparison, targetValue);
        }
    }
}
