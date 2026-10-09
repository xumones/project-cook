using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectCook.Core;

namespace ProjectCook.UI
{
    /// <summary>หน้าเริ่มเกม: Play ค่อยๆ มืดแล้วเข้าเกม Exit ปิดเกม</summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button exitButton;

        [Tooltip("ฉากดำทับทั้งจอ alpha เริ่มที่ 0")]
        [SerializeField] private CanvasGroup fade;

        [Min(0f)]
        [SerializeField] private float fadeDuration = 0.5f;

        private void Start()
        {
            // ฉากนี้ไม่มี CursorManager (อยู่ใน Core) และอาจกลับมาจากในเกมที่ล็อกเมาส์ไว้
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            playButton.onClick.AddListener(Play);
            exitButton.onClick.AddListener(SceneFlow.QuitGame);
        }

        private void Play() => StartCoroutine(FadeAndStart());

        private IEnumerator FadeAndStart()
        {
            // กันกดซ้ำแล้วโหลดเกมสองรอบ
            playButton.interactable = false;
            exitButton.interactable = false;
            fade.blocksRaycasts = true;

            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                fade.alpha = t / fadeDuration;
                yield return null;
            }
            fade.alpha = 1f;

            yield return SceneFlow.StartGame();
        }
    }
}
