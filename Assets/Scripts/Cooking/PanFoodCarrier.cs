using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// สคริปต์จัดการฟิสิกส์อาหารในกระทะ (Hybrid Pan Food Carrier System)
    /// ช่วยให้อาหารตอบสนองต่อการเอียง การสไลด์ และการเหวี่ยงกระทะได้อย่างสมจริง (Slide, Roll, Momentum)
    /// </summary>
    public class PanFoodCarrier : MonoBehaviour
    {
        [Header("Pan References")]
        [Tooltip("Transform ของกระทะ (หากไม่ใส่จะอ้างอิงจาก Transform ตัวเองหรือ Parent)")]
        [SerializeField] private Transform panTransform;

        [Header("Physics Multipliers")]
        [Tooltip("ตัวคูณแรงเหวี่ยง (Inertia Force) เมื่อสไลด์กระทะ WASD")]
        [SerializeField] private float momentumMultiplier = 5f;

        [Tooltip("ตัวคูณแรงสไลด์ตามความเอียงของกระทะ (Slope Gravity Assist)")]
        [SerializeField] private float slopeForceMultiplier = 8f;

        [Tooltip("ตัวคูณแรงหมุน/พลิกตัวของวัตถุ (Rolling Torque)")]
        [SerializeField] private float rollTorqueMultiplier = 3f;

        [Tooltip("แรงดึงประคองเข้าหาก้นกระทะเบาๆ ป้องกันอาหารกระเด็นหลุดกระทะง่ายเกินไป")]
        [SerializeField] private float bowlAttractionForce = 2f;

        [Header("Filtering Settings")]
        [Tooltip("เปิดใช้งานการกรองเฉพาะวัตถุที่มี Tag ที่กำหนด (หากปิดจะส่งแรงให้ Rigidbody ทุกชิ้นที่อยู่ในกระทะ)")]
        [SerializeField] private bool useTagFilter = false;

        [Tooltip("Tag ของวัตถุดิบอาหาร เช่น 'Food' หรือ 'Ingredient'")]
        [SerializeField] private string foodTag = "Food";

        private readonly List<Rigidbody> foodRigidbodies = new List<Rigidbody>();
        private Vector3 previousPanPosition;

        private void Awake()
        {
            if (panTransform == null)
            {
                panTransform = transform;
            }

            previousPanPosition = panTransform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb == null || rb.isKinematic) return;

            if (useTagFilter && !other.CompareTag(foodTag)) return;

            if (!foodRigidbodies.Contains(rb))
            {
                foodRigidbodies.Add(rb);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && foodRigidbodies.Contains(rb))
            {
                foodRigidbodies.Remove(rb);
            }
        }

        private void FixedUpdate()
        {
            if (panTransform == null) return;

            // คำนวณความเร็วการขยับของกระทะใน Physics Step
            Vector3 panDeltaPos = panTransform.position - previousPanPosition;
            Vector3 panVelocity = panDeltaPos / Time.fixedDeltaTime;
            previousPanPosition = panTransform.position;

            // ลบ null rigidbody (วัตถุที่ถูกทำลายไประหว่างเล่น)
            foodRigidbodies.RemoveAll(item => item == null || !item.gameObject.activeInHierarchy);

            if (foodRigidbodies.Count == 0) return;

            Vector3 panUp = panTransform.up;
            Vector3 panCenter = panTransform.position;

            // คำนวณทิศทางความเอียงของกระทะ (Slope Vector)
            Vector3 slopeDirection = Vector3.ProjectOnPlane(Physics.gravity, panUp);

            foreach (Rigidbody foodRb in foodRigidbodies)
            {
                if (foodRb == null || foodRb.isKinematic) continue;

                // 1. แรงเหวี่ยงสไลด์กระทะ (Inertia Force จากการขยับ WASD)
                Vector3 inertiaForce = -panVelocity * momentumMultiplier;
                foodRb.AddForce(inertiaForce, ForceMode.Acceleration);

                // 2. แรงสไลด์ตามความเอียงกระทะ (Slope Slide Force)
                foodRb.AddForce(slopeDirection * slopeForceMultiplier, ForceMode.Acceleration);

                // 3. แรงดึงดูดเข้าหาก้นกระทะเบาๆ (Bowl Cavity Center Attraction)
                Vector3 toCenterDir = (panCenter - foodRb.position);
                toCenterDir.y = 0f; // เน้นดึงในแนวราบก้นกระทะ
                foodRb.AddForce(toCenterDir * bowlAttractionForce, ForceMode.Acceleration);

                // 4. แรงหมุนตัว/กลิ้งของวัตถุดิบ (Rolling & Tumbling Torque)
                Vector3 foodVel = foodRb.linearVelocity;
                if (foodVel.sqrMagnitude > 0.01f)
                {
                    Vector3 rollAxis = Vector3.Cross(panUp, foodVel.normalized);
                    foodRb.AddTorque(rollAxis * (foodVel.magnitude * rollTorqueMultiplier), ForceMode.Acceleration);
                }
            }
        }

        /// <summary>
        /// ดึงรายการ Rigidbody ของอาหารที่อยู่ในกระทะในปัจจุบัน
        /// </summary>
        public IReadOnlyList<Rigidbody> GetFoodInPan()
        {
            foodRigidbodies.RemoveAll(item => item == null);
            return foodRigidbodies.AsReadOnly();
        }
    }
}
