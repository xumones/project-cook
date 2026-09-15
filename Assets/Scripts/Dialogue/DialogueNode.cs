using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Dialogue.Conditions;

namespace ProjectCook.Dialogue
{
    /// <summary>Data structure สำหรับเก็บข้อมูลโหนดบทสนทนา 1 โหนดใน Dialogue Tree</summary>
    [System.Serializable]
    public class DialogueNode
    {
        [Tooltip("ID ประจำโหนด (เช่น 'start', 'ask_menu', 'node_01')")]
        public string nodeID;

        [Tooltip("ชื่อผู้พูดในโหนดนี้ (หากเว้นว่างไว้จะใช้ชื่อ default ของ NPC)")]
        public string speakerNameOverride;

        [TextArea(3, 6)]
        [Tooltip("ข้อความบทสนทนาที่จะพิมพ์บนหน้าจอ ใช้ {key} ดึงคำพูดประจำตัวจาก DialogueLinesSO ของผู้พูดได้")]
        public string dialogueText;

        [Tooltip("ภาพตัวละคร/พอร์ตเทรตผู้พูด")]
        public Sprite speakerAvatar;

        [Tooltip("ID ของโหนดถัดไปอัตโนมัติกรณีไม่มีตัวเลือกคำตอบ ถูก branches ทับได้")]
        public string nextNodeID;

        [Tooltip("ปลายทางตาม flag ตรวจตามลำดับ ตัวแรกที่เข้าเงื่อนไขชนะ ไม่เข้าเลยจึงใช้ nextNodeID")]
        public List<DialogueBranch> branches = new List<DialogueBranch>();

        [Tooltip("รายการตัวเลือกคำตอบ (ถ้ามีหลายข้อ หน้าต่างตัวเลือกจะเด้งขึ้นมาแทนการผ่านบรรทัดอัตโนมัติ)")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();

        [Tooltip("ยืมตัวเลือกของโหนดอื่นมาใช้แทน choices ของตัวเอง ใช้ทำข้อความต่างกันแต่เมนูเดียวกัน")]
        public string choicesFrom;

        /// <summary>เช็คว่าโหนดนี้มีตัวเลือกคำตอบหรือไม่ (นับรวมที่ยืมมาด้วย)</summary>
        public bool HasChoices => (choices != null && choices.Count > 0) || !string.IsNullOrEmpty(choicesFrom);

        /// <summary>หาโหนดถัดไปจริง ๆ ตอน auto-advance ให้ NPC พูดต่างกันตามสถานะได้ในไฟล์เดียว</summary>
        public string ResolveNextNodeID() => DialogueConditionUtility.ResolveBranch(branches, nextNodeID);
    }
}
