using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>หัวเตาหนึ่งหัว ส่งระดับไฟจากลูกบิดไปเป็นความร้อนของภาชนะที่วางอยู่</summary>
    public class StoveBurner : MonoBehaviour
    {
        [SerializeField] private StoveKnob knob;

        [Tooltip("ภาชนะที่วางอยู่บนหัวเตานี้ ว่างได้")]
        [SerializeField] private HeatingFoodContainer occupant;

        private void Update()
        {
            if (knob == null || occupant == null) return;

            occupant.HeatInput = knob.NormalizedLevel;
        }
    }
}
