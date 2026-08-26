using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Dialogue.Conditions;

namespace ProjectCook.Dialogue
{
    /// <summary>
    /// โครงสร้างเก็บข้อมูลบทสนทนาที่มีเงื่อนไข (ใช้โดย NPCController เพื่อเลือกบทสนทนาที่เหมาะสมที่สุด)
    /// </summary>
    [System.Serializable]
    public class ConditionalDialogEntry
    {
        public DialogData dialogData;
        [Tooltip("ไฟล์บทสนทนาแบบ JSON (ใช้แทน dialogData ได้)")]
        public TextAsset jsonFile;
        public int priority = 0;
        public List<DialogConditionSO> conditions = new List<DialogConditionSO>();

        // แคชผลการแปลง JSON ไว้ เพราะการ Parse จะสร้าง ScriptableObject Instance ใหม่ทุกครั้ง
        // หากไม่แคช การกดคุยแต่ละครั้งจะสร้าง Object ทิ้งค้างในหน่วยความจำโดยไม่มีใครทำลาย
        private DialogData cachedJsonDialogData;

        public bool AreConditionsMet()
        {
            return DialogConditionUtility.AreAllMet(conditions);
        }

        public DialogData GetEffectiveDialogData()
        {
            if (dialogData != null) return dialogData;

            if (jsonFile != null)
            {
                if (cachedJsonDialogData == null)
                {
                    cachedJsonDialogData = DialogParser.ParseTextAssetToDialogData(jsonFile);
                }
                return cachedJsonDialogData;
            }

            return null;
        }
    }
}
