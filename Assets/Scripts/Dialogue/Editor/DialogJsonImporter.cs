#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectCook.Dialogue.Editor
{
    /// <summary>
    /// Unity Editor Script สำหรับสร้าง ScriptableObject (DialogData) จากไฟล์ข้อความ JSON
    /// </summary>
    public static class DialogJsonImporter
    {
        [MenuItem("Assets/Create/ProjectCook/Dialogue/DialogData from Selected JSON", false, 10)]
        public static void CreateDialogDataFromSelectedJson()
        {
            Object selectedObject = Selection.activeObject;
            if (selectedObject == null || !(selectedObject is TextAsset textAsset))
            {
                EditorUtility.DisplayDialog("Import Dialogue JSON", "กรุณาเลือกไฟล์ .json (TextAsset) ใน Project Window ก่อนรันคำสั่งนี้", "ตกลง");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(textAsset);
            if (!assetPath.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Import Dialogue JSON", "ไฟล์ที่เลือกไม่ใช่ไฟล์ .json", "ตกลง");
                return;
            }

            DialogData dialogData = DialogParser.ParseTextAssetToDialogData(textAsset);
            if (dialogData == null)
            {
                EditorUtility.DisplayDialog("Import Dialogue JSON", "เกิดข้อผิดพลาดในการ Parse ไฟล์ JSON กรุณาเช็คโครงสร้างไฟล์", "ตกลง");
                return;
            }

            string dirPath = Path.GetDirectoryName(assetPath);
            string assetName = string.IsNullOrEmpty(dialogData.dialogID) ? textAsset.name : dialogData.dialogID;
            string targetPath = Path.Combine(dirPath, $"{assetName}_Data.asset");
            targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);

            AssetDatabase.CreateAsset(dialogData, targetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = dialogData;

            Debug.Log($"[DialogJsonImporter] สร้าง DialogData Asset สำเร็จที่: {targetPath}");
        }

        // ตรวจสอบว่าเมนูนี้กดเปิดใช้งานได้เฉพาะเมื่อเลือก TextAsset
        [MenuItem("Assets/Create/ProjectCook/Dialogue/DialogData from Selected JSON", true)]
        public static bool ValidateCreateDialogDataFromSelectedJson()
        {
            Object selectedObject = Selection.activeObject;
            return selectedObject != null && selectedObject is TextAsset;
        }
    }
}
#endif
