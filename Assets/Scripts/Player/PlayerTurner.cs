using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;

namespace ProjectCook.Player
{
    /// <summary>กดค้างที่ ITurnable ที่เล็งอยู่แล้วส่งระยะเมาส์แนวนอนให้ ระหว่างนั้นล็อกกล้อง การเดิน และซ่อนเคอร์เซอร์</summary>
    public class PlayerTurner : MonoBehaviour
    {
        [Header("Input Settings")]
        [Tooltip("ปุ่มกดค้างเพื่อหมุน (คลิกซ้าย ตัวเดียวกับที่ใช้คีบวัตถุดิบ)")]
        [SerializeField] private InputActionReference holdAction;

        [Tooltip("Vector2 ระยะที่เมาส์ขยับต่อเฟรม")]
        [SerializeField] private InputActionReference deltaAction;

        [Header("References")]
        [Tooltip("เว้นว่าง = หาจาก GameObject นี้")]
        [SerializeField] private PlayerMovement movement;

        private ITurnable current;

        // แคชไว้ตอน subscribe เพราะตอนหยุดเกม Instance คืน null ไปแล้ว แต่ยังต้องถอน event ให้ครบ
        private GameModeManager gameMode;

        private void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
        }

        private void OnEnable()
        {
            holdAction.EnableAction();
            // ไม่ Disable คืนตอนปิด เพราะกล้องใช้ Look ตัวเดียวกันหมุนมุมมองอยู่
            deltaAction.EnableAction();
        }

        private void OnDisable()
        {
            holdAction.DisableAction();
            EndTurn();
        }

        private void Start()
        {
            gameMode = GameModeManager.Instance;
            if (gameMode != null) gameMode.ModeChanged += HandleModeChanged;
        }

        private void OnDestroy()
        {
            if (gameMode != null) gameMode.ModeChanged -= HandleModeChanged;
        }

        // เข้า/ออกสถานีหรือเริ่มคุยระหว่างหมุนอยู่ ให้ปล่อยทันที
        private void HandleModeChanged(GameMode mode) => EndTurn();

        private void Update()
        {
            if (current == null)
            {
                if (GameModeManager.WorldInputAllowed && holdAction.WasPressedThisFrame()) TryBeginTurn();
                return;
            }

            if (!holdAction.IsPressed())
            {
                EndTurn();
                return;
            }

            current.Turn(deltaAction.ReadValue<Vector2>().x);
        }

        private void TryBeginTurn()
        {
            Collider target = PlayerAiming.Instance != null ? PlayerAiming.Instance.Target : null;
            current = target != null ? target.GetComponentInParent<ITurnable>() : null;
            if (current == null) return;

            if (CameraManager.Instance != null) CameraManager.Instance.RequestLookLock(this);
            if (movement != null) movement.RequestMovementLock(this);
            CursorManager.RequestHide(this);
        }

        private void EndTurn()
        {
            if (current == null) return;

            current.Release();
            current = null;

            if (CameraManager.Instance != null) CameraManager.Instance.ReleaseLookLock(this);
            if (movement != null) movement.ReleaseMovementLock(this);
            CursorManager.ReleaseHide(this);
        }
    }
}
