using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using ProjectCook.Core;
using ProjectCook.Dialogue.UI;

namespace ProjectCook.Dialogue
{
    /// <summary>วงจรการคุย: ทักทาย → เมนูตัวเลือก → NPC ตอบ → กลับเมนู จนกว่าจะเลือกตัวเลือกที่จบการคุย
    /// และแสดงบทพูดทีละบรรทัดให้ระบบอื่น (เช่น cutscene) ผ่าน PlayLine</summary>
    // บรรทัดเดี่ยวไม่อ่านปุ่มเอง ผู้เรียกเป็นเจ้าของปุ่มของโหมดตัวเองแล้วสั่ง AdvanceLine
    public class DialogueRunner : SceneSingleton<DialogueRunner>
    {
        private enum Phase { Idle, Typing, Menu, AwaitClose }

        [Header("Settings")]
        [SerializeField] private float typingSpeed = 0.03f;

        [Tooltip("ตัวเลือกปิดการคุยที่ต่อท้ายเมนูทุกครั้ง")]
        [SerializeField] private LocalizedString farewellLabel;

        [Header("Input")]
        [SerializeField] private InputActionReference advanceAction;
        [SerializeField] private InputActionReference navigateAction;
        [SerializeField] private InputActionReference submitAction;

        [Header("UI")]
        [SerializeField] private DialogueUI dialogueUI;

        private IDialogueSpeaker speaker;
        private Phase phase = Phase.Idle;

        private readonly List<DialogueOption> options = new List<DialogueOption>();
        private readonly List<string> optionTexts = new List<string>();
        private int selectedIndex = -1;

        private Coroutine typingCoroutine;
        private string currentText;
        private bool endAfterLine;
        private WaitForSecondsRealtime typingDelay;

        // โหมดบรรทัดเดี่ยว: มีค่า = มีบรรทัดค้างอยู่ รอผู้เรียกสั่งไปต่อ
        private bool lineMode;
        private Action lineAdvanced;

        private void Update()
        {
            if (lineMode) return;

            switch (phase)
            {
                case Phase.Typing:
                    if (advanceAction.WasPressedThisFrame()) SkipTyping();
                    break;
                case Phase.Menu:
                    HandleMenuInput();
                    break;
                case Phase.AwaitClose:
                    if (advanceAction.WasPressedThisFrame()) End();
                    break;
            }
        }

        public void Begin(IDialogueSpeaker newSpeaker) => Begin(newSpeaker, null);

        /// <summary>เริ่มคุยโดยผู้พูดเปิดด้วยประโยคนี้แทนคำทักทาย (เช่น ตอบของที่เพิ่งได้รับ) แล้วเข้าเมนูตามปกติ null = ทักทายตามปกติ</summary>
        public void Begin(IDialogueSpeaker newSpeaker, LocalizedString opening)
        {
            if (newSpeaker == null || phase != Phase.Idle || lineMode) return;

            speaker = newSpeaker;
            GameModeManager.Instance?.Enter(this, GameMode.Dialogue);
            MessageBus<DialogueStarted>.Publish(new DialogueStarted(speaker.SpeakerId));

            dialogueUI.ShowUI();
            dialogueUI.SetTextStyle(speaker.TextStyle);
            dialogueUI.SetSpeakerInfo(Resolve(speaker.DisplayName), speaker.Portrait);

            if (opening != null && !opening.IsEmpty)
            {
                PublishBeat(DialogueBeat.Response);
                Say(opening, endAfter: false);
            }
            else if (speaker.TryGetGreeting(out LocalizedString greeting))
            {
                PublishBeat(DialogueBeat.Greeting);
                Say(greeting, endAfter: false);
            }
            else
            {
                ShowMenu();
            }
        }

        private void End()
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);

            string speakerId = speaker.SpeakerId;
            phase = Phase.Idle;
            options.Clear();
            optionTexts.Clear();
            speaker = null;

