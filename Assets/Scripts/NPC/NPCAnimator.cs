using UnityEngine;
using ProjectCook.Core;
using ProjectCook.Dialogue;

namespace ProjectCook.NPC
{
    /// <summary>เล่นท่าตอบสนองของ NPC เมื่อผู้เล่นเริ่มคุยหรือเลือกตัวเลือก นอกนั้นปล่อยให้ Animator เล่น Idle</summary>
    [RequireComponent(typeof(NPCCharacter))]
    public class NPCAnimator : MonoBehaviour
    {
        private static readonly int ReactHash = Animator.StringToHash("React");

        [SerializeField] private Animator animator;

        private NPCCharacter character;

        private void Awake()
        {
            character = GetComponent<NPCCharacter>();
        }

        private void OnEnable()
        {
            MessageBus<DialogueStarted>.Subscribe(HandleDialogueStarted);
            MessageBus<DialogueOptionSelected>.Subscribe(HandleOptionSelected);
        }

        private void OnDisable()
        {
            MessageBus<DialogueStarted>.Unsubscribe(HandleDialogueStarted);
            MessageBus<DialogueOptionSelected>.Unsubscribe(HandleOptionSelected);
        }

        private void HandleDialogueStarted(DialogueStarted message) => ReactIfMine(message.SpeakerId);

        private void HandleOptionSelected(DialogueOptionSelected message) => ReactIfMine(message.SpeakerId);

        private void ReactIfMine(string speakerId)
        {
            if (animator == null || speakerId != character.SpeakerId) return;
            animator.SetTrigger(ReactHash);
        }
    }
}
