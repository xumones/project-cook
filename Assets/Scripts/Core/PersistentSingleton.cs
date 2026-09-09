using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>Base Class สำหรับ Manager ที่ต้องคงอยู่ข้าม Scene (Persistent / Cross-Scene Singleton)</summary>
    public abstract class PersistentSingleton<T> : MonoBehaviour where T : PersistentSingleton<T>
    {
        private static T instance;
        private static bool isQuitting = false;

        /// <summary>ดึง Instance โดยค้นหาใน Scene ก่อน หากไม่พบจะสร้าง GameObject ขึ้นมาให้อัตโนมัติ</summary>
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

        /// <summary>มี Instance ที่เรียกใช้ได้จริงอยู่หรือไม่ โดยไม่สร้างขึ้นมาใหม่</summary>
        /// <remarks>ต้องเช็ค isQuitting ด้วย ไม่งั้นตอนปิดเกมจะคืน true ทั้งที่ Instance คืน null ไปแล้ว</remarks>
        public static bool HasInstance => !isQuitting && instance != null;

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
