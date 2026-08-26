using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Core
{
    public enum FlagComparison
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual
    }

    /// <summary>
    /// Singleton Manager สำหรับจัดเก็บข้อมูลสถานะความคืบหน้าของเกม (Game Flags & Progress)
    /// </summary>
    public class GameStateManager : PersistentSingleton<GameStateManager>
    {
        // Dictionary เก็บค่า Flag ของเกม ( Key = string, Value = int )
        private Dictionary<string, int> flags = new Dictionary<string, int>();

        /// <summary>
        /// ตั้งค่าหรืออัปเดตค่า Flag
        /// </summary>
        public void SetFlag(string key, int value)
        {
            if (string.IsNullOrEmpty(key)) return;
            flags[key] = value;
        }

        /// <summary>
        /// ดึงค่า Flag (ถ้าไม่มีจะคืนค่า default 0)
        /// </summary>
        public int GetFlag(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;
            return flags.TryGetValue(key, out int value) ? value : 0;
        }

        /// <summary>
        /// เพิ่มค่าสะสมให้ Flag
        /// </summary>
        public void IncrementFlag(string key, int amount = 1)
        {
            int current = GetFlag(key);
            SetFlag(key, current + amount);
        }

        /// <summary>
        /// บันทึกว่าทำอาหารเมนูนั้นสำเร็จแล้ว (Helper method)
        /// </summary>
        public void RecordCookedDish(string dishID)
        {
            if (string.IsNullOrEmpty(dishID)) return;
            SetFlag($"cooked_{dishID.ToLower()}", 1);
        }

        /// <summary>
        /// ตรวจสอบว่าเคยทำอาหารเมนูนั้นหรือยัง
        /// </summary>
        public bool HasCookedDish(string dishID)
        {
            if (string.IsNullOrEmpty(dishID)) return false;
            return GetFlag($"cooked_{dishID.ToLower()}") > 0;
        }

        /// <summary>
        /// ตรวจสอบเปรียบเทียบค่า Flag ตามเงื่อนไขที่กำหนด
        /// </summary>
        public bool CheckFlag(string key, FlagComparison comparison, int targetValue)
        {
            int currentValue = GetFlag(key);

            switch (comparison)
            {
                case FlagComparison.Equal:
                    return currentValue == targetValue;
                case FlagComparison.NotEqual:
                    return currentValue != targetValue;
                case FlagComparison.GreaterThan:
                    return currentValue > targetValue;
                case FlagComparison.GreaterThanOrEqual:
                    return currentValue >= targetValue;
                case FlagComparison.LessThan:
                    return currentValue < targetValue;
                case FlagComparison.LessThanOrEqual:
                    return currentValue <= targetValue;
                default:
                    return false;
            }
        }
    }
}
