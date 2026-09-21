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

        /// <summary>อินพุตตอนเดินสำรวจเท่านั้น (หยิบ/วาง/interact) ปิดทั้งในสถานีและตอนคุย</summary>
        public static bool GameplayInputAllowed => Instance == null || Instance.AcceptsInput(GameMode.Exploring);

        /// <summary>อินพุตที่ใช้กับของในโลกได้ทั้งตอนเดินและในสถานี (เช่น คีบวัตถุดิบ) ปิดเฉพาะตอนคุย</summary>
        public static bool WorldInputAllowed => Instance == null || Instance.AcceptsInput(GameMode.Exploring, GameMode.Station);

        // เฟรมที่เพิ่งออกจากโหมดอื่นยังนับปุ่มที่กดออกมาค้างอยู่ จึงต้องกันไว้หนึ่งเฟรม
        private bool AcceptsInput(GameMode a, GameMode b = GameMode.Exploring) =>
            (CurrentMode == a || CurrentMode == b) && Time.frameCount != lastChangeFrame;

        public void SetMode(GameMode mode)
        {
            if (CurrentMode == mode) return;

            CurrentMode = mode;
            lastChangeFrame = Time.frameCount;
            ModeChanged?.Invoke(mode);
        }
    }
}
