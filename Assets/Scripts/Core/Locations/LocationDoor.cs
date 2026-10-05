using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>ประตูไปพื้นที่อื่น กดใช้แล้วเดินทาง ถ้าพื้นที่ปลายทางเข้าไม่ได้ (เช่น ปิดพื้นที่ทดสอบ) ประตูจะซ่อนตัวเอง</summary>
    public class LocationDoor : MonoBehaviour, IInteractable, IAimHintProvider
    {
        [SerializeField] private LocationSO destination;

        [Tooltip("รหัสจุดเกิดที่ปลายทาง ว่าง = จุดเกิดหลัก")]
        [SerializeField] private string spawnId;

        private void Start()
        {
            if (LocationLoader.Instance == null || !LocationLoader.Instance.CanTravelTo(destination))
            {
                gameObject.SetActive(false);
            }
        }

        public AimHint GetAimHint() => AimHint.Use;

        public void Interact() => LocationLoader.Instance.TravelTo(destination, spawnId);
    }
}
