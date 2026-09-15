using System;
using UnityEngine;
using UnityEngine.UI;
using ProjectCook.Core;

namespace ProjectCook.UI
{
    /// <summary>จุดกลางหน้าจอ: สลับไอคอนตามสิ่งที่ PlayerAiming เล็งอยู่ จุด default จะจางหายเมื่อกล้องนิ่ง</summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public class CrosshairUI : MonoBehaviour
    {
        [Serializable]
        private struct HintIcon
        {
            public AimHint hint;
            public Sprite sprite;
            [Tooltip("ขนาดบนจอ (px) 0 = ใช้ขนาดเดียวกับจุด default")]
            public Vector2 size;
            [Tooltip("ความทึบตอนแสดง 0 = ใช้ Default Alpha")]
            [Range(0f, 1f)]
            public float alpha;
        }

        [Header("Icons")]
        [Tooltip("จุดกลางจอตอนไม่ได้เล็งอะไรที่ทำอะไรได้")]
        [SerializeField] private Sprite defaultSprite;

        [Tooltip("ขนาดจุด default (px) 0 = ใช้ขนาด RectTransform ตอนเริ่ม")]
        [SerializeField] private Vector2 defaultSize = new Vector2(4f, 4f);

        [Tooltip("ไอคอนต่อการกระทำ hint ที่ไม่มีในตารางจะใช้ default")]
        [SerializeField] private HintIcon[] hintIcons;

        [Tooltip("เวลาที่ hint ใหม่ต้องค้างก่อนสลับไอคอน (วินาที) กันกระพริบตอนกวาดผ่านของหลายชิ้น")]
        [SerializeField] private float hintSwitchDelay = 0.1f;

        [Header("References")]
        [Tooltip("Transform ของกล้องหลักสำหรับตรวจจับการหันกล้อง (เว้นว่าง = Camera.main)")]
        [SerializeField] private Transform targetCamera;

        [Header("Default Dot Fade")]
        [Tooltip("ความทึบสูงสุดของจุด default ตอนแสดงเต็มที่ และของไอคอนที่ไม่ได้ตั้ง alpha ไว้")]
        [Range(0f, 1f)]
        [SerializeField] private float defaultAlpha = 1.0f;

        [Tooltip("ระยะเวลาการ Fade In แสดงจุดเมื่อเริ่มหันหน้า (วินาที)")]
        [SerializeField] private float fadeInDuration = 2.0f;

        [Tooltip("ระยะเวลานิ่งรอก่อนเริ่ม Fade Out หลังจากหยุดหันหน้า (วินาที)")]
        [SerializeField] private float idleDelayBeforeFadeOut = 3.0f;

        [Tooltip("ระยะเวลาการ Fade Out จางหายไป (วินาที)")]
        [SerializeField] private float fadeOutDuration = 2.0f;

        [Tooltip("ความเร็วหมุนกล้องขั้นต่ำที่ถือว่ากำลังหันหน้า (องศา/วินาที)")]
        [SerializeField] private float rotationSpeedThreshold = 1.0f;

        private CanvasGroup canvasGroup;
        private Image image;
        private RectTransform rectTransform;

        // แคช provider ของ collider ล่าสุด จะค้น component ใหม่เฉพาะตอนเล็งเปลี่ยนชิ้น
        private Collider lastTarget;
        private IAimHintProvider lastProvider;
        private AimHint currentHint = AimHint.None;
        private AimHint pendingHint = AimHint.None;
        private float pendingSince;
        private float currentHintAlpha;

        private Quaternion previousRotation;
        private float idleTimer;
        private float currentAlphaTarget;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            image = GetComponent<Image>();
            rectTransform = (RectTransform)transform;
            if (defaultSize.sqrMagnitude <= 0f)
            {
                defaultSize = rectTransform.sizeDelta;
            }
            rectTransform.sizeDelta = defaultSize;
        }

        private void Start()
        {
            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }

