using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>สวิตช์สำหรับทดสอบตอนพัฒนา มีผลเฉพาะใน Editor และ development build เกมจริงปิดหมดเสมอ</summary>
    [CreateAssetMenu(fileName = "DevSettings", menuName = "ProjectCook/Core/Dev Settings")]
    public class DevSettingsSO : ScriptableObject
    {
        [Tooltip("เปิดพื้นที่ที่ติ๊ก Dev Only (ประตูไปพื้นที่ทดสอบจะโผล่)")]
        [SerializeField] private bool devLocationsEnabled = true;

        public bool DevLocationsEnabled => devLocationsEnabled && Debug.isDebugBuild;
    }
}
