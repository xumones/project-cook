using System.Collections;
using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>จอดำทับทุกอย่าง ใช้บังตอนสลับพื้นที่</summary>
    // ในฉากตั้ง alpha = 1 ไว้ เฟรมแรกของ Core จึงดำต่อจากจอดำของเมนูโดยไม่เห็นรอยต่อ
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        [Tooltip("วินาทีจากใสจนดำสนิท (หรือกลับกัน)")]
        [Min(0.01f)]
        [SerializeField] private float duration = 2f;

        private CanvasGroup group;

        private void Awake() => group = GetComponent<CanvasGroup>();

        public IEnumerator FadeOut() => FadeTo(1f);

        public IEnumerator FadeIn() => FadeTo(0f);

        private IEnumerator FadeTo(float target)
        {
            group.blocksRaycasts = true;
            while (!Mathf.Approximately(group.alpha, target))
            {
                group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / duration);
                yield return null;
            }
            group.blocksRaycasts = target > 0f;
        }
    }
}
