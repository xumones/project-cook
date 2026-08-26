using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.NPC;
using ProjectCook.Control;
using ProjectCook.Core;
using ProjectCook.Dialogue.UI;

namespace ProjectCook.Dialogue
{
    /// <summary>
    /// Singleton Manager สำหรับควบคุมวงจรการทำงานของบทสนทนาทั้งหมด
    /// </summary>
    public class DialogManager : MonoBehaviour
    {
        public static DialogManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float typingSpeed = 0.03f; // ความเร็วการพิมพ์ตัวอักษร (วินาที/ตัว)

        [Header("Input Settings")]
        [SerializeField] private InputActionReference advanceAction; // ปุ่มกดไปต่อ (เช่น E/Space/Click)

        [Header("UI Reference")]
        [SerializeField] private DialogUI dialogUI;

        private NPCController currentNPC;
        private DialogData currentDialogData;
        private DialogNode currentNode;
        private Coroutine typingCoroutine;

        // แคชตัวหน่วงเวลาของเอฟเฟกต์พิมพ์ เพื่อไม่ให้ Allocate ใหม่ทุกตัวอักษร
        private WaitForSeconds cachedTypingDelay;
        private float cachedTypingSpeed = -1f;

        private bool isDialogueActive = false;
        private bool isTyping = false;
        private bool isWaitingForChoice = false;

        // เฟรมล่าสุดที่ผู้เล่นกดเลือกคำตอบบน UI
        // ใช้กันคลิกเดียวถูกนับซ้ำเป็น "กดไปต่อ" ในเฟรมเดียวกัน เพราะ wasPressedThisFrame ยังเป็น true ทั้งเฟรม
        private int lastChoiceSelectedFrame = -1;

        public bool IsDialogueActive => isDialogueActive;
        public bool IsTyping => isTyping;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            advanceAction?.action?.Enable();
        }

        private void OnDisable()
        {
            advanceAction?.action?.Disable();
        }

        private void Update()
        {
            if (!isDialogueActive || isWaitingForChoice) return;

            // ข้ามเฟรมที่เพิ่งกดเลือกคำตอบไป เพื่อไม่ให้คลิกเดียวทั้งเลือกคำตอบและข้ามข้อความบรรทัดถัดไปรวดเดียว
            if (Time.frameCount == lastChoiceSelectedFrame) return;

            // ตรวจสอบการกดปุ่มเปลี่ยนบรรทัด/ข้ามข้อความ
            if (WasAdvancePressed())
            {
                OnAdvancePressed();
            }
        }

        /// <summary>
        /// เริ่มต้นบทสนทนากับ NPC โดยใช้ไฟล์ JSON (TextAsset)
        /// </summary>
        public void StartDialog(NPCController npc, TextAsset jsonAsset)
        {
            if (jsonAsset == null) return;
            DialogData data = DialogParser.ParseTextAssetToDialogData(jsonAsset);
            if (data != null)
            {
                StartDialog(npc, data);
            }
        }

        /// <summary>
        /// เริ่มต้นบทสนทนากับ NPC
        /// </summary>
        public void StartDialog(NPCController npc, DialogData dialogData)
        {
            if (npc == null || dialogData == null) return;

            currentNPC = npc;
            currentDialogData = dialogData;
            isDialogueActive = true;

            // 1. หยุดการเคลื่อนที่ของผู้เล่นและปลดล็อกเคอร์เซอร์เมาส์
            SetPlayerControl(false);

            // 2. แสดงผล UI หน้าต่างบทสนทนา
            if (dialogUI != null)
            {
                dialogUI.ShowUI();
            }

            // 3. เริ่มโหนดแรกสุด
            DialogNode startNode = currentDialogData.GetStartNode();
            if (startNode != null)
            {
                DisplayNode(startNode);
            }
            else
            {
                Debug.LogWarning($"[DialogManager] ไม่พบโหนดเริ่มต้นใน DialogData: {dialogData.name}");
                EndDialog();
            }
        }

        /// <summary>
        /// แสดงผลโหนดบทสนทนา
        /// </summary>
        public void DisplayNode(DialogNode node)
        {
            if (node == null)
            {
                EndDialog();
                return;
            }

            currentNode = node;
            isWaitingForChoice = false;

            if (dialogUI != null)
            {
                dialogUI.HideChoices();
            }

            // กำหนดชื่อผู้พูด
            string speakerName = !string.IsNullOrEmpty(node.speakerNameOverride)
                ? node.speakerNameOverride
                : (currentNPC != null ? currentNPC.NPCName : currentDialogData.defaultSpeakerName);

            if (dialogUI != null)
            {
                dialogUI.SetSpeakerInfo(speakerName, node.speakerAvatar);
            }

            // เล่นเสียงพากย์ถ้ามี
            if (node.voiceClip != null)
            {
                AudioSource.PlayClipAtPoint(node.voiceClip, UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform.position : Vector3.zero);
            }

            // เริ่มเอฟเฟกต์พิมพ์ตัวอักษรทีละตัว
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            typingCoroutine = StartCoroutine(TypeTextRoutine(node.dialogueText));
        }

        private IEnumerator TypeTextRoutine(string fullText)
        {
            isTyping = true;

            // ใส่ข้อความเต็มลง TMP ครั้งเดียวแล้วค่อยๆ เปิดเผยทีละตัวอักษร
            // ไม่มีการต่อสตริงและไม่มีการสร้าง Mesh ใหม่ระหว่างพิมพ์ (Zero Allocation)
            int totalCharacters = dialogUI != null ? dialogUI.PrepareTypewriterText(fullText) : 0;
            WaitForSeconds delay = GetTypingDelay();

            for (int i = 1; i <= totalCharacters; i++)
            {
                dialogUI.SetVisibleCharacterCount(i);
                yield return delay;
            }

            isTyping = false;
            OnTypingComplete();
        }

