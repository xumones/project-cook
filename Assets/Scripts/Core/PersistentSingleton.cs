using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>
    /// Base Class สำหรับ Manager ที่ต้องคงอยู่ข้าม Scene (Persistent / Cross-Scene Singleton)
    ///
    /// ใช้กับ Manager ที่ "ไม่มี Reference ไปยัง Object ใน Scene" เท่านั้น เช่น ระบบเก็บสถานะความคืบหน้าเกม
    /// หรือระบบควบคุมเคอร์เซอร์ เพราะ Object ของ Scene เดิมจะถูกทำลายเมื่อเปลี่ยน Scene
    /// ทำให้ Reference ที่ถือค้างไว้ใช้งานไม่ได้อีกต่อไป
    ///
    /// คุณสมบัติ: สร้างตัวเองอัตโนมัติเมื่อถูกเรียกใช้ครั้งแรก จึงไม่จำเป็นต้องวางไว้ใน Scene ล่วงหน้า
    /// หาก Manager จำเป็นต้องตั้งค่า Reference ผ่าน Inspector ให้ใช้ SceneSingleton แทน
    /// </summary>
    public abstract class PersistentSingleton<T> : MonoBehaviour where T : PersistentSingleton<T>
    {
        private static T instance;
        private static bool isQuitting = false;

        /// <summary>
        /// ดึง Instance โดยค้นหาใน Scene ก่อน หากไม่พบจะสร้าง GameObject ขึ้นมาให้อัตโนมัติ
        /// จะคืนค่า null เฉพาะตอนเกมกำลังปิดตัวลงเท่านั้น เพื่อไม่ให้สร้าง Object ค้างไว้ระหว่างปิดเกม
        /// </summary>
        public static T Instance
        {
            get
            {
                if (isQuitting) return null;

                if (instance == null)
                {
                    instance = FindFirstObjectByType<T>();

                    if (instance == null)
                    {
                        GameObject managerObject = new GameObject($"[{typeof(T).Name}]");
                        instance = managerObject.AddComponent<T>();
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// ตรวจสอบว่ามี Instance อยู่แล้วหรือไม่ โดยไม่สร้างขึ้นมาใหม่
        /// </summary>
        public static bool HasInstance => instance != null;

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = (T)this;

            // DontDestroyOnLoad ทำงานได้กับ GameObject ที่เป็น Root เท่านั้น
            // หากถูกวางเป็นลูกของ Object อื่นไว้ ให้ดึงออกมาเป็น Root ก่อนเพื่อไม่ให้ Unity เตือนและทำงานไม่สำเร็จ
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            isQuitting = true;
        }
    }
}
