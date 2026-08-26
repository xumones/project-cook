using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using ProjectCook.Interaction;
using ProjectCook.Dialogue;
using ProjectCook.Dialogue.Conditions;

namespace ProjectCook.NPC
{
    /// <summary>
    /// คลาสหลักสำหรับ NPC ทุกตัวในเกม Implement IInteractable และ INPCActionHandler
    /// </summary>
    public class NPCController : MonoBehaviour, IInteractable, INPCActionHandler
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

        public string NPCID => npcID;
        public string NPCName => npcName;

        /// <summary>
        /// ถูกเรียกจาก PlayerInteractor เมื่อผู้เล่นเล็ง NPC แล้วกด E
        /// </summary>
        public void Interact(PlayerInteractor interactor)
        {
            onInteract?.Invoke(this);

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

        /// <summary>
        /// ค้นหาบทสนทนาที่ผ่านเงื่อนไขและมีความสำคัญสูงสุด
        /// </summary>
        public DialogueDataSO GetValidDialogue()
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
                    cachedDefaultJsonDialogue = DialogueParser.ParseTextAsset(defaultJsonDialogue);
                }
                return cachedDefaultJsonDialogue;
            }

            return null;
        }

        /// <summary>
        /// รับคำสั่ง Action ID ที่ผู้เล่นเลือกจากตัวเลือกบทสนทนา (Implement INPCActionHandler)
        /// </summary>
        public void HandleAction(string actionID)
        {
            if (string.IsNullOrEmpty(actionID)) return;

            // ยิง Event กระจายสัญญาณคำสั่งไปให้ Component อื่นๆ รับไปทำงานต่อ
            onActionTriggered?.Invoke(actionID);

            // ถ้ามี INPCActionHandler ตัวอื่นบน GameObject นี้ (เช่น สคริปต์ร้านค้า MerchantShop) สั่งให้ทำงานด้วย
            var handlers = GetComponents<INPCActionHandler>();
            foreach (var handler in handlers)
            {
                if (handler != null && handler != (INPCActionHandler)this)
                {
                    handler.HandleAction(actionID);
                }
            }
        }

        /// <summary>
        /// ถูกเรียกจาก DialogueManager เมื่อเล่นบทสนทนาจบลง
        /// </summary>
        public void OnDialogueEnded()
        {
            onDialogueComplete?.Invoke(this);
        }
    }
}
