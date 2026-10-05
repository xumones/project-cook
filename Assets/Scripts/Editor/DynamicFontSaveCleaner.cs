using TMPro;
using UnityEditor;

namespace ProjectCook.Editor
{
    /// <summary>ล้างตัวอักษรที่ฟอนต์ TMP แบบ Dynamic สร้างไว้ระหว่างใช้งานก่อนบันทึก ไฟล์ฟอนต์จึงไม่โผล่ใน git ทุกครั้งที่กด Play</summary>
    // ตัวอักษรถูกสร้างใหม่ในหน่วยความจำเองเมื่อมีข้อความใช้ ใช้กับฟอนต์ที่ติ๊ก Clear Dynamic Data On Build เท่านั้น
    internal class DynamicFontSaveCleaner : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string path in paths)
            {
                if (!path.EndsWith(".asset")) continue;

                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font == null || font.atlasPopulationMode != AtlasPopulationMode.Dynamic) continue;
                if (!new SerializedObject(font).FindProperty("m_ClearDynamicDataOnBuild").boolValue) continue;

                font.ClearFontAssetData(true);
            }
            return paths;
        }
    }
}
