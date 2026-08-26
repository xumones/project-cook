using UnityEngine;

namespace ProjectCook.CameraSystem
{
    /// <summary>
    /// สคริปต์จัดการขยับตำแหน่ง localPosition ของ Head Anchor ด้วยคลื่น Sin/Cos ตามการเคลื่อนที่ของผู้เล่น
    /// เพื่อให้ CinemachineCamera ลอยตามตำแหน่ง head anchor
    /// </summary>
    public class HeadBob : MonoBehaviour
    {
        [Header("Bobbing Settings")]
        [SerializeField] private float walkBobSpeed = 13f;      // ความเร็วคลื่นตอนเดิน
        [SerializeField] private float walkBobAmount = 0.015f;    // ระยะกล้องไหวตอนเดิน
        [SerializeField] private float sprintBobSpeed = 16f;    // ความเร็วคลื่นตอนวิ่ง
        [SerializeField] private float sprintBobAmount = 0.03f;   // ระยะกล้องไหวตอนวิ่ง
        [SerializeField] private float resetSpeed = 6f;         // ความเร็วการคืนกล้องสู่จุดเดิมตอนหยุดเดิน

        [Header("References")]
        [SerializeField] private CharacterController controller; // อ้างอิง CharacterController เพื่อเช็คความเร็วและสถานะติดพื้น
        [SerializeField] private Transform headTransform;       // Transform ของ Head Anchor ที่ต้องการให้ขยับไหว

        private float timer = 0f;
        private Vector3 defaultHeadPos;

        private void Start()
        {
            if (controller == null)
            {
                controller = GetComponentInParent<CharacterController>();
            }

            // หากไม่ได้ระบุ หรือดันระบุไปที่ Main Camera (ซึ่งโดน CinemachineBrain เขียนทับทุกเฟรม) ให้ใช้ transform ของตัวเอง (Head)
            if (headTransform == null || (Camera.main != null && headTransform == Camera.main.transform))
            {
                headTransform = transform;
            }

            // บันทึกตำแหน่งเดิมของ Head ไว้เป็นจุดอ้างอิง
            defaultHeadPos = headTransform.localPosition;
        }

        private void LateUpdate()
        {
            if (controller == null || headTransform == null) return;

            // 1. คำนวณความเร็วเคลื่อนที่บนระนาบพื้น (ไม่คิดแกน Y ที่เป็นแรงโน้มถ่วง)
            Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
            float speed = horizontalVelocity.magnitude;

            // 2. เช็คว่าผู้เล่นติดพื้นอยู่ และมีการเคลื่อนที่จริงหรือไม่
            if (controller.isGrounded && speed > 0.1f)
            {
                // เช็คว่ากำลังวิ่งเร็วหรือไม่
                bool isSprinting = speed > 2.5f;
                float currentBobSpeed = isSprinting ? sprintBobSpeed : walkBobSpeed;
                float currentBobAmount = isSprinting ? sprintBobAmount : walkBobAmount;

                timer += Time.deltaTime * currentBobSpeed;

                // คำนวณตำแหน่งใหม่ด้วย Sin และ Cos
                float newX = defaultHeadPos.x + Mathf.Cos(timer / 2f) * currentBobAmount;
                float newY = defaultHeadPos.y + Mathf.Sin(timer) * currentBobAmount;

                headTransform.localPosition = new Vector3(newX, newY, defaultHeadPos.z);
            }
            else
            {
                // 3. เมื่อหยุดเดินหรืออยู่กลางอากาศ ค่อยๆ ปรับคืนสู่ตำแหน่งปกติ (Smooth Reset)
                timer = 0f;
                headTransform.localPosition = Vector3.Lerp(headTransform.localPosition, defaultHeadPos, Time.deltaTime * resetSpeed);
            }
        }
    }
}
