using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>ส่งแรงฟิสิกส์ให้วัตถุดิบที่อยู่ในภาชนะเคลื่อนที่ตามการสไลด์และการเอียงของภาชนะ</summary>
    [DisallowMultipleComponent]
    public class ContainerContentPhysics : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Transform ของภาชนะที่เคลื่อนที่ (หากไม่ใส่จะใช้ Transform ของ GameObject นี้)")]
        [SerializeField] private Transform containerTransform;

        [Tooltip("ภาชนะที่เก็บวัตถุดิบ (หากไม่ใส่จะค้นหาบน GameObject นี้หรือลูกๆ อัตโนมัติ)")]
        [SerializeField] private FoodContainer foodContainer;

        [Header("Food Physics Settings")]
        [Tooltip("ตัวคูณแรงเหวี่ยง (Inertia Force) เมื่อสไลด์ภาชนะ")]
        [SerializeField] private float momentumMultiplier = 5f;

        [Tooltip("ตัวคูณแรงสไลด์ตามความเอียงของภาชนะ (Slope Gravity Assist)")]
        [SerializeField] private float slopeForceMultiplier = 8f;

        [Tooltip("ตัวคูณแรงหมุน/พลิกตัวของวัตถุ (Rolling Torque)")]
        [SerializeField] private float rollTorqueMultiplier = 3f;

        [Tooltip("แรงดึงประคองเข้าหาก้นภาชนะเบาๆ ป้องกันอาหารกระเด็นหลุดง่ายเกินไป")]
        [SerializeField] private float bowlAttractionForce = 2f;

        private Vector3 previousContainerPosition;

        private void Awake()
        {
            if (containerTransform == null)
            {
                containerTransform = transform;
            }

            if (foodContainer == null)
            {
                foodContainer = FoodContainer.FindFor(this);
            }

            previousContainerPosition = containerTransform.position;
        }

        private void FixedUpdate()
        {
            if (foodContainer == null || containerTransform == null) return;

            var items = foodContainer.Contents;
            if (items.Count == 0)
            {
                // ไม่มีวัตถุดิบในภาชนะ แต่ยังต้องอัปเดตตำแหน่งอ้างอิงไว้
                // เพื่อไม่ให้คำนวณความเร็วผิดพลาดเป็นค่ามหาศาลตอนมีวัตถุดิบชิ้นแรกตกลงมา
                previousContainerPosition = containerTransform.position;
                return;
            }

            Vector3 deltaPosition = containerTransform.position - previousContainerPosition;
            Vector3 containerVelocity = deltaPosition / Time.fixedDeltaTime;
            previousContainerPosition = containerTransform.position;

            Vector3 containerUp = containerTransform.up;
            Vector3 containerCenter = containerTransform.position;
            Vector3 slopeDirection = Vector3.ProjectOnPlane(Physics.gravity, containerUp);

            Vector3 inertiaForce = -containerVelocity * momentumMultiplier;
            Vector3 slopeForce = slopeDirection * slopeForceMultiplier;

            for (int i = 0; i < items.Count; i++)
            {
                IngredientInstance ingredient = items[i].Ingredient;
                if (ingredient == null) continue;

                var bodies = ingredient.Bodies;
                for (int b = 0; b < bodies.Count; b++)
                {
                    // ชิ้นที่ถูกคีบอยู่ปล่อยให้ IngredientGrabber คุมแรงคนเดียว
                    if (bodies[b] == ingredient.GrippedBody) continue;

                    ApplyForces(bodies[b], inertiaForce, slopeForce, containerCenter, containerUp);
                }
            }
        }

        private void ApplyForces(Rigidbody body, Vector3 inertiaForce, Vector3 slopeForce, Vector3 containerCenter, Vector3 containerUp)
        {
            if (body == null || body.isKinematic) return;

            Vector3 toCenterDir = containerCenter - body.position;
            toCenterDir.y = 0f;

            Vector3 combinedForce = inertiaForce + slopeForce + toCenterDir * bowlAttractionForce;
            if (combinedForce.sqrMagnitude > 0.0001f)
            {
                body.AddForce(combinedForce, ForceMode.Acceleration);
            }

            // แรงกลิ้ง/พลิกตัวตามทิศที่ไถล
            Vector3 velocity = body.linearVelocity;
            if (velocity.sqrMagnitude > 0.01f)
            {
                Vector3 rollAxis = Vector3.Cross(containerUp, velocity.normalized);
                body.AddTorque(rollAxis * (velocity.magnitude * rollTorqueMultiplier), ForceMode.Acceleration);
            }
        }
    }
}
