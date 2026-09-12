using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using ProjectCook.Core;
using ProjectCook.Dialogue.UI;

namespace ProjectCook.Dialogue
{
    /// <summary>ควบคุมวงจรการทำงานของบทสนทนาทั้งหมด</summary>
    public class DialogueManager : SceneSingleton<DialogueManager>
    {
        [Header("Settings")]
        [SerializeField] private float typingSpeed = 0.03f; // ความเร็วการพิมพ์ตัวอักษร (วินาที/ตัว)

        [Header("Input Settings")]
        [SerializeField] private InputActionReference advanceAction; // ปุ่มกดไปต่อ (เช่น E/Space/Click)
        [SerializeField] private InputActionReference navigateAction; // เลื่อนตัวเลือกคำตอบขึ้น/ลง (UI/Navigate: W/S)
        [SerializeField] private InputActionReference submitAction; // ยืนยันตัวเลือกคำตอบที่ไฮไลต์อยู่ (UI/Submit: E)

        [Header("UI Reference")]
        [FormerlySerializedAs("dialogUI")]
        [SerializeField] private DialogueUI dialogueUI;

        private IDialogueTarget currentTarget;
        private DialogueDataSO currentDialogueData;
        private DialogueNode currentNode;
        private string currentDisplayText;
        private Coroutine typingCoroutine;

        // แคชตัวหน่วงเวลาของเอฟเฟกต์พิมพ์ เพื่อไม่ให้ Allocate ใหม่ทุกตัวอักษร
        private WaitForSeconds cachedTypingDelay;
        private float cachedTypingSpeed = -1f;

        private bool isDialogueActive = false;
        private bool isTyping = false;
        private bool isWaitingForChoice = false;

        // ตัวเลือกคำตอบที่ผ่านเงื่อนไขของโหนดปัจจุบัน และ index ที่กำลังไฮไลต์อยู่ (-1 = ยังไม่ได้เลือก ต้องกด W/S ก่อน)
        private readonly List<DialogueChoice> currentChoices = new List<DialogueChoice>();
        private int selectedChoiceIndex = -1;

        private void OnEnable()
        {
            advanceAction?.action?.Enable();
            navigateAction?.action?.Enable();
            submitAction?.action?.Enable();
        }

        private void OnDisable()
        {
            advanceAction?.action?.Disable();
            navigateAction?.action?.Disable();
            submitAction?.action?.Disable();
        }

        private void Update()
        {
            if (!isDialogueActive) return;

            if (isWaitingForChoice)
            {
                HandleChoiceInput();
                return;
            }

            // ตรวจสอบการกดปุ่มเปลี่ยนบรรทัด/ข้ามข้อความ
            if (WasAdvancePressed())
            {
                OnAdvancePressed();
            }
        }

        /// <summary>เริ่มต้นบทสนทนากับ NPC โดยใช้ไฟล์ JSON (TextAsset)</summary>
        public void StartDialogue(IDialogueTarget target, TextAsset jsonAsset)
        {
            if (jsonAsset == null) return;
            DialogueDataSO data = DialogueDataSO.FromTextAsset(jsonAsset);
            if (data != null)
            {
                StartDialogue(target, data);
            }
        }

        /// <summary>เริ่มต้นบทสนทนากับ NPC</summary>
        public void StartDialogue(IDialogueTarget target, DialogueDataSO dialogueData)
        {
            if (target == null || dialogueData == null) return;

            currentTarget = target;
            currentDialogueData = dialogueData;
            isDialogueActive = true;

            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SetMode(GameMode.Dialogue);
            }

            // แสดงผล UI หน้าต่างบทสนทนา
            if (dialogueUI != null)
            {
                dialogueUI.ShowUI();
            }

            // 3. เริ่มโหนดแรกสุด
            DialogueNode startNode = currentDialogueData.GetStartNode();
            if (startNode != null)
            {
                DisplayNode(startNode);
            }
            else
            {
                Debug.LogWarning($"[DialogueManager] ไม่พบโหนดเริ่มต้นใน DialogueDataSO: {dialogueData.name}");
                EndDialogue();
            }
        }

