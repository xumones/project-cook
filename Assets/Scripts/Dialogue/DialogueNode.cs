using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Dialogue
{
    /// <summary>
    /// Data structure สำหรับเก็บข้อมูลโหนดบทสนทนา 1 โหนดใน Dialogue Tree
    /// </summary>
    [System.Serializable]
    public class DialogueNode
    {
        [Tooltip("ID ประจำโหนด (เช่น 'start', 'ask_menu', 'node_01')")]
        public string nodeID;

        [Tooltip("ชื่อผู้พูดในโหนดนี้ (หากเว้นว่างไว้จะใช้ชื่อ default ของ NPC)")]
        public string speakerNameOverride;

        [TextArea(3, 6)]
        [Tooltip("ข้อความบทสนทนาที่จะพิมพ์บนหน้าจอ")]
        public string dialogueText;

        [Tooltip("ภาพตัวละคร/พอร์ตเทรตผู้พูด")]
        public Sprite speakerAvatar;

        [Tooltip("เสียงพากย์หรือเสียงประกอบ (ถ้ามี)")]
        public AudioClip voiceClip;

        [Tooltip("ID หรือ Path ของ Event เสียงพากย์/SFX (สำหรับใช้กับ FMOD/Audio Event System หรือ JSON Data)")]
        public string audioEvent;

        [Tooltip("ID ของโหนดถัดไปอัตโนมัติกรณีไม่มีตัวเลือกคำตอบ")]
        public string nextNodeID;

        [Tooltip("รายการตัวเลือกคำตอบ (ถ้ามีหลายข้อ หน้าต่างตัวเลือกจะเด้งขึ้นมาแทนการผ่านบรรทัดอัตโนมัติ)")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();

        /// <summary>
        /// เช็คว่าโหนดนี้มีตัวเลือกคำตอบหรือไม่
        /// </summary>
        public bool HasChoices => choices != null && choices.Count > 0;
    }
}
