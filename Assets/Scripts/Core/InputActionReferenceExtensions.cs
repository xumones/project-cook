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

        /// <summary>ปุ่มที่มี interaction (เช่น Hold) ทำงานครบในเฟรมนี้</summary>
        public static bool WasPerformedThisFrame(this InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            return action != null && action.enabled && action.WasPerformedThisFrame();
        }

        /// <summary>ความคืบหน้าของการกดค้าง 0–1 ของปุ่มที่ใช้ Hold</summary>
        public static float HoldProgress(this InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            return action != null && action.enabled ? action.GetTimeoutCompletionPercentage() : 0f;
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
    }
}
