using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>Singleton Manager สำหรับจัดเก็บข้อมูลสถานะความคืบหน้าของเกม (Game Flags & Progress)</summary>
    public class GameFlagsManager : PersistentSingleton<GameFlagsManager>
    {
        // Dictionary เก็บค่า Flag ของเกม ( Key = string, Value = int )
        private Dictionary<string, int> flags = new Dictionary<string, int>();

        /// <summary>ตั้งค่าหรืออัปเดตค่า Flag</summary>
        public void SetFlag(string key, int value)
        {
            if (string.IsNullOrEmpty(key)) return;
            flags[key] = value;
        }

        /// <summary>ลบ flag ทุกตัวที่ขึ้นต้นด้วย prefix ใช้ล้าง flag ชั่วคราวก่อนคำนวณใหม่</summary>
        public void ClearWithPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return;

            removeBuffer.Clear();
            foreach (string key in flags.Keys)
            {
                if (key.StartsWith(prefix)) removeBuffer.Add(key);
            }
            foreach (string key in removeBuffer)
            {
                flags.Remove(key);
            }
        }

        private readonly List<string> removeBuffer = new List<string>();

        /// <summary>ล้างความคืบหน้าทั้งหมด ใช้ตอนเริ่มเกมใหม่</summary>
        public void ClearAll()
        {
            flags.Clear();
        }

        /// <summary>ดึงค่า Flag (ถ้าไม่มีจะคืนค่า default 0)</summary>
        public int GetFlag(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;
            return flags.TryGetValue(key, out int value) ? value : 0;
        }

        /// <summary>ตรวจสอบว่าเคยทำอาหารเมนูนั้นหรือยัง</summary>
        public bool HasCookedDish(string dishID)
        {
            if (string.IsNullOrEmpty(dishID)) return false;
            return GetFlag(CookedDishKey(dishID)) > 0;
        }

        /// <summary>บันทึกว่าส่งมอบเมนูนั้นสำเร็จแล้ว</summary>
        public void SetDishCooked(string dishID)
        {
            if (string.IsNullOrEmpty(dishID)) return;
            SetFlag(CookedDishKey(dishID), 1);
        }

        private static string CookedDishKey(string dishID) => $"cooked_{dishID.ToLower()}";

        /// <summary>ตรวจสอบเปรียบเทียบค่า Flag ตามเงื่อนไขที่กำหนด</summary>
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
