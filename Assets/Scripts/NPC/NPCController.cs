using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using ProjectCook.Core;
using ProjectCook.Dialogue;
using ProjectCook.Dialogue.Conditions;

namespace ProjectCook.NPC
{
    /// <summary>NPC ที่ผู้เล่นคุยด้วยได้ เลือกบทสนทนาตามเงื่อนไขแล้วส่งต่อให้ DialogueManager</summary>
    public class NPCController : MonoBehaviour, IInteractable, IDialogueTarget
    {
        [Header("NPC Profile")]
        [SerializeField] private string npcID;
        [SerializeField] private string npcName;

        [Header("Dialogues")]
        [Tooltip("บทสนทนาเริ่มต้น กรณีไม่มีเงื่อนไขใดๆ ผ่านเลย")]
        [FormerlySerializedAs("defaultDialog")]
        [SerializeField] private DialogueDataSO defaultDialogue;

        [Tooltip("ไฟล์บทสนทนาเริ่มต้นแบบ JSON (ใช้แทน defaultDialogue กรณีใช้ระบบ Data-Driven)")]
        [FormerlySerializedAs("defaultJsonDialog")]
        [SerializeField] private TextAsset defaultJsonDialogue;

        [Tooltip("รายการบทสนทนาแบบมีเงื่อนไข (จะเลือกอันที่ Priority สูงสุดและเงื่อนไขผ่าน)")]
        [FormerlySerializedAs("conditionalDialogs")]
        [SerializeField] private List<ConditionalDialogueEntry> conditionalDialogues = new List<ConditionalDialogueEntry>();

        [Header("Event Hooks")]
        [Tooltip("เหตุการณ์ที่เกิดเมื่อผู้เล่นกด Interact คุยกับ NPC")]
        [SerializeField] private UnityEvent<NPCController> onInteract;

        [Tooltip("เหตุการณ์ที่เกิดเมื่อคุยจบทุกโหนด")]
        [FormerlySerializedAs("onDialogComplete")]
        [SerializeField] private UnityEvent<NPCController> onDialogueComplete;

        [Tooltip("เหตุการณ์ที่เกิดเมื่อมีการยิง Action ID จากตัวเลือกคำตอบ")]
        [SerializeField] private UnityEvent<string> onActionTriggered;

        // แคชผลการแปลง JSON ของบทสนทนาเริ่มต้น (กันการสร้าง ScriptableObject ใหม่ทุกครั้งที่กดคุย)
        private DialogueDataSO cachedDefaultJsonDialogue;

        public string DisplayName => npcName;

        /// <summary>รหัสประจำ NPC ใช้เป็นส่วนหนึ่งของชื่อ flag ที่ผูกกับ NPC ตัวนี้</summary>
        public string NpcID => npcID;

        public void Interact()
        {
            onInteract?.Invoke(this);

            // ให้ component บน NPC เตรียม flag ก่อน เพราะทั้งการเลือกบทสนทนาและตัวเลือกจะอ่าน flag ตอนนี้
            RefreshDialogueState();

            DialogueDataSO dialogueToPlay = GetValidDialogue();
            if (dialogueToPlay != null)
            {
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartDialogue(this, dialogueToPlay);
                }
                else
                {
                    Debug.LogWarning($"[NPC] DialogueManager ไม่ถูกติดตั้งใน Scene! ไม่สามารถเริ่มคุยกับ {npcName} ได้", this);
                }
            }
            else
            {
                Debug.LogWarning($"[NPC] ไม่พบบทสนทนาที่เหมาะสมสำหรับ {npcName}", this);
            }
        }

        /// <summary>บทสนทนาที่ผ่านเงื่อนไขและมี priority สูงสุด</summary>
        private DialogueDataSO GetValidDialogue()
        {
            ConditionalDialogueEntry bestMatch = null;
            DialogueDataSO bestMatchData = null;

            foreach (var entry in conditionalDialogues)
            {
                if (entry == null) continue;

                // เรียก GetEffectiveDialogue เพียงครั้งเดียวต่อ entry แล้วเก็บผลไว้ใช้ต่อ
                DialogueDataSO entryData = entry.GetEffectiveDialogue();
                if (entryData == null) continue;
                if (!entry.AreConditionsMet()) continue;

                if (bestMatch == null || entry.priority > bestMatch.priority)
                {
                    bestMatch = entry;
                    bestMatchData = entryData;
                }
            }

            if (bestMatchData != null) return bestMatchData;

            if (defaultDialogue != null) return defaultDialogue;

            if (defaultJsonDialogue != null)
            {
                if (cachedDefaultJsonDialogue == null)
                {
                    cachedDefaultJsonDialogue = DialogueDataSO.FromTextAsset(defaultJsonDialogue);
                }
                return cachedDefaultJsonDialogue;
            }

            return null;
        }

        /// <summary>รับคำสั่ง Action ID ที่ผู้เล่นเลือกจากตัวเลือกบทสนทนา (Implement INPCActionHandler)</summary>
        public void HandleAction(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return;

            // ยิง Event กระจายสัญญาณคำสั่งไปให้ Component อื่นๆ รับไปทำงานต่อ
            onActionTriggered?.Invoke(actionId);

            // ถ้ามี IDialogueActionHandler ตัวอื่นบน GameObject นี้ (เช่น สคริปต์ร้านค้า) สั่งให้ทำงานด้วย
            foreach (IDialogueActionHandler handler in GetComponents<IDialogueActionHandler>())
            {
                if (handler != null && handler != (IDialogueActionHandler)this)
                {
                    handler.HandleAction(actionId);
                }
            }

            // action เปลี่ยนสถานะได้ (ให้ของ/รับของ) ต้องให้ทุก component คำนวณ flag ใหม่ ไม่ใช่แค่ตัวที่ทำ action
            RefreshDialogueState();
        }

        private void RefreshDialogueState()
        {
            foreach (IDialogueStateListener listener in GetComponents<IDialogueStateListener>())
            {
                listener.RefreshDialogueState();
            }
        }

        public string ResolveText(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;

            foreach (IDialogueTextProvider provider in GetComponents<IDialogueTextProvider>())
            {
                text = provider.ResolveText(text);
            }
            return text;
        }

        /// <summary>ถูกเรียกจาก DialogueManager เมื่อเล่นบทสนทนาจบลง</summary>
        public void OnDialogueEnded()
        {
            onDialogueComplete?.Invoke(this);
        }
    }
}
