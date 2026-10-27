using UnityEngine;
using ProjectCook.AI;
using ProjectCook.Core;
using ProjectCook.Dialogue;
using ProjectCook.Quests;

namespace ProjectCook.NPC
{
    /// <summary>ต่อสมองกลาง (AgentBrain) เข้ากับโลกของ NPC: หยุดตอนผู้เล่นคุยด้วย และหายอยากเมื่อผู้เล่นทำเควสให้</summary>
    // ร้านค้า เควส และการให้ของยังเป็นของความสามารถเดิม ความสามารถอ่านใจ NPC ได้ผ่าน INPCMind เท่านั้น
    [RequireComponent(typeof(NPCCharacter))]
    [RequireComponent(typeof(AgentBrain))]
    public class NPCBrain : MonoBehaviour, INPCMind
    {
        [Tooltip("คุยจบแล้วยืนรอกี่วินาทีก่อนทำแผนต่อ เผื่อผู้เล่นจะคุยอีก")]
        [Min(0f)]
        [SerializeField] private float afterTalkWaitSeconds = 5f;

        private NPCCharacter character;
        private AgentBrain brain;
        private bool inDialogue;

        // แคชไว้ตอน subscribe เพราะตอนหยุดเกม Instance อาจคืน null ก่อน OnDestroy
        private GameModeManager gameMode;

        public bool IsCraving => brain.IsWanting;

        private void Awake()
        {
            character = GetComponent<NPCCharacter>();
            brain = GetComponent<AgentBrain>();
        }

        private void OnEnable()
        {
            MessageBus<DialogueStarted>.Subscribe(HandleDialogueStarted);
            MessageBus<QuestCompleted>.Subscribe(HandleQuestCompleted);
        }

        private void OnDisable()
        {
            MessageBus<DialogueStarted>.Unsubscribe(HandleDialogueStarted);
            MessageBus<QuestCompleted>.Unsubscribe(HandleQuestCompleted);
            inDialogue = false;
        }

        private void Start()
        {
            gameMode = GameModeManager.Instance;
            if (gameMode != null) gameMode.ModeChanged += HandleModeChanged;
        }

        private void OnDestroy()
        {
            if (gameMode != null) gameMode.ModeChanged -= HandleModeChanged;
        }

        private void HandleDialogueStarted(DialogueStarted message)
        {
            if (message.SpeakerId != character.SpeakerId) return;

            inDialogue = true;
            brain.Hold("Talking with the player");
        }

        // ร้านค้าและกล่องให้ของเปิดต่อหลังบทสนทนาปิด จึงรอจนกลับไปเดินสำรวจจริงก่อน แล้วยืนรออีกครู่เผื่อผู้เล่นจะคุยอีก
        private void HandleModeChanged(GameMode mode)
        {
            if (mode != GameMode.Exploring || !inDialogue) return;

            inDialogue = false;
            brain.Release(afterTalkWaitSeconds, "Waiting for the player");
        }

        private void HandleQuestCompleted(QuestCompleted message)
        {
            if (message.GiverId == character.Profile.NpcId) brain.FulfillWants();
        }
    }
}
