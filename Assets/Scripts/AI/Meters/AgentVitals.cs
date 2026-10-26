using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>มาตรวัดกับความมั่นคงของตัวละครหนึ่งตัว เดินเวลาให้ทุกเฟรม</summary>
    // ฉากถูกซ่อน (ผู้เล่นออกไปข้างนอก) Update ไม่ทำงาน ค่าทุกตัวจึงหยุดรอจนกลับมา
    public class AgentVitals : MonoBehaviour
    {
        [Serializable]
        private class MeterEntry
        {
            public MeterSO meter;

            [Range(0f, MeterSO.Max)]
            public float startValue = 80f;
        }

        [Tooltip("มาตรวัดที่ตัวละครนี้มี พร้อมค่าตอนเริ่มเกมใหม่")]
        [SerializeField] private List<MeterEntry> meters = new List<MeterEntry>();

        [SerializeField] private WellbeingSettings wellbeing = new WellbeingSettings();

        public MeterSet Meters { get; } = new MeterSet();
        public Wellbeing Wellbeing { get; private set; }

        private void Awake()
        {
            foreach (MeterEntry entry in meters)
            {
                if (entry.meter == null)
                {
                    Debug.LogWarning($"[AgentVitals] {name} มีช่องมาตรวัดว่าง", this);
                    continue;
                }
                Meters.Add(entry.meter, entry.startValue);
            }
            Wellbeing = new Wellbeing(wellbeing);
        }

        private void Update()
        {
            float minutes = Time.deltaTime / 60f;
            Meters.Advance(minutes);
            Wellbeing.Advance(Meters, minutes);
        }
    }
}
