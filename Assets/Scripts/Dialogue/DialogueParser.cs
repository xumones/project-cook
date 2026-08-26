using UnityEngine;

namespace ProjectCook.Dialogue
{
    /// <summary>
    /// Utility Class สำหรับแปลงข้อมูลระหว่าง JSON Text และ DialogueDataSO Object
    /// </summary>
    public static class DialogueParser
    {
        /// <summary>
        /// แปลงข้อความ JSON String ให้เป็น DialogueDataSO ScriptableObject Instance ใหม่ในความจำ
        /// </summary>
        public static DialogueDataSO ParseJson(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogWarning("[DialogueParser] ข้อความ JSON ว่างเปล่า ไม่สามารถ Parse ได้");
                return null;
            }

            DialogueDataSO dialogueData = ScriptableObject.CreateInstance<DialogueDataSO>();
            bool success = dialogueData.PopulateFromJson(jsonText);
            if (!success)
            {
                Object.Destroy(dialogueData);
                return null;
            }

            return dialogueData;
        }

        /// <summary>
        /// แปลงไฟล์ TextAsset (JSON) ให้เป็น DialogueDataSO ScriptableObject Instance
        /// </summary>
        public static DialogueDataSO ParseTextAsset(TextAsset jsonAsset)
        {
            if (jsonAsset == null)
            {
                Debug.LogWarning("[DialogueParser] jsonAsset เป็น null ไม่สามารถ Parse ได้");
                return null;
            }

            DialogueDataSO dialogueData = ParseJson(jsonAsset.text);
            if (dialogueData != null && string.IsNullOrEmpty(dialogueData.dialogID))
            {
                dialogueData.dialogID = jsonAsset.name;
            }

            return dialogueData;
        }

        /// <summary>
        /// ส่งออกข้อมูล DialogueDataSO ให้กลายเป็นข้อความ JSON String สำหรับบันทึกลงไฟล์
        /// </summary>
        public static string ExportToJson(DialogueDataSO data, bool prettyPrint = true)
        {
            if (data == null) return string.Empty;

            DialogueDataDTO dto = new DialogueDataDTO
            {
                dialogID = data.dialogID,
                defaultSpeakerName = data.defaultSpeakerName,
                startNodeID = data.startNodeID,
                nodes = data.nodes
            };

            return JsonUtility.ToJson(dto, prettyPrint);
        }
    }
}
