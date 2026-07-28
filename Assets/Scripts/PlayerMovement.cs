using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float gravity = -9.81f;

    [Header("References")]
    [SerializeField] private Transform orientation;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference sprintAction;

    private CharacterController controller;
    private Vector3 velocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Enable();
        }
        if (sprintAction != null && sprintAction.action != null)
        {
            sprintAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Disable();
        }
        if (sprintAction != null && sprintAction.action != null)
        {
            sprintAction.action.Disable();
        }
    }

    private void Update()
    {
        // 1. อ่านค่า Input การเดิน (Vector2: WASD / Left Stick)
        Vector2 input = Vector2.zero;
        if (moveAction != null && moveAction.action != null)
        {
            input = moveAction.action.ReadValue<Vector2>();
        }

        // 2. เช็คว่ากำลังกดปุ่มวิ่งเร็ว (Sprint: Shift / Left Stick Press) หรือไม่
        bool isSprinting = false;
        if (sprintAction != null && sprintAction.action != null)
        {
            isSprinting = sprintAction.action.IsPressed();
        }

        // 3. เลือกความเร็วระหว่าง เดิน (walkSpeed) หรือ วิ่ง (sprintSpeed)
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // 4. คำนวณทิศทางการเคลื่อนที่ตามทิศของ orientation (หรือทิศผู้เล่น)
        Vector3 moveDir = Vector3.zero;
        if (orientation != null)
        {
            moveDir = (orientation.forward * input.y) + (orientation.right * input.x);
        }
        else
        {
            moveDir = (transform.forward * input.y) + (transform.right * input.x);
        }
        moveDir.y = 0f; // ล็อคไม่ให้เอียงลอยขึ้นหรือจมดิน

        // 5. สั่งเคลื่อนที่ในแกน XZ
        if (moveDir.sqrMagnitude > 0.001f)
        {
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }

        // 6. คำนวณแรงโน้มถ่วง (Gravity)
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // แรงกดพื้นเล็กน้อยเพื่อความเสถียร
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
