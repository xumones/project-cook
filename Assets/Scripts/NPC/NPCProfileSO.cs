using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace ProjectCook.NPC
{
    /// <summary>ข้อมูลประจำตัว NPC หนึ่งคน: ชื่อ หน้าตา font และประโยคที่พูดเป็นของตัวเอง</summary>
    [CreateAssetMenu(fileName = "NPC_", menuName = "ProjectCook/NPC/Profile")]
    public class NPCProfileSO : ScriptableObject
    {
        /// <summary>ชุดประโยคให้สุ่ม ไม่พูดซ้ำอันเดิมติดกัน</summary>
        [System.Serializable]
        public class LineSet
        {
            [SerializeField] private LocalizedString[] variants;

            [System.NonSerialized] private int lastIndex = -1;

            public bool TryPick(out LocalizedString line)
            {
                int count = variants != null ? variants.Length : 0;
                if (count == 0)
                {
                    line = null;
                    return false;
                }

                int index = Random.Range(0, count);
                if (count > 1 && index == lastIndex) index = (index + 1) % count;

                lastIndex = index;
                line = variants[index];
                return true;
            }
        }

        [Header("Identity")]
        [SerializeField] private LocalizedString displayName;
        [SerializeField] private TMP_FontAsset font;

        [Tooltip("ภาพตัวละคร ตั้งค่าไว้ก่อน ยังไม่ถูกใช้งาน")]
        [SerializeField] private Sprite characterSprite;

        [Header("Lines")]
        [Tooltip("ทักทายตอนเริ่มคุย ว่างไว้ = ขึ้นเมนูทันที")]
        [SerializeField] private LineSet greeting;

        [Tooltip("คุยเล่น ใช้โดย NPCChat")]
        [SerializeField] private LineSet chat;

        [Tooltip("ตอบรับเมื่อผู้เล่นส่งของให้เฉย ๆ โดยไม่มีเควส ใช้โดย NPCReceiver")]
        [SerializeField] private LineSet received;

        public LocalizedString DisplayName => displayName;
        public TMP_FontAsset Font => font;
        public Sprite CharacterSprite => characterSprite;
        public LineSet Greeting => greeting;
        public LineSet Chat => chat;
        public LineSet Received => received;
    }
}
