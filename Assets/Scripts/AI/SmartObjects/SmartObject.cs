using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.AI
{
    [Serializable]
    public struct Advertisement
    {
        public MeterSO meter;

        [Tooltip("ใช้จนครบเวลาแล้วมาตรวัดนี้เปลี่ยนเท่าไหร่ ใช้ไม่ครบได้ตามสัดส่วน")]
        public float amount;
    }

    /// <summary>ของที่ตัวละครเดินไปใช้ได้: ประกาศว่าช่วยมาตรวัดไหนเท่าไหร่ (แบบ The Sims) มีจุดยืนใช้ เวลาใช้ และการจอง</summary>
    // ตัวละครไม่ต้องรู้ว่าของคืออะไร รู้แค่ที่ประกาศ ของใหม่จึงใช้ได้ทันทีโดยไม่แก้โค้ด AI
    public class SmartObject : MonoBehaviour
    {
        [Tooltip("จุดที่ตัวละครยืนใช้ หันตามแกนหน้าของจุดนี้ ว่าง = ตัวมันเอง")]
        [SerializeField] private Transform useSpot;

        [Tooltip("ใช้นานกี่วินาทีถึงได้ผลครบตามที่ประกาศ")]
        [Min(1f)]
        [SerializeField] private float useSeconds = 15f;

        [SerializeField] private List<Advertisement> advertisements = new List<Advertisement>();

        private IOccupancy occupancy;
        private bool occupancySearched;
        private object reservedBy;

        private Transform Spot => useSpot != null ? useSpot : transform;

        public Vector3 UsePosition => Spot.position;
        public Quaternion UseRotation => Spot.rotation;
        public float UseSeconds => useSeconds;
        public IReadOnlyList<Advertisement> Advertisements => advertisements;

        /// <summary>มีคนอื่นที่ไม่ใช่ตัวละคร AI ใช้อยู่ เช่น ผู้เล่นกำลังใช้เครื่องเล่นแผ่น</summary>
        public bool IsOccupied
        {
            get
            {
                // หาครั้งแรกที่ถาม ไม่ใช่ใน Awake ที่ไม่ทำงานตอนอยู่ใน Editor
                if (!occupancySearched)
                {
                    occupancy = GetComponentInParent<IOccupancy>();
                    occupancySearched = true;
                }
                return occupancy != null && occupancy.IsOccupied;
            }
        }

        private void OnEnable() => SmartObjectRegistry.Register(this);

        private void OnDisable()
        {
            SmartObjectRegistry.Unregister(this);
            reservedBy = null;
        }

        /// <summary>ใช้จนครบแล้วมาตรวัดนี้เปลี่ยนเท่าไหร่ 0 = ไม่ได้ประกาศ</summary>
        public float Promise(MeterSO meter)
        {
            float total = 0f;
            foreach (Advertisement ad in advertisements)
            {
                if (ad.meter == meter) total += ad.amount;
            }
            return total;
        }

        public bool IsAvailableTo(object user) => !IsOccupied && (reservedBy == null || reservedBy == user);

        public bool TryReserve(object user)
        {
            if (!IsAvailableTo(user)) return false;

            reservedBy = user;
            return true;
        }

        public void Release(object user)
        {
            if (reservedBy == user) reservedBy = null;
        }

        /// <summary>ให้ผลตามสัดส่วนเวลาที่ใช้ไป ถูกขัดกลางคันก็ได้ผลเท่าที่ใช้</summary>
        public void ApplyUse(MeterSet meters, float seconds)
        {
            float share = seconds / useSeconds;
            foreach (Advertisement ad in advertisements)
            {
                meters.Change(ad.meter, ad.amount * share);
            }
        }
    }
}
