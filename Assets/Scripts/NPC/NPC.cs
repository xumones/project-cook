using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ProjectCook.Interaction;
using ProjectCook.Dialogue;
using ProjectCook.Dialogue.Conditions;

namespace ProjectCook.NPC
{
    /// <summary>
    /// โครงสร้างเก็บข้อมูลบทสนทนาที่มีเงื่อนไข
    /// </summary>
    [System.Serializable]
    public class ConditionalDialogEntry
    {
        public DialogData dialogData;
        [Tooltip("ไฟล์บทสนทนาแบบ JSON (ใช้แทน dialogData ได้)")]
        public TextAsset jsonFile;
        public int priority = 0;
        public List<DialogConditionSO> conditions = new List<DialogConditionSO>();

        public bool AreConditionsMet()
        {
            if (conditions == null || conditions.Count == 0) return true;
            foreach (var cond in conditions)
            {
                if (cond != null && !cond.IsMet()) return false;
            }
            return true;
        }

        public DialogData GetEffectiveDialogData()
        {
            if (dialogData != null) return dialogData;
            if (jsonFile != null) return DialogParser.ParseTextAssetToDialogData(jsonFile);
            return null;
        }
    }

    /// <summary>
    /// คลาสหลักสำหรับ NPC ทุกตัวในเกม Implement IInteractable และ INPCActionHandler
    /// </summary>
    public class NPC : MonoBehaviour, IInteractable, INPCActionHandler
    {
        [Header("NPC Profile")]
        [SerializeField] private string npcID;
        [SerializeField] private string npcName;

        [Header("Dialogues")]
        [Tooltip("บทสนทนาเริ่มต้น กรณีไม่มีเงื่อนไขใดๆ ผ่านเลย")]
        [SerializeField] private DialogData defaultDialog;

        [Tooltip("ไฟล์บทสนทนาเริ่มต้นแบบ JSON (ใช้แทน defaultDialog กรณีใช้ระบบ Data-Driven)")]
        [SerializeField] private TextAsset defaultJsonDialog;

        [Tooltip("รายการบทสนทนาแบบมีเงื่อนไข (จะเลือกอันที่ Priority สูงสุดและเงื่อนไขผ่าน)")]
        [SerializeField] private List<ConditionalDialogEntry> conditionalDialogs = new List<ConditionalDialogEntry>();

        [Header("Event Hooks")]
        [Tooltip("เหตุการณ์ที่เกิดเมื่อผู้เล่นกด Interact คุยกับ NPC")]
        public UnityEvent<NPC> onInteract;

        [Tooltip("เหตุการณ์ที่เกิดเมื่อคุยจบทุกโหนด")]
        public UnityEvent<NPC> onDialogComplete;

        [Tooltip("เหตุการณ์ที่เกิดเมื่อมีการยิง Action ID จากตัวเลือกคำตอบ")]
        public UnityEvent<string> onActionTriggered;

        public string NPCID => npcID;
        public string NPCName => npcName;

        /// <summary>
        /// ถูกเรียกจาก PlayerInteractor เมื่อผู้เล่นเล็ง NPC แล้วกด E
        /// </summary>
        public void Interact(PlayerInteractor interactor)
        {
            onInteract?.Invoke(this);

            DialogData dialogToPlay = GetValidDialog();
            if (dialogToPlay != null)
            {
                if (DialogManager.Instance != null)
                {
                    DialogManager.Instance.StartDialog(this, dialogToPlay);
                }
                else
                {
                    Debug.LogWarning($"[NPC] DialogManager ไม่ถูกติดตั้งใน Scene! ไม่สามารถเริ่มคุยกับ {npcName} ได้", this);
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
        public DialogData GetValidDialog()
        {
            ConditionalDialogEntry bestMatch = null;

            foreach (var entry in conditionalDialogs)
            {
                if (entry != null && entry.GetEffectiveDialogData() != null && entry.AreConditionsMet())
                {
                    if (bestMatch == null || entry.priority > bestMatch.priority)
                    {
                        bestMatch = entry;
                    }
                }
            }

            if (bestMatch != null)
            {
                return bestMatch.GetEffectiveDialogData();
            }

            if (defaultDialog != null) return defaultDialog;
            if (defaultJsonDialog != null) return DialogParser.ParseTextAssetToDialogData(defaultJsonDialog);

            return null;
        }

        /// <summary>
        /// รับคำสั่ง Action ID ที่ผู้เล่นเลือกจากตัวเลือกบทสนทนา (Implement INPCActionHandler)
        /// </summary>
        public void HandleAction(string actionID)
        {
            if (string.IsNullOrEmpty(actionID)) return;

            Debug.Log($"[NPC] {npcName} ได้รับ Action ID: {actionID}");

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
        /// ถูกเรียกจาก DialogManager เมื่อเล่นบทสนทนาจบลง
        /// </summary>
        public void OnDialogEnded()
        {
            onDialogComplete?.Invoke(this);
        }
    }
}
