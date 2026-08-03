using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Dialogue.Conditions;

namespace ProjectCook.Dialogue
{
    /// <summary>
    /// Data structure สำหรับเก็บข้อมูลตัวเลือกคำตอบ 1 ข้อ (Multiple Choice Option)
    /// </summary>
    [System.Serializable]
    public class DialogChoice
    {
        [Tooltip("ข้อความที่จะแสดงบนปุ่มตัวเลือก")]
        public string choiceText;

        [Tooltip("ID ของโหนดบทสนทนาถัดไปเมื่อเลือกข้อนี้ (ถ้าว่างไว้จะจบการคุย)")]
        public string nextNodeID;

        [Tooltip("Action ID สำหรับเรียกใช้ฟังก์ชันพิเศษของ NPC (เช่น 'OPEN_SHOP', 'GIVE_RECIPE', 'CLOSE')")]
        public string actionID;

        [Tooltip("ชื่อ Flag Key ในเกมที่ต้องการเช็คแบบรวดเร็ว (สำหรับใช้กับ JSON Data เช่น 'has_recipe')")]
        public string conditionFlag;

        [Tooltip("รายการเงื่อนไขที่จะให้ตัวเลือกนี้โชว์บน UI (ทุกข้อต้องผ่านทั้งหมด)")]
        public List<DialogConditionSO> conditions = new List<DialogConditionSO>();

        /// <summary>
        /// เช็คว่าเงื่อนไขทุกข้อของตัวเลือกนี้ผ่านหรือไม่
        /// </summary>
        public bool AreConditionsMet()
        {
            // 1. เช็ค conditionFlag แบบง่าย (ถ้ามีระบุไว้)
            if (!string.IsNullOrEmpty(conditionFlag))
            {
                if (ProjectCook.Core.GameStateManager.Instance != null)
                {
                    if (ProjectCook.Core.GameStateManager.Instance.GetFlag(conditionFlag) <= 0)
                    {
                        return false;
                    }
                }
            }

            // 2. เช็ค ScriptableObject conditions (ถ้ามีระบุไว้)
            if (conditions != null && conditions.Count > 0)
            {
                foreach (var cond in conditions)
                {
                    if (cond != null && !cond.IsMet())
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
