using UnityEngine;
using UnityEngine.UI;

namespace ProjectCook.Cinematics
{
    /// <summary>ป้าย "กดค้างเพื่อข้าม" ที่แสดงระหว่างเล่น cutscene แถบเติมตามความคืบหน้าของการกดค้าง</summary>
    public class CutsceneSkipPrompt : MonoBehaviour
    {
        [Tooltip("Image แบบ Filled ที่เติมตามการกดค้าง")]
        [SerializeField] private Image fill;

        public void Show()
        {
            SetProgress(0f);
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        public void SetProgress(float progress) => fill.fillAmount = progress;
    }
}
