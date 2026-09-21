using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using ProjectCook.Core;
using ProjectCook.Dialogue.UI;

namespace ProjectCook.Dialogue
{
    /// <summary>วงจรการคุย: ทักทาย → เมนูตัวเลือก → NPC ตอบ → กลับเมนู จนกว่าจะเลือกตัวเลือกที่จบการคุย</summary>
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
        private WaitForSeconds typingDelay;

        private void OnEnable()
        {
            advanceAction.EnableAction();
            navigateAction.EnableAction();
            submitAction.EnableAction();
        }

        private void OnDisable()
        {
            advanceAction.DisableAction();
            navigateAction.DisableAction();
            submitAction.DisableAction();
        }

        private void Update()
        {
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

        public void Begin(IDialogueSpeaker newSpeaker)
        {
            if (newSpeaker == null || phase != Phase.Idle) return;

            speaker = newSpeaker;
            GameModeManager.Instance?.SetMode(GameMode.Dialogue);

            dialogueUI.ShowUI();
            dialogueUI.SetFont(speaker.Font);
            dialogueUI.SetSpeakerInfo(Resolve(speaker.DisplayName), speaker.Portrait);

            if (speaker.TryGetGreeting(out LocalizedString greeting))
            {
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

            phase = Phase.Idle;
            options.Clear();
            optionTexts.Clear();
            speaker = null;

            dialogueUI.HideUI();
            GameModeManager.Instance?.SetMode(GameMode.Exploring);
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
            WaitForSeconds delay = GetTypingDelay();

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
            option.OnSelected?.Invoke();

            if (option.Response != null && !option.Response.IsEmpty)
            {
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

        private static string Resolve(LocalizedString text) =>
            text != null && !text.IsEmpty ? text.GetLocalizedString() : string.Empty;

        private WaitForSeconds GetTypingDelay()
        {
            if (typingDelay == null) typingDelay = new WaitForSeconds(typingSpeed);
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
