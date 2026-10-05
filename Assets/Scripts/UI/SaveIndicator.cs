using TMPro;
using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.UI
{
    /// <summary>ป้ายมุมจอบอกว่าเปิด save อยู่ เห็นเฉพาะตอนพัฒนา จะได้ไม่ลืมว่ากด Play รอบหน้าจะโหลดของเดิม</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class SaveIndicator : MonoBehaviour
    {
        private TMP_Text text;

        // แคชไว้ตอน subscribe เพราะตอนหยุดเกม Instance อาจคืน null ก่อน OnDestroy
        private SaveService save;

        private void Start()
        {
            text = GetComponent<TMP_Text>();
            save = SaveService.Instance;
            if (save == null || !Debug.isDebugBuild)
            {
                text.enabled = false;
                return;
            }

            save.EnabledChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (save != null) save.EnabledChanged -= Refresh;
        }

        private void Refresh() => text.enabled = save.IsEnabled;
    }
}
