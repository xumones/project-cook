using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectCook.Core
{
    /// <summary>จุดเดียวที่เปิด/ปิดชุดปุ่ม (action map) ตามโหมดบนสุดของเกม ระบบอื่นแค่อ่านปุ่ม ห้าม Enable/Disable เอง</summary>
    // ปุ่มที่ถูกปิดอ่านได้ว่า "ไม่ได้กด" ผ่าน InputActionReferenceExtensions ระบบอื่นจึงไม่ต้องเช็คโหมดก่อนอ่านปุ่ม
    public class InputContextSwitcher : MonoBehaviour
    {
        [Serializable]
        private struct ModeMaps
        {
            public GameMode mode;

            [Tooltip("ชื่อ action map ที่เปิดตอนโหมดนี้อยู่บนสุด")]
            public string[] maps;
        }

        [SerializeField] private ModeMaps[] modeMaps;

        [Tooltip("ชุดปุ่มที่เปิดตลอดไม่ว่าโหมดไหน")]
        [SerializeField] private string[] alwaysOn = { "Dev" };

        // แคชไว้ตอน subscribe เพราะตอนหยุดเกม Instance อาจคืน null ก่อน OnDestroy
        private GameModeManager gameMode;

        private void Start()
        {
            WarnUnknownMaps();

            gameMode = GameModeManager.Instance;
            gameMode.ModeChanged += Apply;
            Apply(gameMode.CurrentMode);
        }

        private void OnDestroy()
        {
            if (gameMode != null) gameMode.ModeChanged -= Apply;
        }

        private void Apply(GameMode mode)
        {
            foreach (InputActionMap map in InputSystem.actions.actionMaps)
            {
                bool wanted = Array.IndexOf(alwaysOn, map.name) >= 0 || Array.IndexOf(MapsFor(mode), map.name) >= 0;
                if (wanted && !map.enabled) map.Enable();
                else if (!wanted && map.enabled) map.Disable();
            }
        }

        private string[] MapsFor(GameMode mode)
        {
            foreach (ModeMaps entry in modeMaps)
            {
                if (entry.mode == mode) return entry.maps;
            }
            return Array.Empty<string>();
        }

        // ชื่อ map ผิดจะทำให้ปุ่มทั้งชุดเงียบโดยไม่มี error จึงเตือนตั้งแต่เริ่ม
        private void WarnUnknownMaps()
        {
            foreach (ModeMaps entry in modeMaps)
            {
                foreach (string name in entry.maps)
                {
                    if (InputSystem.actions.FindActionMap(name) == null)
                    {
                        Debug.LogError($"[InputContextSwitcher] ไม่พบ action map '{name}' ของโหมด {entry.mode}", this);
                    }
                }
            }
        }
    }
}
