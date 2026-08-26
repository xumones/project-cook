using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>
    /// Base Class สำหรับ Manager ที่ผูกอยู่กับ Scene (Scene-Scoped Singleton)
    ///
    /// ใช้กับ Manager ที่ "ถือ Reference ไปยัง Object ใน Scene" เช่น กล้องประจำฉาก หรือหน้าต่าง UI
    /// Manager ประเภทนี้จะถูกทำลายไปพร้อมกับ Scene และ Scene ใหม่จะมี Instance ของตัวเองแยกกัน
    ///
    /// ข้อสำคัญ: จะไม่สร้างตัวเองอัตโนมัติ เพราะ Reference ที่ตั้งค่าไว้ใน Inspector เติมให้เองไม่ได้
    /// หากลืมวางไว้ใน Scene ค่า Instance จะเป็น null ผู้เรียกจึงควรเช็ค null ก่อนใช้งานเสมอ
    /// </summary>
    public abstract class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>
    {
        private static T instance;

        /// <summary>
        /// Instance ประจำ Scene ปัจจุบัน (เป็น null หากไม่ได้วาง Manager ตัวนี้ไว้ใน Scene)
        /// </summary>
        public static T Instance => instance;

        /// <summary>
        /// ตรวจสอบว่ามี Instance อยู่ใน Scene หรือไม่
        /// </summary>
        public static bool HasInstance => instance != null;

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"[{typeof(T).Name}] พบ Instance ซ้ำกันใน Scene กำลังทำลายตัวที่เกินมาออก", this);
                Destroy(gameObject);
                return;
            }

            instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
