using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.AI
{
    [Serializable]
    public struct SavedMeter
    {
        public string id;
        public float value;
    }

    /// <summary>ค่าปัจจุบันของมาตรวัดทุกตัวของตัวละครหนึ่งตัว ค่าอยู่ในช่วง 0–100 เสมอ</summary>
    public class MeterSet
    {
        private readonly List<MeterSO> meters = new List<MeterSO>();
        private readonly Dictionary<MeterSO, float> values = new Dictionary<MeterSO, float>();

        public IReadOnlyList<MeterSO> Meters => meters;

        public void Add(MeterSO meter, float startValue)
        {
            if (values.ContainsKey(meter)) return;

            meters.Add(meter);
            values[meter] = Mathf.Clamp(startValue, 0f, MeterSO.Max);
        }

        public bool Contains(MeterSO meter) => meter != null && values.ContainsKey(meter);

        /// <summary>ค่าของมาตรวัดนี้ 0 ถ้าตัวละครไม่มีมาตรวัดนี้</summary>
        public float Get(MeterSO meter) => meter != null && values.TryGetValue(meter, out float value) ? value : 0f;

        public void Set(MeterSO meter, float value)
        {
            if (Contains(meter)) values[meter] = Mathf.Clamp(value, 0f, MeterSO.Max);
        }

        public void Change(MeterSO meter, float delta) => Set(meter, Get(meter) + delta);

        public void Advance(float minutes)
        {
            foreach (MeterSO meter in meters)
            {
                values[meter] = meter.Advance(values[meter], minutes);
            }
        }

        public float WellbeingOffset()
        {
            float offset = 0f;
            foreach (MeterSO meter in meters)
            {
                offset += meter.WellbeingOffset(values[meter]);
            }
            return offset;
        }

        public List<SavedMeter> Capture()
        {
            var saved = new List<SavedMeter>(meters.Count);
            foreach (MeterSO meter in meters)
            {
                saved.Add(new SavedMeter { id = meter.MeterId, value = values[meter] });
            }
            return saved;
        }

        // จับคู่ด้วยรหัส มาตรวัดที่ save ไม่รู้จัก (ถูกลบไปแล้ว) ข้ามไป ที่ไม่มีใน save คงค่าเริ่มต้น
        public void Restore(IEnumerable<SavedMeter> saved)
        {
            foreach (SavedMeter entry in saved)
            {
                MeterSO meter = meters.Find(m => m.MeterId == entry.id);
                if (meter != null) Set(meter, entry.value);
            }
        }
    }
}
