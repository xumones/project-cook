using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>แหล่งความจริงเดียวว่าเกมอยู่โหมดไหน โหมดซ้อนกันได้: ระบบที่เข้าทีหลังอยู่ชั้นบน ออกแล้วกลับไปโหมดชั้นล่างเอง</summary>
    public class GameModeManager : SceneSingleton<GameModeManager>
    {
        private struct Layer
        {
            public UnityEngine.Object Owner;
            public GameMode Mode;
        }

        private readonly List<Layer> layers = new List<Layer>();

        /// <summary>โหมดชั้นบนสุด ไม่มีชั้นไหนเลย = เดินสำรวจ ชุดปุ่มที่ใช้ได้ตามโหมดนี้ (InputContextSwitcher)</summary>
        public GameMode CurrentMode => layers.Count > 0 ? layers[layers.Count - 1].Mode : GameMode.Exploring;

        /// <summary>มีการเข้าหรือออกโหมดชั้นใดก็ได้ ส่งโหมดบนสุดมาให้</summary>
        public event Action<GameMode> ModeChanged;

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

        private void NotifyChanged() => ModeChanged?.Invoke(CurrentMode);
    }
}
