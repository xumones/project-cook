using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>Manager ที่มีตัวเดียวและวางไว้ในฉาก อยู่ใน Core = อยู่ตลอดเกม อยู่ในฉากพื้นที่ = อยู่เท่าพื้นที่นั้น</summary>
    public abstract class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>
    {
        private static T instance;

        /// <summary>Instance ประจำ Scene ปัจจุบัน (เป็น null หากไม่ได้วาง Manager ตัวนี้ไว้ใน Scene)</summary>
        public static T Instance => instance;

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
