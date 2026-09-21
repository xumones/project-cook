using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Core;
using ProjectCook.Dialogue;

namespace ProjectCook.NPC
{
    /// <summary>NPC ใน scene: คุยด้วยได้เสมอ ความสามารถอื่นมาจาก NPCCapability ที่ใส่เพิ่มบน GameObject เดียวกัน</summary>
    public class NPCCharacter : MonoBehaviour, IInteractable, IAimHintProvider, IDialogueSpeaker
    {
        [SerializeField] private NPCProfileSO profile;

        private NPCCapability[] capabilities;

        public NPCProfileSO Profile => profile;

        public LocalizedString DisplayName => profile.DisplayName;
        public TMP_FontAsset Font => profile.Font;
        public Sprite Portrait => profile.CharacterSprite;

        private void Awake()
        {
            capabilities = GetComponents<NPCCapability>();
        }

        public AimHint GetAimHint() => AimHint.Talk;

        public void Interact()
        {
            if (profile == null)
            {
                Debug.LogWarning($"[NPC] {name} ยังไม่ได้ตั้ง Profile", this);
                return;
            }

            DialogueRunner.Instance?.Begin(this);
        }

        public bool TryGetGreeting(out LocalizedString greeting) => profile.Greeting.TryPick(out greeting);

        public void CollectOptions(List<DialogueOption> options)
        {
            for (int i = 0; i < capabilities.Length; i++)
            {
                capabilities[i].BuildOptions(options);
            }
        }
    }
}
