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
        [Tooltip("รหัส NPC ไม่ซ้ำกันทั้งเกม ใช้ผูกโน้ตเควสกับ NPC")]
        [SerializeField] private string npcId;

        [SerializeField] private LocalizedString displayName;
        [SerializeField] private TMP_FontAsset font;

        [Tooltip("ภาพตัวละคร แสดงในหน้าต่างคุย เว้นว่าง = ซ่อนรูป")]
        [SerializeField] private Sprite characterSprite;

        [Header("Lines")]
        [Tooltip("ทักทายตอนเริ่มคุย ว่างไว้ = ขึ้นเมนูทันที")]
        [SerializeField] private LineSet greeting;

        [Tooltip("คุยเล่น ใช้โดย NPCChat")]
        [SerializeField] private LineSet chat;

        [Header("Random Orders")]
        [Tooltip("ประโยคของเควสสุ่ม ใส่ {0:list:{}|, | and } ตรงที่จะแทรกรายชื่อวัตถุดิบ และต้องเปิด Smart ใน String Table")]
        [SerializeField] private LineSet orderOffer;

        [SerializeField] private LineSet orderReminder;

        [Tooltip("ส่งไม่ผ่าน (เกรด F)")]
        [SerializeField] private LineSet orderFail;

        [Tooltip("ส่งผ่าน (เกรด D ถึง A)")]
        [SerializeField] private LineSet orderPass;

        [Tooltip("ส่งได้เกรด S")]
        [SerializeField] private LineSet orderExcellent;

        public string NpcId => npcId;
        public LocalizedString DisplayName => displayName;
        public TMP_FontAsset Font => font;
        public Sprite CharacterSprite => characterSprite;
        public LineSet Greeting => greeting;
        public LineSet Chat => chat;
        public LineSet OrderOffer => orderOffer;
        public LineSet OrderReminder => orderReminder;
        public LineSet OrderFail => orderFail;
        public LineSet OrderPass => orderPass;
        public LineSet OrderExcellent => orderExcellent;
    }
}
