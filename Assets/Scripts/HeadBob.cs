using UnityEngine;

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
    [SerializeField] private Transform cameraTransform;     // Transform ของ Main Camera ที่ต้องการให้ไหว

    private float timer = 0f;
    private Vector3 defaultCamPos;

    private void Start()
    {
        if (cameraTransform != null)
        {
            // บันทึกตำแหน่งเดิมของกล้องไว้เป็นจุดอ้างอิง
            defaultCamPos = cameraTransform.localPosition;
        }
    }

    private void Update()
    {
        if (controller == null || cameraTransform == null) return;

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

            // เดินหน้าเวลาสะสมตามความเร็ว
            timer += Time.deltaTime * currentBobSpeed;

            // คำนวณตำแหน่งใหม่ด้วย Sin และ Cos
            float newX = defaultCamPos.x + Mathf.Cos(timer / 2f) * currentBobAmount;
            float newY = defaultCamPos.y + Mathf.Sin(timer) * currentBobAmount;

            cameraTransform.localPosition = new Vector3(newX, newY, defaultCamPos.z);
        }
        else
        {
            // 3. เมื่อหยุดเดินหรืออยู่กลางอากาศ ค่อยๆ ปรับกล้องคืนสู่ตำแหน่งปกติ (Smooth Reset)
            timer = 0f;
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, defaultCamPos, Time.deltaTime * resetSpeed);
        }
    }
}
