using TMPro;
using UnityEngine;

namespace ProjectCook.Dialogue
{
    /// <summary>หน้าตาตัวหนังสือของผู้พูดในหน้าต่างคุย ผู้พูดหลายตัวใช้ไฟล์เดียวกันได้</summary>
    [CreateAssetMenu(fileName = "TextStyle_", menuName = "ProjectCook/Dialogue/Text Style")]
    public class DialogueTextStyleSO : ScriptableObject
    {
        [Tooltip("ใช้กับชื่อและประโยค เว้นว่าง = ใช้ font ของหน้าต่างคุย")]
        [SerializeField] private TMP_FontAsset font;

        [Tooltip("0 = ใช้ขนาดของหน้าต่างคุย")]
        [Min(0)] [SerializeField] private float nameFontSize;

        [Tooltip("0 = ใช้ขนาดของหน้าต่างคุย")]
        [Min(0)] [SerializeField] private float dialogueFontSize;

        public TMP_FontAsset Font => font;
        public float NameFontSize => nameFontSize;
        public float DialogueFontSize => dialogueFontSize;
    }
}
