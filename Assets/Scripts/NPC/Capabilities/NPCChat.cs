using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Dialogue;

namespace ProjectCook.NPC.Capabilities
{
    /// <summary>คุยเล่น: สุ่มประโยค Chat จาก profile ของ NPC</summary>
    public class NPCChat : NPCCapability
    {
        [SerializeField] private LocalizedString chatLabel;

        public override void BuildOptions(List<DialogueOption> options)
        {
            if (!Npc.Profile.Chat.TryPick(out LocalizedString line)) return;

            options.Add(new DialogueOption { Label = chatLabel, Response = line });
        }
    }
}
