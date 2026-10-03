using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectCook.Core
{
    /// <summary>Singleton ที่เป็นเจ้าของสถานะเคอร์เซอร์เมาส์เพียงจุดเดียวของทั้งเกม (Single Source of Truth)</summary>
    [DisallowMultipleComponent]
    public class CursorManager : PersistentSingleton<CursorManager>
    {
        // เก็บรายชื่อระบบที่กำลังขอให้ปลดล็อกเคอร์เซอร์อยู่
        // ใช้ HashSet แทนตัวนับเลข เพื่อกันบั๊กนับเกิน/นับขาดกรณีระบบเดิมเรียกซ้ำหรือลืมคืนสิทธิ์
        private readonly HashSet<Object> unlockRequesters = new HashSet<Object>();

        // ระบบที่ขอซ่อนเคอร์เซอร์ชั่วคราว (เช่น คลิกขวาลากหมุนกล้อง) มีผลเหนือคำขอปลดล็อก
        private readonly HashSet<Object> hideRequesters = new HashSet<Object>();

        // ตำแหน่งเคอร์เซอร์ก่อนถูกซ่อน เพื่อคืนกลับที่เดิมตอนโผล่
        private Vector2 positionBeforeHide;
        private bool restorePositionOnShow;
        private bool wasVisible;

        // สร้างตั้งแต่เริ่มเกม เคอร์เซอร์จะได้ล็อกกลางจอทันทีโดยไม่ต้องรอให้ระบบอื่นเรียกใช้ก่อน
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOnStart() => _ = Instance;

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

        /// <summary>ขอซ่อนและล็อกเคอร์เซอร์ชั่วคราว พอเลิกขอจะโผล่กลับที่ตำแหน่งเดิม</summary>
        public static void RequestHide(Object requester)
        {
            if (requester == null) return;

            CursorManager manager = Instance;
            if (manager == null) return;

            if (manager.hideRequesters.Add(requester))
            {
                manager.ApplyState();
            }
        }

        /// <summary>คืนสิทธิ์การซ่อนเคอร์เซอร์</summary>
        public static void ReleaseHide(Object requester)
        {
            if (requester == null) return;

            CursorManager manager = Instance;
            if (manager == null) return;

            if (manager.hideRequesters.Remove(requester))
            {
                manager.ApplyState();
            }
        }

        /// <summary>ตำแหน่งตัวชี้บนจอตอนเคอร์เซอร์ปลดล็อก (ระหว่างซ่อนชั่วคราวคืนจุดก่อนซ่อน) คืน false ตอนล็อกกลางจอ</summary>
        public static bool TryGetPointerPosition(out Vector2 position)
        {
            position = default;
            if (!HasInstance) return false;

            CursorManager manager = Instance;
            if (manager.unlockRequesters.Count == 0) return false;

            if (manager.hideRequesters.Count > 0)
            {
                position = manager.positionBeforeHide;
                return manager.restorePositionOnShow;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null) return false;

            position = mouse.position.ReadValue();
            return true;
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
            hideRequesters.RemoveWhere(requester => requester == null);

            bool visible = unlockRequesters.Count > 0 && hideRequesters.Count == 0;
            Mouse mouse = Mouse.current;

            if (wasVisible && !visible && hideRequesters.Count > 0 && mouse != null)
            {
                positionBeforeHide = mouse.position.ReadValue();
                restorePositionOnShow = true;
            }

            // ไม่มีใครขอปลดล็อกแล้ว (เช่น ออกจากสถานีระหว่างซ่อน) ตำแหน่งที่จำไว้ก็หมดความหมาย
            if (unlockRequesters.Count == 0) restorePositionOnShow = false;

            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = visible;

            if (!wasVisible && visible && restorePositionOnShow && mouse != null)
            {
                mouse.WarpCursorPosition(positionBeforeHide);
                restorePositionOnShow = false;
            }

            wasVisible = visible;
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
