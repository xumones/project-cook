using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using ProjectCook.Dialogue.Conditions;

namespace ProjectCook.Dialogue
{
    /// <summary>
    /// โครงสร้างเก็บข้อมูลบทสนทนาที่มีเงื่อนไข (ใช้โดย NPCController เพื่อเลือกบทสนทนาที่เหมาะสมที่สุด)
    /// </summary>
    [System.Serializable]
    public class ConditionalDialogueEntry
    {
        [FormerlySerializedAs("dialogData")]
        public DialogueDataSO dialogueData;

        [Tooltip("ไฟล์บทสนทนาแบบ JSON (ใช้แทน dialogueData ได้)")]
        public TextAsset jsonFile;

        public int priority = 0;
        public List<DialogueConditionSO> conditions = new List<DialogueConditionSO>();

        // แคชผลการแปลง JSON ไว้ เพราะการ Parse จะสร้าง ScriptableObject Instance ใหม่ทุกครั้ง
        // หากไม่แคช การกดคุยแต่ละครั้งจะสร้าง Object ทิ้งค้างในหน่วยความจำโดยไม่มีใครทำลาย
        private DialogueDataSO cachedJsonDialogue;

        public bool AreConditionsMet()
        {
            return DialogueConditionUtility.AreAllMet(conditions);
        }

        /// <summary>
        /// คืนบทสนทนาที่ใช้งานจริงของรายการนี้ (เลือก Asset ก่อน ถ้าไม่มีจึงแปลงจากไฟล์ JSON)
        /// </summary>
        public DialogueDataSO GetEffectiveDialogue()
        {
            if (dialogueData != null) return dialogueData;

            if (jsonFile != null)
            {
                if (cachedJsonDialogue == null)
                {
                    cachedJsonDialogue = DialogueParser.ParseTextAsset(jsonFile);
                }
                return cachedJsonDialogue;
            }

            return null;
        }
    }
}
