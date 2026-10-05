using System;

namespace ProjectCook.Core
{
    /// <summary>ช่องข่าวกลางแบบมีชนิด ผู้ส่งกับผู้ฟังไม่ต้องรู้จักกัน ใช้บอกว่า "เกิดอะไรขึ้น" ส่วนการสั่งให้ทำให้เรียกเมธอดตรงๆ</summary>
    // ผู้ฟังต้องถอนตัวเองตอน OnDisable/OnDestroy เสมอ ไม่งั้นจะค้างอยู่ในช่องข่าวหลังวัตถุถูกทำลาย
    public static class MessageBus<T> where T : struct
    {
        private static event Action<T> Published;

        public static void Subscribe(Action<T> handler) => Published += handler;

        public static void Unsubscribe(Action<T> handler) => Published -= handler;

        public static void Publish(T message) => Published?.Invoke(message);
    }
}
