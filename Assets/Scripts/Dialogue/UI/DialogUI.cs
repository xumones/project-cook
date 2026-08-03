using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectCook.Dialogue.UI
{
    /// <summary>
    /// สคริปต์ควบคุมหน้าต่าง Canvas UI สำหรับแสดงผลบทสนทนาและปุ่มตัวเลือกคำตอบแบบ Dynamic
    /// </summary>
    public class DialogUI : MonoBehaviour
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

        // Choice Button Pool สำหรับบริหารจัดการปุ่มตัวเลือกโดยไม่ต้อง Instantiates ใหม่ทุกครั้ง (Zero GC Leak)
        private List<GameObject> buttonPool = new List<GameObject>();

        private void Awake()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
            HideChoices();
        }

        /// <summary>
        /// แสดงหน้าต่างบทสนทนา
        /// </summary>
        public void ShowUI()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }
        }

        /// <summary>
        /// ซ่อนหน้าต่างบทสนทนา
        /// </summary>
        public void HideUI()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
            HideChoices();
        }

        /// <summary>
        /// กำหนดข้อมูลผู้พูดและภาพตัวละคร
        /// </summary>
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

        /// <summary>
        /// กำหนดข้อความบทสนทนาแบบตั้งค่าโดยตรง
        /// </summary>
        public void SetDialogueText(string text)
        {
            if (dialogueText != null)
            {
                dialogueText.text = text;
            }
        }

        /// <summary>
        /// ต่อเติมอักษรทีละตัวสำหรับ Typewriter effect
        /// </summary>
        public void AppendDialogueChar(char c)
        {
            if (dialogueText != null)
            {
                dialogueText.text += c;
            }
        }

        /// <summary>
        /// แสดงผลปุ่มตัวเลือกคำตอบแบบ Dynamic
        /// </summary>
        public void DisplayChoices(List<DialogChoice> choices, Action<DialogChoice> onChoiceSelected)
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
                DialogChoice choice = choices[i];
                GameObject btnObj = GetChoiceButtonFromPool(i);

                btnObj.SetActive(true);

                // ตั้งค่าข้อความบนปุ่ม
                TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = $"{i + 1}. {choice.choiceText}";
                }

                // ผูก Event ปุ่มกด
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        onChoiceSelected?.Invoke(choice);
                    });
                }
            }
        }

        /// <summary>
        /// ซ่อนปุ่มตัวเลือกคำตอบทั้งหมด
        /// </summary>
        public void HideChoices()
        {
            foreach (var btnObj in buttonPool)
            {
                if (btnObj != null)
                {
                    btnObj.SetActive(false);
                }
            }

            if (continuePrompt != null)
            {
                continuePrompt.SetActive(true);
            }
        }

        /// <summary>
        /// ดึงปุ่มจาก Pool หรือสร้างใหม่กรณีไม่พอ
        /// </summary>
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
