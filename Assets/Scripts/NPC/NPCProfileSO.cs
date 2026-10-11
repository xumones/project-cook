using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Dialogue;

namespace ProjectCook.NPC
{
    /// <summary>ข้อมูลประจำตัว NPC หนึ่งคน: ชื่อ หน้าตา สไตล์ตัวหนังสือ และประโยคที่พูดเป็นของตัวเอง</summary>
    [CreateAssetMenu(fileName = "NPC_", menuName = "ProjectCook/NPC/Profile")]
    public class NPCProfileSO : ScriptableObject, ISpeakerIdentity
    {
        /// <summary>ชุดประโยคให้สุ่ม การสุ่มทำผ่าน LinePicker ของ NPC แต่ละตัว</summary>
        [System.Serializable]
        public class LineSet
        {
            [SerializeField] private LocalizedString[] variants;

            public int Count => variants != null ? variants.Length : 0;

            public LocalizedString this[int index] => variants[index];
        }

        [Header("Identity")]
        [Tooltip("รหัส NPC ไม่ซ้ำกันทั้งเกม ใช้ผูกโน้ตเควสกับ NPC")]
        [SerializeField] private string npcId;

        [SerializeField] private LocalizedString displayName;

        [Tooltip("font และขนาดตัวหนังสือในหน้าต่างคุย เว้นว่าง = หน้าตาปกติ")]
        [SerializeField] private DialogueTextStyleSO textStyle;

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
        public DialogueTextStyleSO TextStyle => textStyle;
        public Sprite CharacterSprite => characterSprite;

        // บทพูดใน cutscene ใช้ไฟล์นี้เป็นผู้พูดได้ตรงๆ โดยไม่ต้องมีตัว NPC ในฉาก
        string ISpeakerIdentity.SpeakerId => npcId;
        Sprite ISpeakerIdentity.Portrait => characterSprite;
        public LineSet Greeting => greeting;
        public LineSet Chat => chat;
        public LineSet OrderOffer => orderOffer;
        public LineSet OrderReminder => orderReminder;
        public LineSet OrderFail => orderFail;
        public LineSet OrderPass => orderPass;
        public LineSet OrderExcellent => orderExcellent;
    }
}
