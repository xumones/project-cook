using UnityEngine;

namespace ProjectCook.Dialogue
{
    /// <summary>
    /// Utility Class สำหรับแปลงข้อมูลระหว่าง JSON Text และ DialogData Object
    /// </summary>
    public static class DialogParser
    {
        /// <summary>
        /// แปลงข้อความ JSON String ให้เป็น DialogData ScriptableObject Instance ใหม่ในความจำ
        /// </summary>
        public static DialogData ParseJsonToDialogData(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogWarning("[DialogParser] ข้อความ JSON ว่างเปล่า ไม่สามารถ Parse ได้");
                return null;
            }

            DialogData dialogData = ScriptableObject.CreateInstance<DialogData>();
            bool success = dialogData.PopulateFromJson(jsonText);
            if (!success)
            {
                Object.Destroy(dialogData);
                return null;
            }

            return dialogData;
        }

        /// <summary>
        /// แปลงไฟล์ TextAsset (JSON) ให้เป็น DialogData ScriptableObject Instance
        /// </summary>
        public static DialogData ParseTextAssetToDialogData(TextAsset jsonAsset)
        {
            if (jsonAsset == null)
            {
                Debug.LogWarning("[DialogParser] jsonAsset เป็น null ไม่สามารถ Parse ได้");
                return null;
            }

            DialogData dialogData = ParseJsonToDialogData(jsonAsset.text);
            if (dialogData != null && string.IsNullOrEmpty(dialogData.dialogID))
            {
                dialogData.dialogID = jsonAsset.name;
            }

            return dialogData;
        }

        /// <summary>
        /// ส่งออกข้อมูล DialogData ให้กลายเป็นข้อความ JSON String สำหรับบันทึกลงไฟล์
        /// </summary>
        public static string ExportDialogDataToJson(DialogData data, bool prettyPrint = true)
        {
            if (data == null) return string.Empty;

            DialogDataDTO dto = new DialogDataDTO
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
