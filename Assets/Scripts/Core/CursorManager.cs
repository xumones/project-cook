using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>
    /// Singleton ที่เป็นเจ้าของสถานะเคอร์เซอร์เมาส์เพียงจุดเดียวของทั้งเกม (Single Source of Truth)
    ///
    /// หลักการทำงาน: สถานะเริ่มต้นคือ "ล็อกกลางจอ + ซ่อนเคอร์เซอร์" สำหรับเกมมุมมอง FPS
    /// ระบบใดที่ต้องการตัวชี้เมาส์ (เช่น หน้าต่างบทสนทนา, เมนู, ร้านค้า) ให้เรียก RequestUnlock
    /// และเรียก ReleaseUnlock เมื่อเลิกใช้ เมื่อไม่มีผู้ขอเหลืออยู่แล้วจะกลับไปล็อกอัตโนมัติ
    ///
    /// เหตุผลที่ต้องรวมศูนย์: ก่อนหน้านี้มีหลายสคริปต์เขียนค่า Cursor แข่งกันเองทุกเฟรม
    /// ทำให้เกิดบั๊กเมาส์ถูกล็อกกลับระหว่างเปิดหน้าต่างบทสนทนาจนกดตัวเลือกไม่ได้
    /// </summary>
    [DisallowMultipleComponent]
    public class CursorManager : MonoBehaviour
    {
        private static CursorManager instance;
        private static bool isQuitting = false;

        // เก็บรายชื่อระบบที่กำลังขอให้ปลดล็อกเคอร์เซอร์อยู่
        // ใช้ HashSet แทนตัวนับเลข เพื่อกันบั๊กนับเกิน/นับขาดกรณีระบบเดิมเรียกซ้ำหรือลืมคืนสิทธิ์
        private readonly HashSet<Object> unlockRequesters = new HashSet<Object>();

        /// <summary>
        /// ดึง Instance โดยสร้าง GameObject ให้อัตโนมัติหากยังไม่มีใน Scene (ไม่ต้องวางเองใน Hierarchy)
        /// </summary>
        public static CursorManager Instance
        {
            get
            {
                if (isQuitting) return null;

                if (instance == null)
                {
                    instance = FindFirstObjectByType<CursorManager>();

                    if (instance == null)
                    {
                        GameObject managerObject = new GameObject("[CursorManager]");
                        instance = managerObject.AddComponent<CursorManager>();
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// สถานะปัจจุบันว่าเคอร์เซอร์ถูกปลดล็อกให้ใช้งานอยู่หรือไม่
        /// </summary>
        public static bool IsUnlocked => instance != null && instance.unlockRequesters.Count > 0;

        /// <summary>
        /// เรียกให้แน่ใจว่า CursorManager ถูกสร้างและบังคับใช้สถานะเริ่มต้นแล้ว
        /// (ใช้ตอนเกมเริ่มเพื่อล็อกเคอร์เซอร์กลางจอสำหรับมุมมอง FPS)
        /// </summary>
        public static void EnsureInitialized()
        {
            CursorManager manager = Instance;
            if (manager != null)
            {
                manager.ApplyState();
            }
        }

        /// <summary>
        /// ขอปลดล็อกเคอร์เซอร์เพื่อใช้ตัวชี้เมาส์ (เช่น ตอนเปิดหน้าต่างบทสนทนาหรือเมนู)
        /// </summary>
        /// <param name="requester">ตัวสคริปต์ที่ขอ (ส่ง this มาได้เลย) ใช้ระบุตัวตนเพื่อคืนสิทธิ์ภายหลัง</param>
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

        /// <summary>
        /// คืนสิทธิ์การปลดล็อกเคอร์เซอร์ หากไม่มีระบบใดขออยู่แล้วจะกลับไปล็อกกลางจออัตโนมัติ
        /// </summary>
        /// <param name="requester">ตัวสคริปต์ที่เคยขอไว้ (ส่ง this มาได้เลย)</param>
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

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            ApplyState();
        }

        /// <summary>
        /// บังคับใช้สถานะเคอร์เซอร์ตามจำนวนผู้ขอปลดล็อกที่เหลืออยู่จริง
        /// </summary>
        private void ApplyState()
        {
            // ล้างผู้ขอที่ถูกทำลายไปแล้วออกก่อน (กันเคอร์เซอร์ค้างปลดล็อกเมื่อ Scene เปลี่ยนหรือ Object ถูก Destroy)
            unlockRequesters.RemoveWhere(requester => requester == null);

            bool shouldUnlock = unlockRequesters.Count > 0;

            Cursor.lockState = shouldUnlock ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = shouldUnlock;
        }

        /// <summary>
        /// เมื่อผู้เล่นสลับหน้าต่างกลับเข้ามาในเกม ให้บังคับใช้สถานะเดิมอีกครั้ง
        /// (ระบบปฏิบัติการจะคืนเคอร์เซอร์ให้เสมอเมื่อสลับหน้าต่าง)
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ApplyState();
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
