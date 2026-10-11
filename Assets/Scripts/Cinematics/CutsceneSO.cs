using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Video;
using ProjectCook.NPC;
using ProjectCook.Quests;

namespace ProjectCook.Cinematics
{
    /// <summary>ข่าวชนิดไหนทำให้ cutscene เริ่ม รหัสใน Trigger Id ตีความตามชนิดนี้</summary>
    public enum CutsceneTrigger
    {
        /// <summary>รหัสเหตุการณ์ของ WorldEventTrigger หรือ Finish Event ของ cutscene อื่น</summary>
        WorldEvent,
        /// <summary>รหัสเควส (Quest Id) ตอนผู้เล่นรับเควส</summary>
        QuestStarted,
        /// <summary>รหัสเควส (Quest Id) ตอนเควสจบ</summary>
        QuestCompleted,
        /// <summary>รหัส NPC (Npc Id) ตอนปิดบทสนทนากับคนนั้น</summary>
        DialogueEnded
    }

    /// <summary>Realtime = โลกเดินต่อระหว่างเล่น, Full = หยุดเวลาทั้งเกมให้ผู้เล่นดูเต็มที่</summary>
    public enum CutsceneKind
    {
        Realtime,
        Full
    }

    /// <summary>วิธีเปลี่ยนภาพตอนเข้าหรือออก: เลื่อนกล้องจากหรือกลับสายตาผู้เล่น, จอดำ, หรือตัดทันที</summary>
    public enum CutsceneTransition
    {
        Blend,
        Fade,
        Cut
    }

    /// <summary>Timeline = เล่นด้วย CutsceneStage ในฉาก, Video / Slides = แสดงเต็มจอจาก Core เล่นได้ทุกที่ไม่ต้องมี Stage</summary>
    public enum CutsceneContent
    {
        Timeline,
        Video,
        Slides
    }

    /// <summary>ภาพนิ่งหนึ่งหน้าของ cutscene แบบ Slides ผู้เล่นกดไปต่อทีละหน้า</summary>
    [Serializable]
    public class MediaSlide
    {
        [Tooltip("ภาพเต็มจอ ว่าง = พื้นดำ")]
        [SerializeField] private Sprite image;

        [Tooltip("ผู้พูด ว่าง = ไม่แสดงชื่อ (เช่น คำบรรยาย)")]
        [SerializeField] private NPCProfileSO speaker;

        [Tooltip("ข้อความในหน้าต่างคุย ว่าง = แสดงแต่ภาพ")]
        [SerializeField] private LocalizedString text;

        public Sprite Image => image;
        public NPCProfileSO Speaker => speaker;
        public LocalizedString Text => text;
        public bool HasText => text != null && !text.IsEmpty;
    }

    /// <summary>ข้อมูลของ cutscene หนึ่งอัน: เล่นเมื่อไหร่ เล่นแบบไหน และส่งข่าวอะไรตอนจบ ตัวฉากจริงคือ CutsceneStage ในฉากพื้นที่</summary>
    [CreateAssetMenu(fileName = "Cutscene_", menuName = "ProjectCook/Cinematics/Cutscene")]
    public class CutsceneSO : ScriptableObject
    {
        [Tooltip("รหัส cutscene ไม่ซ้ำกันทั้งเกม ใช้จำว่าดูแล้ว ห้ามเปลี่ยนหลังมี save")]
        [SerializeField] private string cutsceneId;

        [Header("When")]
        [SerializeField] private CutsceneTrigger trigger = CutsceneTrigger.WorldEvent;

        [Tooltip("WorldEvent = รหัสเหตุการณ์, QuestStarted / QuestCompleted = Quest Id, DialogueEnded = Npc Id")]
        [SerializeField] private string triggerId;

        [Tooltip("ต้องจบเควสเหล่านี้ก่อน ตัวกระตุ้นถึงจะเปิดคำขอ ว่าง = ไม่มีเงื่อนไข")]
        [SerializeField] private List<QuestSO> requiredQuests = new List<QuestSO>();

        [Tooltip("เล่นครั้งเดียวทั้งเกม (cutscene เนื้อเรื่อง) ไม่ติ๊ก = เล่นทุกครั้งที่ตัวกระตุ้นเกิด")]
        [SerializeField] private bool playOnce = true;

        [Header("Content")]
        [Tooltip("Video / Slides หยุดเวลาและเสียงเกมเสมอ และเข้าออกด้วยจอดำ ช่อง Playback ด้านล่างจึงไม่มีผล")]
        [SerializeField] private CutsceneContent content = CutsceneContent.Timeline;

        [Tooltip("ใช้เมื่อ Content = Video")]
        [SerializeField] private VideoClip video;

        [Tooltip("ใช้เมื่อ Content = Slides")]
        [SerializeField] private List<MediaSlide> slides = new List<MediaSlide>();

        [Header("Playback")]
        [SerializeField] private CutsceneKind kind = CutsceneKind.Realtime;

        [SerializeField] private CutsceneTransition transitionIn = CutsceneTransition.Blend;

        [Tooltip("ถ้า Stage ตั้งจุดย้ายผู้เล่นไว้ ตอนออกจะเป็น Fade เสมอ")]
        [SerializeField] private CutsceneTransition transitionOut = CutsceneTransition.Blend;

        [Tooltip("หันกล้องได้ในกรอบจำกัดตอนเป็นมุมสายตาผู้เล่น ไม่ติ๊ก = ล็อกกล้อง")]
        [SerializeField] private bool allowLook = true;

        [Header("After")]
        [Tooltip("ส่งเป็นข่าวเหตุการณ์ตอนจบ (ทั้งดูจบและข้าม) ใช้เริ่มเควสหรือ cutscene ถัดไป ว่าง = ไม่ส่ง")]
        [SerializeField] private string finishEvent;

        public string CutsceneId => cutsceneId;
        public IReadOnlyList<QuestSO> RequiredQuests => requiredQuests;
        public CutsceneContent Content => content;
        public bool IsMedia => content != CutsceneContent.Timeline;
        public VideoClip Video => video;
        public IReadOnlyList<MediaSlide> Slides => slides;
        public bool PlayOnce => playOnce;
        public CutsceneKind Kind => kind;
        public CutsceneTransition TransitionIn => transitionIn;
        public CutsceneTransition TransitionOut => transitionOut;
        public bool AllowLook => allowLook;
        public string FinishEvent => finishEvent;

        /// <summary>ข่าวนี้ตรงกับตัวกระตุ้นของ cutscene นี้ไหม รหัสว่างไม่ตรงกับอะไรเลย</summary>
        public bool IsTriggeredBy(CutsceneTrigger kind, string id) =>
            kind == trigger && !string.IsNullOrEmpty(triggerId) && triggerId == id;
    }
}
