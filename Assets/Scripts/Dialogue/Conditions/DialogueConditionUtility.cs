using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Dialogue.Conditions
{
    /// <summary>Utility Class สำหรับตรวจสอบเงื่อนไขบทสนทนาแบบรวมศูนย์</summary>
    public static class DialogueConditionUtility
    {
        // เรียงตัวยาวไว้ก่อน ไม่งั้น "=" จะไปจับกลางคำ ">=" ก่อน
        private static readonly (string token, FlagComparison comparison)[] Operators =
        {
            (">=", FlagComparison.GreaterThanOrEqual),
            ("<=", FlagComparison.LessThanOrEqual),
            ("!=", FlagComparison.NotEqual),
            ("=", FlagComparison.Equal),
            (">", FlagComparison.GreaterThan),
            ("<", FlagComparison.LessThan)
        };

        /// <summary>เช็คว่าเงื่อนไขทุกข้อในลิสต์ผ่านหรือไม่ (ลิสต์ว่างหรือ null ถือว่าผ่าน)</summary>
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

        /// <summary>เช็คนิพจน์ flag จาก JSON: "a" = a > 0, "a=2", "a>=1", หลายตัวคั่นด้วย , ต้องผ่านทุกตัว ว่าง = ผ่าน</summary>
        public static bool IsFlagExpressionMet(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return true;

            foreach (string term in expression.Split(','))
            {
                if (!IsTermMet(term.Trim())) return false;
            }
            return true;
        }

        private static bool IsTermMet(string term)
        {
            if (term.Length == 0) return true;

            foreach ((string token, FlagComparison comparison) in Operators)
            {
                int index = term.IndexOf(token);
                if (index <= 0) continue;

                string key = term.Substring(0, index).Trim();
                if (!int.TryParse(term.Substring(index + token.Length).Trim(), out int value))
                {
                    Debug.LogWarning($"[DialogueConditionUtility] อ่านค่าตัวเลขจากเงื่อนไข '{term}' ไม่ได้");
                    return false;
                }
                return IsFlagMet(key, comparison, value);
            }

            return IsFlagMet(term, FlagComparison.GreaterThan, 0);
        }

        /// <summary>เลือกปลายทางจาก branches ตัวแรกที่เข้าเงื่อนไข ไม่มีเลยจึงใช้ fallback</summary>
        public static string ResolveBranch(List<DialogueBranch> branches, string fallback)
        {
            if (branches == null) return fallback;

            for (int i = 0; i < branches.Count; i++)
            {
                DialogueBranch branch = branches[i];
                if (branch == null || string.IsNullOrEmpty(branch.conditionFlag)) continue;

                if (IsFlagExpressionMet(branch.conditionFlag)) return branch.nextNodeID;
            }
            return fallback;
        }

        /// <summary>จุดเช็คเดียวสำหรับ Game Flag ทั้งหมด</summary>
        public static bool IsFlagMet(string flagKey, FlagComparison comparison, int targetValue)
        {
            if (GameFlagsManager.Instance == null)
            {
                Debug.LogWarning("[DialogueConditionUtility] ไม่พบ GameFlagsManager ใน Scene!");
                return false;
            }

            return GameFlagsManager.Instance.CheckFlag(flagKey, comparison, targetValue);
        }
    }
}
