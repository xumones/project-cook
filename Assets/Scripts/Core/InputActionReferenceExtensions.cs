using UnityEngine.InputSystem;

namespace ProjectCook.Core
{
    /// <summary>กติกาเดียวสำหรับอ่าน InputActionReference ที่อาจว่างหรือยังไม่ enable ทุกระบบใช้ชุดนี้แทนเขียนเอง</summary>
    public static class InputActionReferenceExtensions
    {
        public static bool WasPressedThisFrame(this InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            return action != null && action.enabled && action.WasPressedThisFrame();
        }

        public static bool WasReleasedThisFrame(this InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            return action != null && action.enabled && action.WasReleasedThisFrame();
        }

        public static bool IsPressed(this InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            return action != null && action.enabled && action.IsPressed();
        }

        public static T ReadValue<T>(this InputActionReference reference) where T : struct
        {
            InputAction action = reference != null ? reference.action : null;
            return action != null && action.enabled ? action.ReadValue<T>() : default;
        }

        public static void EnableAction(this InputActionReference reference) => reference?.action?.Enable();

        public static void DisableAction(this InputActionReference reference) => reference?.action?.Disable();
    }
}