            dialogueUI.HideUI();
            // ส่งก่อนออกจากโหมด ระบบที่รอให้คุยจบ (เช่น cutscene) จะเปิดคำขอทันก่อนเกมกลับไปเดินสำรวจ
            MessageBus<DialogueEnded>.Publish(new DialogueEnded(speakerId));
            GameModeManager.Instance?.Exit(this);
        }

        /// <summary>แสดงหนึ่งบรรทัดแบบพิมพ์ทีละตัว onAdvanced ถูกเรียกครั้งเดียวเมื่อผู้เล่นอ่านจบแล้วกดไปต่อ
        /// หน้าต่างยังค้างจนกว่าจะเรียก HideLine หรือบรรทัดถัดไป speaker = null คือไม่แสดงชื่อ</summary>
        public void PlayLine(ISpeakerIdentity lineSpeaker, LocalizedString line, Action onAdvanced)
        {
            if (phase != Phase.Idle && !lineMode) return;

            lineMode = true;
            lineAdvanced = onAdvanced;
            dialogueUI.ShowUI();
            dialogueUI.SetTextStyle(lineSpeaker?.TextStyle);
            dialogueUI.SetSpeakerInfo(Resolve(lineSpeaker?.DisplayName), lineSpeaker?.Portrait);
            Say(line, endAfter: true);
        }

        /// <summary>กดไปต่อของบรรทัดเดี่ยว: กำลังพิมพ์ = แสดงทั้งบรรทัด, พิมพ์จบแล้ว = แจ้งผู้เรียก</summary>
        public void AdvanceLine()
        {
            if (!lineMode) return;

            if (phase == Phase.Typing)
            {
                SkipTyping();
                return;
            }

            if (phase != Phase.AwaitClose) return;

            phase = Phase.Idle;
            Action advanced = lineAdvanced;
            lineAdvanced = null;
            advanced?.Invoke();
        }

        public void HideLine()
        {
            if (!lineMode) return;

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            phase = Phase.Idle;
            lineMode = false;
            lineAdvanced = null;
            dialogueUI.HideUI();
        }

        private void Say(LocalizedString line, bool endAfter)
        {
            endAfterLine = endAfter;
            currentText = Resolve(line);
            phase = Phase.Typing;

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeTextRoutine(currentText));
        }

        private IEnumerator TypeTextRoutine(string fullText)
        {
            int totalCharacters = dialogueUI.PrepareTypewriterText(fullText);
            WaitForSecondsRealtime delay = GetTypingDelay();

            for (int i = 1; i <= totalCharacters; i++)
            {
                dialogueUI.SetVisibleCharacterCount(i);
                yield return delay;
            }

            OnLineFinished();
        }

        private void SkipTyping()
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueUI.SetDialogueText(currentText);
            OnLineFinished();
        }

        private void OnLineFinished()
        {
            if (endAfterLine)
            {
                phase = Phase.AwaitClose;
            }
            else
            {
                ShowMenu();
            }
        }

        // รวมตัวเลือกใหม่ทุกครั้ง เพราะตัวเลือกก่อนหน้าอาจเปลี่ยนสถานะไปแล้ว (รับเควส/ให้ของ)
        private void ShowMenu()
        {
            options.Clear();
            optionTexts.Clear();
            speaker.CollectOptions(options);
            options.Add(new DialogueOption { Label = farewellLabel, EndsDialogue = true });

            for (int i = 0; i < options.Count; i++)
            {
                optionTexts.Add(Resolve(options[i].Label));
            }

            selectedIndex = -1;
            phase = Phase.Menu;
            PublishBeat(DialogueBeat.Menu);
            dialogueUI.DisplayChoices(optionTexts);
        }

        private void HandleMenuInput()
        {
            int direction = ReadNavigateDirection();
            if (direction != 0) MoveSelection(direction);

            // ต้องเลื่อนไฮไลต์ก่อนถึงยืนยันได้ กันกดปุ่มเดียวกับ advance แล้วเลือกข้อแรกโดยไม่ตั้งใจ
            if (selectedIndex >= 0 && submitAction.WasPressedThisFrame())
            {
                Select(options[selectedIndex]);
            }
        }

        private void MoveSelection(int direction)
        {
            selectedIndex = selectedIndex < 0
                ? 0
                : Mathf.Clamp(selectedIndex - direction, 0, options.Count - 1);

            dialogueUI.SetHighlightedIndex(selectedIndex);
        }

        private void Select(DialogueOption option)
        {
            dialogueUI.HideChoices();
            MessageBus<DialogueOptionSelected>.Publish(new DialogueOptionSelected(speaker.SpeakerId));
            option.OnSelected?.Invoke();

            if (option.Response != null && !option.Response.IsEmpty)
            {
                PublishBeat(DialogueBeat.Response, option.Shot);
                Say(option.Response, option.EndsDialogue);
            }
            else if (option.EndsDialogue)
            {
                End();
            }
            else
            {
                ShowMenu();
            }
        }

        private void PublishBeat(DialogueBeat beat, DialogueShot shot = DialogueShot.Player) =>
            MessageBus<DialogueBeatStarted>.Publish(new DialogueBeatStarted(speaker.SpeakerId, beat, shot));

        private static string Resolve(LocalizedString text) =>
            text != null && !text.IsEmpty ? text.GetLocalizedString() : string.Empty;

        // ใช้เวลาจริง ยังพิมพ์ได้ตอน cutscene เต็มหยุดเวลาของเกม
        private WaitForSecondsRealtime GetTypingDelay()
        {
            if (typingDelay == null) typingDelay = new WaitForSecondsRealtime(typingSpeed);
            return typingDelay;
        }

        /// <summary>1 = ขึ้น, -1 = ลง, 0 = ไม่ได้กด</summary>
        private int ReadNavigateDirection()
        {
            if (!navigateAction.WasPressedThisFrame()) return 0;

            float y = navigateAction.ReadValue<Vector2>().y;
            if (y > 0.5f) return 1;
            if (y < -0.5f) return -1;
            return 0;
        }
    }
}
