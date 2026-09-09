using System;
using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>แหล่งความจริงเดียวว่าเกมอยู่โหมดไหน</summary>
    public class GameModeManager : PersistentSingleton<GameModeManager>
    {
        private int lastChangeFrame = -1;

        public GameMode CurrentMode { get; private set; } = GameMode.Exploring;

        public event Action<GameMode> ModeChanged;

        /// <summary>เฟรมที่เพิ่งออกจากโหมดอื่นยังนับปุ่มที่กดออกมาค้างอยู่ จึงต้องกันไว้หนึ่งเฟรม</summary>
        public bool AcceptsGameplayInput =>
            CurrentMode == GameMode.Exploring && Time.frameCount != lastChangeFrame;

        /// <summary>เช็คแบบกัน null สำหรับระบบที่ทำงานได้แม้ไม่มี Manager ใน Scene</summary>
        public static bool GameplayInputAllowed => Instance == null || Instance.AcceptsGameplayInput;

        public void SetMode(GameMode mode)
        {
            if (CurrentMode == mode) return;

            CurrentMode = mode;
            lastChangeFrame = Time.frameCount;
            ModeChanged?.Invoke(mode);
        }
    }
}
