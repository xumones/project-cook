using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectCook.Dialogue.UI
{
    /// <summary>สคริปต์ควบคุมหน้าต่าง Canvas UI สำหรับแสดงผลบทสนทนาและปุ่มตัวเลือกคำตอบแบบ Dynamic</summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject dialoguePanel;

        [Header("Text Components")]
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;

        [Header("Avatar & Visuals")]
        [SerializeField] private Image speakerAvatarImage;
        [SerializeField] private GameObject continuePrompt;

        [Header("Choice Components")]
        [SerializeField] private Transform choiceButtonContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("Choice Highlight")]
        [Tooltip("สีข้อความตัวเลือกปกติที่ยังไม่ถูกไฮไลต์")]
        [SerializeField] private Color normalChoiceColor = Color.black;
        [Tooltip("สีข้อความตัวเลือกที่กำลังไฮไลต์อยู่ (เลื่อนด้วย W/S)")]
        [SerializeField] private Color highlightedChoiceColor = Color.yellow;
        [Tooltip("ข้อความนำหน้าตัวเลือกที่กำลังไฮไลต์อยู่")]
        [SerializeField] private string highlightedPrefix = "> ";

        // Choice Button Pool สำหรับบริหารจัดการปุ่มตัวเลือกโดยไม่ต้อง Instantiates ใหม่ทุกครั้ง (Zero GC Leak)
        private List<GameObject> buttonPool = new List<GameObject>();

        // ข้อความดิบของแต่ละตัวเลือก (ไม่รวม prefix ไฮไลต์) เก็บไว้ใช้สร้างข้อความใหม่ตอนสลับไฮไลต์
        private List<string> choiceBaseTexts = new List<string>();

        private void Awake()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
            HideChoices();
        }

        /// <summary>แสดงหน้าต่างบทสนทนา</summary>
        public void ShowUI()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }
        }

        /// <summary>ซ่อนหน้าต่างบทสนทนา</summary>
        public void HideUI()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
            HideChoices();
        }

        /// <summary>กำหนดข้อมูลผู้พูดและภาพตัวละคร</summary>
        public void SetSpeakerInfo(string speakerName, Sprite avatarSprite)
        {
            if (speakerNameText != null)
            {
                speakerNameText.text = speakerName;
            }

            if (speakerAvatarImage != null)
            {
                if (avatarSprite != null)
                {
                    speakerAvatarImage.sprite = avatarSprite;
                    speakerAvatarImage.gameObject.SetActive(true);
                }
                else
                {
                    speakerAvatarImage.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>กำหนดข้อความบทสนทนาและแสดงผลเต็มบรรทัดทันที</summary>
        public void SetDialogueText(string text)
        {
            if (dialogueText != null)
            {
                dialogueText.text = text;
                dialogueText.maxVisibleCharacters = int.MaxValue;
            }
        }

        /// <summary>เตรียมข้อความสำหรับเอฟเฟกต์ Typewriter โดยใส่ข้อความเต็มไว้ก่อนแล้วซ่อนตัวอักษรทั้งหมด</summary>
        public int PrepareTypewriterText(string text)
        {
            if (dialogueText == null) return 0;

            dialogueText.text = text;
            dialogueText.maxVisibleCharacters = 0;

            // บังคับให้ TMP ประมวลผลข้อความทันที เพื่ออ่านจำนวนตัวอักษรจริงที่ไม่รวมแท็ก Rich Text
            dialogueText.ForceMeshUpdate();

            return dialogueText.textInfo.characterCount;
        }

        /// <summary>กำหนดจำนวนตัวอักษรที่เปิดเผยให้เห็นสำหรับเอฟเฟกต์ Typewriter</summary>
        public void SetVisibleCharacterCount(int count)
        {
            if (dialogueText != null)
            {
                dialogueText.maxVisibleCharacters = count;
            }
        }

        /// <summary>แสดงผลปุ่มตัวเลือกคำตอบแบบ Dynamic (เลือกด้วย W/S/E ผ่าน DialogueManager ไม่ใช้เมาส์คลิกแล้ว)</summary>
        public void DisplayChoices(List<DialogueChoice> choices)
        {
            HideChoices();

            if (choices == null || choices.Count == 0 || choiceButtonContainer == null || choiceButtonPrefab == null)
            {
                return;
            }

            if (continuePrompt != null)
            {
                continuePrompt.SetActive(false);
            }

            for (int i = 0; i < choices.Count; i++)
            {
                DialogueChoice choice = choices[i];
                string baseText = $"{i + 1}. {choice.choiceText}";
                choiceBaseTexts.Add(baseText);

                GameObject btnObj = GetChoiceButtonFromPool(i);
                btnObj.SetActive(true);

                TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = baseText;
                    btnText.color = normalChoiceColor;
                }
            }
        }

        /// <summary>ไฮไลต์ตัวเลือกคำตอบที่ index ที่ระบุ (ส่ง -1 เพื่อยกเลิกไฮไลต์ทั้งหมด)</summary>
        public void SetHighlightedIndex(int highlightedIndex)
        {
            for (int i = 0; i < choiceBaseTexts.Count && i < buttonPool.Count; i++)
            {
                GameObject btnObj = buttonPool[i];
                if (btnObj == null) continue;

                TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText == null) continue;

                bool isHighlighted = i == highlightedIndex;
                btnText.text = isHighlighted ? highlightedPrefix + choiceBaseTexts[i] : choiceBaseTexts[i];
                btnText.color = isHighlighted ? highlightedChoiceColor : normalChoiceColor;
            }
        }

        /// <summary>ซ่อนปุ่มตัวเลือกคำตอบทั้งหมด</summary>
        public void HideChoices()
        {
            foreach (var btnObj in buttonPool)
            {
                if (btnObj != null)
                {
                    btnObj.SetActive(false);
                }
            }
            choiceBaseTexts.Clear();

            if (continuePrompt != null)
            {
                continuePrompt.SetActive(true);
            }
        }

        /// <summary>ดึงปุ่มจาก Pool หรือสร้างใหม่กรณีไม่พอ</summary>
        private GameObject GetChoiceButtonFromPool(int index)
        {
            if (index < buttonPool.Count)
            {
                return buttonPool[index];
            }

            GameObject newBtn = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            buttonPool.Add(newBtn);
            return newBtn;
        }
    }
}
