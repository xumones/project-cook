using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Cooking
{
    /// <summary>ลูกบิดเตา ลากเมาส์แนวนอนเพื่อปรับระดับไฟ 0-100 มุมของโมเดลตอนเริ่มเกมคือตำแหน่งปิด</summary>
    public class StoveKnob : MonoBehaviour, ITurnable
    {
        private const float MaxLevel = 100f;

        [Tooltip("ระดับไฟปัจจุบัน 0 = ปิด, 100 = แรงสุด")]
        [Range(0f, MaxLevel)]
        [SerializeField] private float level;

        [Header("Rotation")]
        [Tooltip("แกนหมุนของโมเดล (local)")]
        [SerializeField] private Vector3 rotationAxis = Vector3.right;

        [Tooltip("มุมที่ระดับไฟ 100 (องศา) ใส่ค่าติดลบเพื่อกลับทิศหมุน")]
        [SerializeField] private float maxAngle = 270f;

        [Header("Input")]
        [Tooltip("ระดับไฟที่เปลี่ยนต่อการขยับเมาส์ 1 พิกเซล")]
        [SerializeField] private float sensitivity = 0.3f;

        [Tooltip("ปล่อยมือตอนระดับไฟต่ำกว่านี้จะดีดกลับไปปิด")]
        [SerializeField] private float offSnap = 5f;

        private Quaternion offRotation;

        /// <summary>ระดับไฟ 0-1</summary>
        public float NormalizedLevel => level / MaxLevel;

        private void Awake()
        {
            offRotation = transform.localRotation;
            ApplyRotation();
        }

        public void Turn(float horizontalDelta)
        {
            SetLevel(level + horizontalDelta * sensitivity);
        }

        public void Release()
        {
            if (level < offSnap) SetLevel(0f);
        }

        private void SetLevel(float value)
        {
            level = Mathf.Clamp(value, 0f, MaxLevel);
            ApplyRotation();
        }

        private void ApplyRotation()
        {
            transform.localRotation = offRotation * Quaternion.AngleAxis(level / MaxLevel * maxAngle, rotationAxis);
        }
    }
}
