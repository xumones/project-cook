using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>Singleton ที่เป็นเจ้าของสถานะเคอร์เซอร์เมาส์เพียงจุดเดียวของทั้งเกม (Single Source of Truth)</summary>
    [DisallowMultipleComponent]
    public class CursorManager : PersistentSingleton<CursorManager>
    {
        // เก็บรายชื่อระบบที่กำลังขอให้ปลดล็อกเคอร์เซอร์อยู่
        // ใช้ HashSet แทนตัวนับเลข เพื่อกันบั๊กนับเกิน/นับขาดกรณีระบบเดิมเรียกซ้ำหรือลืมคืนสิทธิ์
        private readonly HashSet<Object> unlockRequesters = new HashSet<Object>();

        /// <summary>สถานะปัจจุบันว่าเคอร์เซอร์ถูกปลดล็อกให้ใช้งานอยู่หรือไม่</summary>
        public static bool IsUnlocked => HasInstance && Instance.unlockRequesters.Count > 0;

        /// <summary>เรียกให้แน่ใจว่า CursorManager ถูกสร้างและบังคับใช้สถานะเริ่มต้นแล้ว</summary>
        public static void EnsureInitialized()
        {
            CursorManager manager = Instance;
            if (manager != null)
            {
                manager.ApplyState();
            }
        }

        /// <summary>ขอปลดล็อกเคอร์เซอร์เพื่อใช้ตัวชี้เมาส์ (เช่น ตอนเปิดหน้าต่างบทสนทนาหรือเมนู)</summary>
        public static void RequestUnlock(Object requester)
        {
            if (requester == null) return;

            CursorManager manager = Instance;
            if (manager == null) return;

            if (manager.unlockRequesters.Add(requester))
            {
                manager.ApplyState();
            }
        }

        /// <summary>คืนสิทธิ์การปลดล็อกเคอร์เซอร์ หากไม่มีระบบใดขออยู่แล้วจะกลับไปล็อกกลางจออัตโนมัติ</summary>
        public static void ReleaseUnlock(Object requester)
        {
            if (requester == null) return;

            CursorManager manager = Instance;
            if (manager == null) return;

            if (manager.unlockRequesters.Remove(requester))
            {
                manager.ApplyState();
            }
        }

        protected override void Awake()
        {
            base.Awake();

            // หากเป็น Instance ซ้ำ base จะสั่งทำลายทิ้งไปแล้ว จึงไม่ต้องบังคับสถานะต่อ
            if (Instance != this) return;

            ApplyState();
        }

        /// <summary>บังคับใช้สถานะเคอร์เซอร์ตามจำนวนผู้ขอปลดล็อกที่เหลืออยู่จริง</summary>
        private void ApplyState()
        {
            // ล้างผู้ขอที่ถูกทำลายไปแล้วออกก่อน (กันเคอร์เซอร์ค้างปลดล็อกเมื่อ Scene เปลี่ยนหรือ Object ถูก Destroy)
            unlockRequesters.RemoveWhere(requester => requester == null);

            bool shouldUnlock = unlockRequesters.Count > 0;

            Cursor.lockState = shouldUnlock ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = shouldUnlock;
        }

        /// <summary>เมื่อผู้เล่นสลับหน้าต่างกลับเข้ามาในเกม ให้บังคับใช้สถานะเดิมอีกครั้ง</summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ApplyState();
            }
        }

    }
}
