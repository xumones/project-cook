using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>แหล่งความจริงเดียวว่าเกมอยู่โหมดไหน โหมดซ้อนกันได้: ระบบที่เข้าทีหลังอยู่ชั้นบน ออกแล้วกลับไปโหมดชั้นล่างเอง</summary>
    public class GameModeManager : PersistentSingleton<GameModeManager>
    {
        private struct Layer
        {
            public UnityEngine.Object Owner;
            public GameMode Mode;
        }

        private readonly List<Layer> layers = new List<Layer>();
        private int lastChangeFrame = -1;

        /// <summary>โหมดชั้นบนสุด ไม่มีชั้นไหนเลย = เดินสำรวจ</summary>
        public GameMode CurrentMode => layers.Count > 0 ? layers[layers.Count - 1].Mode : GameMode.Exploring;

        /// <summary>มีการเข้าหรือออกโหมดชั้นใดก็ได้ ส่งโหมดบนสุดมาให้</summary>
        public event Action<GameMode> ModeChanged;

        /// <summary>อินพุตตอนเดินสำรวจเท่านั้น (หยิบ interact) ปิดเมื่อมีโหมดอื่นอยู่บนสุด</summary>
        public static bool GameplayInputAllowed => InputAllowedIn(GameMode.Exploring);

        /// <summary>อินพุตที่ใช้ได้ทั้งตอนเดินและในสถานี (บิดลูกบิด เปิดกระเป๋า) ปิดตอนคุยหรือเปิดหน้าต่างอยู่</summary>
        public static bool WorldInputAllowed => Instance == null || Instance.AcceptsInput(GameMode.Exploring, GameMode.Station);

        /// <summary>โหมดนี้อยู่บนสุดและรับอินพุตได้ในเฟรมนี้ เช่น ปุ่มของสถานีต้องหยุดตอนเปิดกระเป๋าทับ</summary>
        public static bool InputAllowedIn(GameMode mode) => Instance == null || Instance.AcceptsInput(mode, mode);

        // เฟรมที่เพิ่งเปลี่ยนโหมดยังนับปุ่มที่กดเปลี่ยนโหมดอยู่ จึงต้องกันไว้หนึ่งเฟรม
        private bool AcceptsInput(GameMode a, GameMode b) =>
            (CurrentMode == a || CurrentMode == b) && Time.frameCount != lastChangeFrame;

        /// <summary>มีโหมดนี้อยู่ชั้นใดชั้นหนึ่งไหม เช่น เปิดกระเป๋าทับสถานี สถานียังนับว่าอยู่</summary>
        public bool Contains(GameMode mode)
        {
            foreach (Layer layer in layers)
            {
                if (layer.Mode == mode) return true;
            }
            return false;
        }

        /// <summary>เข้าโหมดในนามของ owner เป็นชั้นบนสุด ถ้า owner มีชั้นอยู่แล้วจะถูกแทนที่</summary>
        public void Enter(UnityEngine.Object owner, GameMode mode)
        {
            RemoveLayerOf(owner);
            layers.Add(new Layer { Owner = owner, Mode = mode });
            NotifyChanged();
        }

        /// <summary>ออกจากโหมดของ owner ไม่ว่าจะอยู่ชั้นไหน</summary>
        public void Exit(UnityEngine.Object owner)
        {
            if (RemoveLayerOf(owner)) NotifyChanged();
        }

        private bool RemoveLayerOf(UnityEngine.Object owner)
        {
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (layers[i].Owner != owner) continue;

                layers.RemoveAt(i);
                return true;
            }
            return false;
        }

        private void NotifyChanged()
        {
            lastChangeFrame = Time.frameCount;
            ModeChanged?.Invoke(CurrentMode);
        }
    }
}
