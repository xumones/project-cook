using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectCook.CameraControl
{
    public class CameraScript : MonoBehaviour
    {
        [SerializeField] private float sensitivity;
        [SerializeField] private float verticalLimit;
        [SerializeField] private float smoothSpeed;
        [SerializeField] private Transform orientation;
        [SerializeField] private Transform body;

        [Header("Input Settings")]
        [SerializeField] private InputActionReference lookAction;

        private float xRotation = 0f;
        private float yRotation = 0f;
        private float currentX;
        private float currentY;

        private void OnEnable()
        {
            lookAction?.action?.Enable();
        }

        private void OnDisable()
        {
            lookAction?.action?.Disable();
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            currentX = xRotation;
            currentY = yRotation;
        }

        void Update()
        {
            Vector2 lookInput = Vector2.zero;
            if (lookAction?.action != null)
            {
                lookInput = lookAction.action.ReadValue<Vector2>();
            }

            float mouseX = lookInput.x * sensitivity;
            float mouseY = lookInput.y * sensitivity;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -verticalLimit, verticalLimit);

            currentX = Mathf.Lerp(currentX, xRotation, smoothSpeed * Time.deltaTime);
            currentY = Mathf.Lerp(currentY, yRotation, smoothSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Euler(currentX, currentY, 0);
            orientation.rotation = Quaternion.Euler(0, currentY, 0);
            body.rotation = Quaternion.Euler(0, currentX, 0);
        }
    }
}