        /// <summary>
        /// ดึงตัวหน่วงเวลาที่แคชไว้ (สร้างใหม่เฉพาะตอนค่า typingSpeed ถูกปรับเปลี่ยน)
        /// เพื่อไม่ให้เกิดการ Allocate Object ใหม่ทุกตัวอักษรที่พิมพ์
        /// </summary>
        private WaitForSeconds GetTypingDelay()
        {
            if (cachedTypingDelay == null || !Mathf.Approximately(cachedTypingSpeed, typingSpeed))
            {
                cachedTypingSpeed = typingSpeed;
                cachedTypingDelay = new WaitForSeconds(typingSpeed);
            }

            return cachedTypingDelay;
        }

        private void OnTypingComplete()
        {
            // เมื่อพิมพ์ข้อความเสร็จแล้ว ให้ตรวจว่าโหนดนี้มีตัวเลือกคำตอบหรือไม่
            if (currentNode != null && currentNode.HasChoices)
            {
                ShowChoicesForCurrentNode();
            }
        }

        private void ShowChoicesForCurrentNode()
        {
            if (currentNode == null || dialogUI == null) return;

            // คัดกรองเฉพาะตัวเลือกที่ผ่านเงื่อนไข
            List<DialogChoice> validChoices = new List<DialogChoice>();
            foreach (var choice in currentNode.choices)
            {
                if (choice != null && choice.AreConditionsMet())
                {
                    validChoices.Add(choice);
                }
            }

            if (validChoices.Count > 0)
            {
                isWaitingForChoice = true;
                dialogUI.DisplayChoices(validChoices, SelectChoice);
            }
            else
            {
                // ถ้าไม่มีตัวเลือกที่ผ่านเงื่อนไขเลย ให้ข้ามไปยัง nextNodeID หรือจบ
                AdvanceOrEnd();
            }
        }

        /// <summary>
        /// ถูกเรียกเมื่อผู้เล่นคลิกเลือกตัวเลือกคำตอบบน UI
        /// </summary>
        public void SelectChoice(DialogChoice choice)
        {
            if (choice == null) return;

            isWaitingForChoice = false;
            lastChoiceSelectedFrame = Time.frameCount;

            if (dialogUI != null)
            {
                dialogUI.HideChoices();
            }

            // 1. ถ้าระบุ actionID ให้ส่งยิง Action ไปยัง NPC
            if (!string.IsNullOrEmpty(choice.actionID))
            {
                if (currentNPC != null)
                {
                    currentNPC.HandleAction(choice.actionID);
                }
            }

            // 2. ขยับไปยังโหนดถัดไป หรือจบการคุย
            if (!string.IsNullOrEmpty(choice.nextNodeID))
            {
                DialogNode nextNode = currentDialogData.GetNode(choice.nextNodeID);
                DisplayNode(nextNode);
            }
            else
            {
                EndDialog();
            }
        }

        private void OnAdvancePressed()
        {
            if (isTyping)
            {
                // หากกำลังพิมพ์อยู่ แล้วผู้เล่นกดปุ่ม ให้พิมพ์ให้เสร็จเต็มบรรทัดทันที
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                }
                isTyping = false;

                if (dialogUI != null && currentNode != null)
                {
                    dialogUI.SetDialogueText(currentNode.dialogueText);
                }

                OnTypingComplete();
            }
            else if (!isWaitingForChoice)
            {
                // หากพิมพ์เสร็จแล้วและไม่มีตัวเลือกค้างอยู่ ให้เปลี่ยนไปโหนดถัดไป
                AdvanceOrEnd();
            }
        }

        private void AdvanceOrEnd()
        {
            if (currentNode != null && !string.IsNullOrEmpty(currentNode.nextNodeID))
            {
                DialogNode nextNode = currentDialogData.GetNode(currentNode.nextNodeID);
                DisplayNode(nextNode);
            }
            else
            {
                EndDialog();
            }
        }

        /// <summary>
        /// จบการคุยบทสนทนา
        /// </summary>
        public void EndDialog()
        {
            isDialogueActive = false;
            isTyping = false;
            isWaitingForChoice = false;

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            if (dialogUI != null)
            {
                dialogUI.HideUI();
            }

            // คืนค่าการเคลื่อนที่และล็อกเคอร์เซอร์กลับให้ผู้เล่น
            SetPlayerControl(true);

            if (currentNPC != null)
            {
                currentNPC.OnDialogEnded();
                currentNPC = null;
            }

            currentDialogData = null;
            currentNode = null;
        }

        private void SetPlayerControl(bool enableControl)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                player.SetControlActive(enableControl);
            }

            // ขอ/คืนสิทธิ์การใช้ตัวชี้เมาส์ผ่าน CursorManager แทนการสั่ง Cursor ตรงๆ
            // เพื่อไม่ให้แย่งสถานะกับระบบอื่น (เช่น ระบบคีบวัตถุดิบในโหมดทำอาหาร)
            if (enableControl)
            {
                CursorManager.ReleaseUnlock(this);
            }
            else
            {
                CursorManager.RequestUnlock(this);
            }
        }

        private bool WasAdvancePressed()
        {
            if (advanceAction?.action != null && advanceAction.action.enabled)
            {
                if (advanceAction.action.WasPressedThisFrame()) return true;
            }
            
            bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

            return ePressed || spacePressed || mousePressed;
        }
    }
}
