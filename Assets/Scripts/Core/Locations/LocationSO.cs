using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>พื้นที่หนึ่งที่ผู้เล่นเดินทางไปได้ (บ้าน พื้นที่ทดสอบ แมพล่าในอนาคต) หนึ่งพื้นที่คือหนึ่งฉาก</summary>
    [CreateAssetMenu(fileName = "Location_", menuName = "ProjectCook/Core/Location")]
    public class LocationSO : ScriptableObject
    {
        [Tooltip("ชื่อฉากของพื้นที่นี้ ต้องอยู่ใน Build Settings")]
        [SerializeField] private string sceneName;

        [Tooltip("ออกไปแล้วซ่อนฉากไว้แทนการปลด ของในพื้นที่จึงอยู่ครบตอนกลับมา (เช่น บ้าน)")]
        [SerializeField] private bool keepLoaded;

        [Tooltip("พื้นที่สำหรับทดสอบ เข้าได้เฉพาะตอนเปิด Dev Locations ใน DevSettings")]
        [SerializeField] private bool devOnly;

        [Tooltip("พื้นที่ข้างนอกบ้าน ของที่หยิบได้ที่นี่ถูกจดว่าได้มาจากข้างนอก")]
        [SerializeField] private bool isOutside;

        public string SceneName => sceneName;
        public bool KeepLoaded => keepLoaded;
        public bool DevOnly => devOnly;
        public bool IsOutside => isOutside;
    }
}