        /// <summary>แสดงผลโหนดบทสนทนา</summary>
        public void DisplayNode(DialogueNode node)
        {
            if (node == null)
            {
                EndDialogue();
                return;
            }

            currentNode = node;
            isWaitingForChoice = false;
            currentChoices.Clear();
            selectedChoiceIndex = -1;

            if (dialogueUI != null)
            {
                dialogueUI.HideChoices();
            }

            // กำหนดชื่อผู้พูด
            string speakerName = !string.IsNullOrEmpty(node.speakerNameOverride)
                ? node.speakerNameOverride
                : (currentTarget != null ? currentTarget.DisplayName : currentDialogueData.defaultSpeakerName);

            if (dialogueUI != null)
            {
                dialogueUI.SetSpeakerInfo(speakerName, node.speakerAvatar);
            }

            // เล่นเสียงพากย์ถ้ามี
            if (node.voiceClip != null)
            {
                AudioSource.PlayClipAtPoint(node.voiceClip, UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform.position : Vector3.zero);
            }

            // เติม placeholder ครั้งเดียวตรงนี้ ทั้งตอนพิมพ์และตอนกดข้ามจะได้ข้อความเดียวกัน
            currentDisplayText = currentTarget != null ? currentTarget.ResolveText(node.dialogueText) : node.dialogueText;

            // เริ่มเอฟเฟกต์พิมพ์ตัวอักษรทีละตัว
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            typingCoroutine = StartCoroutine(TypeTextRoutine(currentDisplayText));
        }

        private IEnumerator TypeTextRoutine(string fullText)
        {
            isTyping = true;

            // ใส่ข้อความเต็มลง TMP ครั้งเดียวแล้วค่อยๆ เปิดเผยทีละตัวอักษร
            // ไม่มีการต่อสตริงและไม่มีการสร้าง Mesh ใหม่ระหว่างพิมพ์ (Zero Allocation)
            int totalCharacters = dialogueUI != null ? dialogueUI.PrepareTypewriterText(fullText) : 0;
            WaitForSeconds delay = GetTypingDelay();

            for (int i = 1; i <= totalCharacters; i++)
            {
                dialogueUI.SetVisibleCharacterCount(i);
                yield return delay;
            }

            isTyping = false;
            OnTypingComplete();
        }

        /// <summary>ดึงตัวหน่วงเวลาที่แคชไว้ (สร้างใหม่เฉพาะตอนค่า typingSpeed ถูกปรับเปลี่ยน)</summary>
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
            if (currentNode == null || dialogueUI == null) return;

            // โหนดที่ตั้ง choicesFrom ไว้ใช้ตัวเลือกของโหนดนั้นแทน จะได้เขียนเมนูเดียวกันไว้ที่เดียว
            DialogueNode source = string.IsNullOrEmpty(currentNode.choicesFrom)
                ? currentNode
                : currentDialogueData.GetNode(currentNode.choicesFrom) ?? currentNode;

            // คัดกรองเฉพาะตัวเลือกที่ผ่านเงื่อนไข
            currentChoices.Clear();
            foreach (var choice in source.choices)
            {
                if (choice != null && choice.AreConditionsMet())
                {
                    currentChoices.Add(choice);
                }
            }

