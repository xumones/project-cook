// ============================================================================
//  PLAYTEST ONLY — ภาพบอกปุ่มชั่วคราว รอระบบสอนเล่นจริงมาแทน
//  ลบทิ้งได้เมื่อไม่ต้องการ: ลบไฟล์นี้และ GameObject "ControlHint" ใน Canvas
//  ไม่มีโค้ดส่วนอื่นอ้างถึงที่นี่
// ============================================================================
using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Playtest
{
    /// <summary>ภาพสอนปุ่มสำหรับ playtest โผล่เมื่อผู้เล่นยืนนิ่งพักหนึ่ง และจางหายเมื่อเริ่มเดิน</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ControlHintUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Transform ของผู้เล่น ใช้ดูว่ากำลังขยับตัวอยู่ไหม")]
        [SerializeField] private Transform player;

        [Header("Timing")]
        [Tooltip("ต้องยืนนิ่งนานเท่านี้ก่อนภาพจะเริ่มโผล่ (วินาที)")]
        [SerializeField] private float idleDelay = 3f;
        [SerializeField] private float fadeInDuration = 1f;
        [Tooltip("เวลาจางหายหลังเริ่มขยับตัว (วินาที)")]
        [SerializeField] private float fadeOutDuration = 3f;

        [Tooltip("ระยะขยับต่อวินาทีที่ถือว่า 'กำลังเดิน' (เมตร/วินาที)")]
        [SerializeField] private float moveSpeedThreshold = 0.1f;

        private CanvasGroup canvasGroup;
        private Vector3 previousPosition;
        private float idleTimer;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        private void Start()
        {
            if (player != null)
            {
                previousPosition = player.position;
            }
        }

        private void Update()
        {
            if (player == null) return;

            float speed = Time.deltaTime > 0f ? (player.position - previousPosition).magnitude / Time.deltaTime : 0f;
            previousPosition = player.position;

            // ระหว่างคุยหรืออยู่ในสถานี หน้าจอมีอย่างอื่นให้ดูอยู่แล้ว ไม่ต้องสอนปุ่มเดิน
            bool exploring = GameModeManager.Instance == null || GameModeManager.Instance.CurrentMode == GameMode.Exploring;
            bool moving = speed >= moveSpeedThreshold;

            idleTimer = moving || !exploring ? 0f : idleTimer + Time.deltaTime;

            bool show = idleTimer >= idleDelay;
            float target = show ? 1f : 0f;
            float duration = show ? fadeInDuration : fadeOutDuration;
            float rate = duration > 0f ? 1f / duration : 100f;

            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, rate * Time.deltaTime);
        }
    }
}
