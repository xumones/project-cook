using UnityEngine;

namespace ProjectCook.UI
{
    /// <summary>
    /// สคริปต์จัดการจุด UI กลางหน้าจอ (Center Dot / Crosshair Reticle) 
    /// แสดงผลเมื่อผู้เล่นหันกล้อง (Fade In 2s), รอ 3s หลังหยุดหัน และ Fade Out หายไปใน 2s
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CenterDotUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("CanvasGroup สำหรับควบคุมความโปร่งใสของจุดกลางหน้าจอ (หากไม่ระบุจะเลือกบน GameObject นี้โดยอัตโนมัติ)")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Tooltip("Transform ของกล้องหลักสำหรับตรวจจับการหันกล้อง (หากไม่ระบุจะค้นหา Camera.main โดยอัตโนมัติ)")]
        [SerializeField] private Transform targetCamera;

        [Header("Fade & Delay Settings")]
        [Tooltip("ระยะเวลาการ Fade In แสดงจุดเมื่อเริ่มหันหน้า (วินาที)")]
        [SerializeField] private float fadeInDuration = 2.0f;

        [Tooltip("ระยะเวลานิ่งรอก่อนเริ่ม Fade Out หลังจากหยุดหันหน้า (วินาที)")]
        [SerializeField] private float idleDelayBeforeFadeOut = 3.0f;

        [Tooltip("ระยะเวลาการ Fade Out จางหายไป (วินาที)")]
        [SerializeField] private float fadeOutDuration = 2.0f;

        [Tooltip("ความเร็วหมุนกล้องขั้นต่ำที่ถือว่ากำลังหันหน้า (องศา/วินาที)")]
        [SerializeField] private float rotationSpeedThreshold = 1.0f;

        private Quaternion previousRotation;
        private float idleTimer = 0f;
        private float currentAlphaTarget = 0f;

        private void Start()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }

            if (targetCamera != null)
            {
                previousRotation = targetCamera.rotation;
            }

            // เริ่มต้นซ่อนจุด UI ไว้ (Alpha = 0)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void Update()
        {
            if (canvasGroup == null) return;

            // ค้นหากล้องหลักหาก targetCamera ยังไม่ได้ตั้งค่าไว้
            if (targetCamera == null)
            {
                if (Camera.main != null)
                {
                    targetCamera = Camera.main.transform;
                    previousRotation = targetCamera.rotation;
                }
                return;
            }

            // 1. คำนวณความเร็วการหมุนกล้องในแต่ละเฟรม (Degrees per second)
            float angleDelta = Quaternion.Angle(targetCamera.rotation, previousRotation);
            float rotationSpeed = Time.deltaTime > 0f ? angleDelta / Time.deltaTime : 0f;
            previousRotation = targetCamera.rotation;

            bool isTurning = rotationSpeed >= rotationSpeedThreshold;

            // 2. ปรับการทำงานตามการเคลื่อนไหวของกล้อง
            if (isTurning)
            {
                // หากกำลังหันหน้า: รีเซ็ตเวลานิ่งรอ และเปลี่ยนเป้าหมายความเด่นชัดเป็น 1.0 (แสดงเต็มที่)
                idleTimer = 0f;
                currentAlphaTarget = 1.0f;
            }
            else
            {
                // หากหยุดหันหน้า: นำเวลาไปสะสมใน idleTimer
                idleTimer += Time.deltaTime;

                if (idleTimer >= idleDelayBeforeFadeOut)
                {
                    // หากหยุดหันหน้าครบ 3 วินาทีแล้ว: เปลี่ยนเป้าหมายความเด่นชัดเป็น 0.0 (จางหายไป)
                    currentAlphaTarget = 0.0f;
                }
            }

            // 3. คำนวณอัตราความเร็วการ Fade (Fade Speed)
            float fadeRate = 0f;
            if (currentAlphaTarget > canvasGroup.alpha)
            {
                // กำลัง Fade In -> คำนวณอัตราความเร็วอ้างอิงตาม fadeInDuration (2 วินาที)
                fadeRate = fadeInDuration > 0f ? 1.0f / fadeInDuration : 100f;
            }
            else if (currentAlphaTarget < canvasGroup.alpha)
            {
                // กำลัง Fade Out -> คำนวณอัตราความเร็วอ้างอิงตาม fadeOutDuration (2 วินาที)
                fadeRate = fadeOutDuration > 0f ? 1.0f / fadeOutDuration : 100f;
            }

            // 4. ค่อยๆ ปรับค่า Alpha ไปยังเป้าหมายแบบเรียบเนียน (Smooth Transition)
            if (fadeRate > 0f)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, currentAlphaTarget, fadeRate * Time.deltaTime);
            }
        }
    }
}
