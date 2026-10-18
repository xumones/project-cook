using UnityEngine.SceneManagement;

namespace ProjectCook.Items
{
    /// <summary>ไอเทมที่ "มีของอยู่ข้างในได้"</summary>
    public interface IItemContents
    {
        /// <summary>ตอนนี้มีของอยู่ข้างในหรือไม่</summary>
        bool HasContents { get; }

        /// <summary>ยึดของข้างในให้ติดไปกับภาชนะ หรือคืนอิสระให้มัน</summary>
        void SetFrozen(bool frozen);

        /// <summary>ย้ายของข้างในไปฉากเดียวกับภาชนะ ของในจานจะได้ไม่ถูกทิ้งไว้ตอนถือจานข้ามพื้นที่</summary>
        void MoveToScene(Scene scene);

        /// <summary>เอาของข้างในออกจากฉากทั้งหมด ใช้ตอนภาชนะออกจากฉากไปด้วย</summary>
        void DestroyContents();
    }
}