            if (currentChoices.Count > 0)
            {
                isWaitingForChoice = true;
                // ยังไม่ auto-select คำตอบแรก ต้องรอผู้เล่นกด W/S ก่อนถึงจะมีไฮไลต์ขึ้นมา
                selectedChoiceIndex = -1;
                dialogueUI.DisplayChoices(currentChoices);
            }
            else
            {
                // ถ้าไม่มีตัวเลือกที่ผ่านเงื่อนไขเลย ให้ข้ามไปยัง nextNodeID หรือจบ
                AdvanceOrEnd();
            }
        }

        /// <summary>อ่าน Input W (ขึ้น) / S (ลง) / E (ยืนยัน) ระหว่างที่กำลังรอผู้เล่นเลือกคำตอบอยู่</summary>
        private void HandleChoiceInput()
        {
            int direction = ReadNavigateDirection();
            if (direction != 0)
            {
                MoveChoiceSelection(direction);
            }

            // ต้องเลื่อนไฮไลต์ไปเจอคำตอบก่อน (กด W หรือ S อย่างน้อยหนึ่งครั้ง) ถึงจะกด E ยืนยันได้
            if (selectedChoiceIndex >= 0 && WasSubmitPressed())
            {
                ConfirmSelectedChoice();
            }
        }

        private void MoveChoiceSelection(int direction)
        {
            if (currentChoices.Count == 0) return;

            // กด W หรือ S ครั้งแรกแค่เริ่มไฮไลต์ที่ตัวบนสุด ยังไม่เลื่อนตามทิศทาง
            selectedChoiceIndex = selectedChoiceIndex < 0
                ? 0
                : Mathf.Clamp(selectedChoiceIndex - direction, 0, currentChoices.Count - 1);

            if (dialogueUI != null)
            {
                dialogueUI.SetHighlightedIndex(selectedChoiceIndex);
            }
        }

        private void ConfirmSelectedChoice()
        {
            if (selectedChoiceIndex < 0 || selectedChoiceIndex >= currentChoices.Count) return;
            SelectChoice(currentChoices[selectedChoiceIndex]);
        }

        /// <summary>ประมวลผลเมื่อผู้เล่นยืนยันเลือกตัวเลือกคำตอบ</summary>
        private void SelectChoice(DialogueChoice choice)
        {
            if (choice == null) return;

            isWaitingForChoice = false;

            if (dialogueUI != null)
            {
                dialogueUI.HideChoices();
            }

            // ตัดสินปลายทางก่อนยิง action เพราะ action อาจเปลี่ยน flag (เช่น ส่งของแล้วจานว่าง)
            // แต่คำตอบของ NPC ต้องอิงสถานะ "ตอนที่ผู้เล่นเลือก" ไม่ใช่หลังจากนั้น
            string nextNodeID = choice.ResolveNextNodeID();

            // 1. ถ้าระบุ actionID ให้ส่งยิง Action ไปยัง NPC
            if (!string.IsNullOrEmpty(choice.actionID))
            {
                if (currentTarget != null)
                {
                    currentTarget.HandleAction(choice.actionID);
                }
            }

            // 2. ขยับไปยังโหนดถัดไป หรือจบการคุย
            if (!string.IsNullOrEmpty(nextNodeID))
            {
                DialogueNode nextNode = currentDialogueData.GetNode(nextNodeID);
                DisplayNode(nextNode);
            }
            else
            {
                EndDialogue();
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

                if (dialogueUI != null && currentNode != null)
                {
                    dialogueUI.SetDialogueText(currentDisplayText);
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
            string nextNodeID = currentNode != null ? currentNode.ResolveNextNodeID() : null;
            if (!string.IsNullOrEmpty(nextNodeID))
            {
                DialogueNode nextNode = currentDialogueData.GetNode(nextNodeID);
                DisplayNode(nextNode);
            }
            else
            {
                EndDialogue();
            }
        }

        /// <summary>จบการคุยบทสนทนา</summary>
        public void EndDialogue()
        {
            isDialogueActive = false;
            isTyping = false;
            isWaitingForChoice = false;
            currentChoices.Clear();
            selectedChoiceIndex = -1;

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            if (dialogueUI != null)
            {
                dialogueUI.HideUI();
            }

            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SetMode(GameMode.Exploring);
            }

            if (currentTarget != null)
            {
                currentTarget.OnDialogueEnded();
                currentTarget = null;
            }

            currentDialogueData = null;
            currentNode = null;
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

        /// <summary>อ่านทิศทางเลื่อนไฮไลต์คำตอบ: 1 = ขึ้น (W), -1 = ลง (S), 0 = ไม่ได้กด</summary>
        private int ReadNavigateDirection()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.wasPressedThisFrame) return 1;
                if (Keyboard.current.sKey.wasPressedThisFrame) return -1;
            }

            if (navigateAction?.action != null && navigateAction.action.enabled && navigateAction.action.WasPerformedThisFrame())
            {
                float y = navigateAction.action.ReadValue<Vector2>().y;
                if (y > 0.5f) return 1;
                if (y < -0.5f) return -1;
            }

            return 0;
        }

        private bool WasSubmitPressed()
        {
            if (submitAction?.action != null && submitAction.action.enabled && submitAction.action.WasPressedThisFrame())
            {
                return true;
            }

            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        }
    }
}
