using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;
using ProjectCook.Dialogue;
using ProjectCook.Quests;

namespace ProjectCook.Cinematics
{
    /// <summary>ตัดสินว่า cutscene ไหนเล่นเมื่อไหร่: ฟังข่าว เปิดคำขอ รอจนเล่นได้ แล้วสั่ง CutscenePlayer และจำลง save</summary>
    // เล่นได้เมื่ออยู่โหมดเดินสำรวจและ Stage ของ cutscene นั้นอยู่ในฉากที่เปิดอยู่ ไม่งั้นคำขอค้างรอต่อ
    public class CutsceneScheduler : SceneSingleton<CutsceneScheduler>, ISaveSection
    {
        [Tooltip("cutscene ทุกอันในเกม ใช้จับคู่กับข่าวและหาคืนจาก save cutscene ใหม่ต้องใส่ที่นี่")]
        [SerializeField] private List<CutsceneSO> cutscenes = new List<CutsceneSO>();

        private readonly CutsceneQueue queue = new CutsceneQueue();
        private readonly List<CutsceneStage> stages = new List<CutsceneStage>();

        // แคชไว้ตอน subscribe เพราะตอนหยุดเกม Instance อาจคืน null ก่อน OnDestroy
        private GameModeManager gameMode;
        private CutscenePlayer player;

        public string SectionKey => "cutscenes";

        private void OnEnable()
        {
            MessageBus<WorldEventTriggered>.Subscribe(HandleWorldEvent);
            MessageBus<QuestStarted>.Subscribe(HandleQuestStarted);
            MessageBus<QuestCompleted>.Subscribe(HandleQuestCompleted);
            MessageBus<DialogueEnded>.Subscribe(HandleDialogueEnded);
        }

        private void OnDisable()
        {
            MessageBus<WorldEventTriggered>.Unsubscribe(HandleWorldEvent);
            MessageBus<QuestStarted>.Unsubscribe(HandleQuestStarted);
            MessageBus<QuestCompleted>.Unsubscribe(HandleQuestCompleted);
            MessageBus<DialogueEnded>.Unsubscribe(HandleDialogueEnded);
        }

        private void Start()
        {
            gameMode = GameModeManager.Instance;
            player = CutscenePlayer.Instance;
            gameMode.ModeChanged += HandleModeChanged;
            player.Completed += queue.Complete;
            SaveService.Instance?.Register(this);
        }

        protected override void OnDestroy()
        {
            if (gameMode != null) gameMode.ModeChanged -= HandleModeChanged;
            if (player != null) player.Completed -= queue.Complete;
            SaveService.Instance?.Unregister(this);
            base.OnDestroy();
        }

        public void Register(CutsceneStage stage)
        {
            stages.Add(stage);
            TryPlayNext();
        }

        public void Unregister(CutsceneStage stage) => stages.Remove(stage);

        public string Capture() => JsonUtility.ToJson(queue.Capture());

        public void Restore(string json)
        {
            var data = JsonUtility.FromJson<CutsceneQueue.SaveData>(json);
            data.pending.RemoveAll(IsUnknown);
            queue.Restore(data);
            TryPlayNext();
        }

        private void HandleWorldEvent(WorldEventTriggered message) => RequestMatching(CutsceneTrigger.WorldEvent, message.EventId);

        private void HandleQuestStarted(QuestStarted message) => RequestMatching(CutsceneTrigger.QuestStarted, message.QuestId);

        private void HandleQuestCompleted(QuestCompleted message) => RequestMatching(CutsceneTrigger.QuestCompleted, message.QuestId);

        private void HandleDialogueEnded(DialogueEnded message) => RequestMatching(CutsceneTrigger.DialogueEnded, message.SpeakerId);

        private void RequestMatching(CutsceneTrigger kind, string id)
        {
            foreach (CutsceneSO cutscene in cutscenes)
            {
                if (cutscene != null && cutscene.IsTriggeredBy(kind, id) && ConditionsMet(cutscene)) queue.Request(cutscene);
            }
            TryPlayNext();
        }

        // เช็คตอนเปิดคำขอเท่านั้น คำขอที่เปิดแล้วอยู่จนเล่นจบ แม้สถานะเควสจะเปลี่ยนทีหลัง
        private static bool ConditionsMet(CutsceneSO cutscene)
        {
            QuestLog log = QuestLog.Instance;
            foreach (QuestSO quest in cutscene.RequiredQuests)
            {
                if (quest != null && (log == null || !log.IsCompleted(quest.QuestId))) return false;
            }
            return true;
        }

        private void HandleModeChanged(GameMode mode) => TryPlayNext();

        private void TryPlayNext()
        {
            if (player == null || player.IsPlaying || gameMode.CurrentMode != GameMode.Exploring) return;

            foreach (string id in queue.Pending)
            {
                CutsceneSO cutscene = cutscenes.Find(c => c != null && c.CutsceneId == id);
                if (cutscene == null) continue;

                // สื่อเล่นได้ทุกที่ ส่วน Timeline ต้องรอ Stage ของมันอยู่ในฉากที่เปิด
                CutsceneStage stage = cutscene.IsMedia ? null : stages.Find(s => s.isActiveAndEnabled && s.Cutscene == cutscene);
                if (!cutscene.IsMedia && stage == null) continue;

                player.Play(cutscene, stage);
                return;
            }
        }

        private bool IsUnknown(string cutsceneId)
        {
            if (cutscenes.Exists(c => c != null && c.CutsceneId == cutsceneId)) return false;

            Debug.LogWarning($"[CutsceneScheduler] ไม่พบ cutscene '{cutsceneId}' ในรายการ Cutscenes ทิ้งคำขอนี้", this);
            return true;
        }
    }
}
