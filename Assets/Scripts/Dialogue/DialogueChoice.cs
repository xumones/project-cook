using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;
using ProjectCook.Dialogue.Conditions;

namespace ProjectCook.Dialogue
{
    /// <summary>ปลายทางทางเลือกของตัวเลือกคำตอบ ใช้เมื่อ flag ที่ระบุเป็นจริง</summary>
    [System.Serializable]
    public class DialogueBranch
    {
        [Tooltip("Flag ที่ต้อง > 0 ถึงจะไปทางนี้")]
        public string conditionFlag;

        [Tooltip("โหนดถัดไปเมื่อเข้าเงื่อนไข")]
        public string nextNodeID;
    }

    /// <summary>Data structure สำหรับเก็บข้อมูลตัวเลือกคำตอบ 1 ข้อ (Multiple Choice Option)</summary>
    [System.Serializable]
    public class DialogueChoice
    {
        [Tooltip("ข้อความที่จะแสดงบนปุ่มตัวเลือก")]
        public string choiceText;

        [Tooltip("ID ของโหนดบทสนทนาถัดไปเมื่อเลือกข้อนี้ (ถ้าว่างไว้จะจบการคุย) ถูก branches ทับได้")]
        public string nextNodeID;

        [Tooltip("ปลายทางตาม flag ตรวจตามลำดับ ตัวแรกที่เข้าเงื่อนไขชนะ ไม่เข้าเลยจึงใช้ nextNodeID")]
        public List<DialogueBranch> branches = new List<DialogueBranch>();

        [Tooltip("Action ID สำหรับเรียกใช้ฟังก์ชันพิเศษของ NPC (เช่น 'OPEN_SHOP', 'GIVE_RECIPE', 'CLOSE')")]
        public string actionID;

        [Tooltip("นิพจน์ flag สำหรับ JSON เช่น 'has_recipe' (> 0), 'step=2', 'a,b>=1' (ทุกตัวต้องผ่าน)")]
        public string conditionFlag;

        [Tooltip("รายการเงื่อนไขที่จะให้ตัวเลือกนี้โชว์บน UI (ทุกข้อต้องผ่านทั้งหมด)")]
        public List<DialogueConditionSO> conditions = new List<DialogueConditionSO>();

        /// <summary>หาโหนดถัดไปจริง ๆ โดยดู branches ก่อน ให้ NPC ตอบต่างกันได้จากตัวเลือกเดียวของผู้เล่น</summary>
        public string ResolveNextNodeID() => DialogueConditionUtility.ResolveBranch(branches, nextNodeID);

        /// <summary>เช็คว่าเงื่อนไขทุกข้อของตัวเลือกนี้ผ่านหรือไม่</summary>
        public bool AreConditionsMet()
        {
            return DialogueConditionUtility.IsFlagExpressionMet(conditionFlag)
                && DialogueConditionUtility.AreAllMet(conditions);
        }
    }
}
