using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// ส่งแรงฟิสิกส์ให้วัตถุดิบที่อยู่ในภาชนะเคลื่อนที่ตามการสไลด์และการเอียงของภาชนะ
    /// (แรงเฉื่อย, แรงไหลตามความลาดเอียง, แรงประคองเข้าก้นภาชนะ และแรงกลิ้งตัว)
    ///
    /// แยกออกมาจาก PanController เพราะตรรกะนี้ไม่ได้เป็นของกระทะโดยเฉพาะ
    /// ภาชนะใดก็ตามที่เคลื่อนที่ได้ (หม้อ, กระทะเหล็ก, ถาด, จาน) ใช้ตรรกะชุดเดียวกันนี้
    /// เพียงแปะ Component นี้ลงบน GameObject ของภาชนะที่เคลื่อนที่ได้เลย
    /// </summary>
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
                foodContainer = GetComponent<FoodContainer>();
                if (foodContainer == null)
                {
                    foodContainer = GetComponentInChildren<FoodContainer>();
                }
            }

            previousContainerPosition = containerTransform.position;
        }

        private void FixedUpdate()
        {
            if (foodContainer == null || containerTransform == null) return;

            var items = foodContainer.GetContainedFoodItems();
            if (items == null || items.Count == 0)
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

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || item.Rigidbody == null || item.Rigidbody.isKinematic) continue;

                // หากถูกคีบจับอยู่ ให้ข้ามแรงของภาชนะ (เพื่อไม่ให้แย่งแรงกับ IngredientDragController)
                if (item.Ingredient != null && item.Ingredient.IsGripped) continue;

                // --- รวมแรงทั้งหมดแล้วส่งครั้งเดียว (Batching Forces) ---
                Vector3 inertiaForce = -containerVelocity * momentumMultiplier;
                Vector3 slopeForce = slopeDirection * slopeForceMultiplier;
                Vector3 toCenterDir = (containerCenter - item.Rigidbody.position);
                toCenterDir.y = 0f;
                Vector3 attractionForce = toCenterDir * bowlAttractionForce;

                Vector3 combinedForce = inertiaForce + slopeForce + attractionForce;
                if (combinedForce.sqrMagnitude > 0.0001f)
                {
                    item.Rigidbody.AddForce(combinedForce, ForceMode.Acceleration);
                }

                // แรงหมุนตัว/กลิ้งของวัตถุดิบ (Rolling & Tumbling Torque)
                Vector3 foodVelocity = item.Rigidbody.linearVelocity;
                if (foodVelocity.sqrMagnitude > 0.01f)
                {
                    Vector3 rollAxis = Vector3.Cross(containerUp, foodVelocity.normalized);
                    item.Rigidbody.AddTorque(rollAxis * (foodVelocity.magnitude * rollTorqueMultiplier), ForceMode.Acceleration);
                }
            }
        }
    }
}