            if (targetCamera != null)
            {
                previousRotation = targetCamera.rotation;
            }

            if (defaultSprite == null)
            {
                defaultSprite = image.sprite;
            }

            canvasGroup.alpha = 0f;
        }

        private void Update()
        {
            AimHint hint = StableHint(ResolveHint());
            if (hint != currentHint)
            {
                currentHint = hint;
                ApplyIcon(hint);
            }

            // ไอคอนการกระทำต้องเห็นชัดเสมอ มีแค่จุด default ที่จางได้
            if (hint != AimHint.None)
            {
                canvasGroup.alpha = currentHintAlpha;
                idleTimer = 0f;
                currentAlphaTarget = defaultAlpha;
                return;
            }

            UpdateDefaultFade();
        }

        // hint ใหม่ต้องค้างครบ hintSwitchDelay ก่อนถึงยอมสลับ กันไอคอนกระพริบตอนกวาดผ่านของหลายชิ้นถี่ ๆ
        private AimHint StableHint(AimHint candidate)
        {
            if (candidate == currentHint)
            {
                pendingHint = candidate;
                return currentHint;
            }

            if (candidate != pendingHint)
            {
                pendingHint = candidate;
                pendingSince = Time.unscaledTime;
            }

            return Time.unscaledTime - pendingSince >= hintSwitchDelay ? candidate : currentHint;
        }

        private AimHint ResolveHint()
        {
            // ระหว่างคุยหรืออยู่ในสถานี ไม่มีอะไรให้ทำกับเป้า
            if (!GameModeManager.GameplayInputAllowed) return AimHint.None;

            PlayerAiming aiming = PlayerAiming.Instance;
            if (aiming == null || aiming.HintsSuppressed) return AimHint.None;

            Collider target = aiming.Target;

            if (target != lastTarget)
            {
                lastTarget = target;
                lastProvider = target != null ? target.GetComponentInParent<IAimHintProvider>() : null;
            }

            return lastProvider != null ? lastProvider.GetAimHint() : AimHint.None;
        }

        private void ApplyIcon(AimHint hint)
        {
            if (hintIcons != null)
            {
                foreach (HintIcon entry in hintIcons)
                {
                    if (entry.hint != hint || entry.sprite == null) continue;

                    image.sprite = entry.sprite;
                    rectTransform.sizeDelta = entry.size.sqrMagnitude > 0f ? entry.size : defaultSize;
                    currentHintAlpha = entry.alpha > 0f ? entry.alpha : defaultAlpha;
                    return;
                }
            }

            image.sprite = defaultSprite;
            rectTransform.sizeDelta = defaultSize;
            currentHintAlpha = defaultAlpha;
        }

        private void UpdateDefaultFade()
        {
            if (targetCamera == null)
            {
                if (Camera.main == null) return;
                targetCamera = Camera.main.transform;
                previousRotation = targetCamera.rotation;
            }

            float angleDelta = Quaternion.Angle(targetCamera.rotation, previousRotation);
            float rotationSpeed = Time.deltaTime > 0f ? angleDelta / Time.deltaTime : 0f;
            previousRotation = targetCamera.rotation;

            if (rotationSpeed >= rotationSpeedThreshold)
            {
                idleTimer = 0f;
                currentAlphaTarget = defaultAlpha;
            }
            else
            {
                idleTimer += Time.deltaTime;
                if (idleTimer >= idleDelayBeforeFadeOut)
                {
                    currentAlphaTarget = 0f;
                }
            }

            float fadeRate = 0f;
            if (currentAlphaTarget > canvasGroup.alpha)
            {
                fadeRate = fadeInDuration > 0f ? 1f / fadeInDuration : 100f;
            }
            else if (currentAlphaTarget < canvasGroup.alpha)
            {
                fadeRate = fadeOutDuration > 0f ? 1f / fadeOutDuration : 100f;
            }

            if (fadeRate > 0f)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, currentAlphaTarget, fadeRate * Time.deltaTime);
            }
        }
    }
}
