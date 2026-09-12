// ============================================================================
//  PLAYTEST ONLY — เครื่องมือทดสอบ ไม่ใช่ส่วนหนึ่งของเกม
//  ลบทิ้งได้เมื่อไม่ต้องการ: ลบโฟลเดอร์ Assets/Scripts/Playtest ทั้งโฟลเดอร์
//  และ GameObject "[Playtest]" ในซีน ไม่มีโค้ดส่วนอื่นอ้างถึงที่นี่
// ============================================================================
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ProjectCook.Core;

namespace ProjectCook.Playtest
{
    /// <summary>กด / เพื่อรีเซ็ตทั้งเกม: ล้างความคืบหน้าทั้งหมด คืนโหมด Exploring แล้วโหลดซีนใหม่</summary>
    public class PlaytestReset : MonoBehaviour
    {
        // ตัว component ยังคอมไพล์ใน build จริงเพื่อไม่ให้ซีนมี Missing Script แต่ไม่มีอะไรทำงาน
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.slashKey.wasPressedThisFrame) return;

            // ตัวจัดการสองตัวนี้อยู่ข้ามซีน โหลดซีนใหม่ไม่ล้างให้ ต้องสั่งเอง
            GameFlagsManager.Instance?.ClearAll();
            GameModeManager.Instance?.SetMode(GameMode.Exploring);

            Debug.Log("[PlaytestReset] รีเซ็ตเกมทั้งหมด");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
#endif
    }
}
