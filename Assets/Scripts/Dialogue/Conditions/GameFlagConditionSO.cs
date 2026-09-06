using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Dialogue.Conditions
{
    // ทำหน้าที่เป็นแค่ตัวหุ้ม flagKey/comparison/targetValue ให้เลือกได้จาก Inspector
    // ตรรกะการเช็คจริงอยู่ที่ DialogueConditionUtility.IsFlagMet ที่เดียว (ใช้ร่วมกับ DialogueChoice.conditionFlag)
    /// <summary>
    /// ScriptableObject สำหรับตรวจสอบเงื่อนไขความก้าวหน้าในเกมจาก GameStateManager
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameFlagCondition", menuName = "ProjectCook/Dialogue/Conditions/Game Flag Condition")]
    public class GameFlagConditionSO : DialogueConditionSO
    {
        [Tooltip("ชื่อ Flag Key ในเกมที่ต้องการตรวจสอบ (เช่น 'cooked_steak', 'player_gold', 'talked_to_bob')")]
        [SerializeField] private string flagKey;

        [Tooltip("เครื่องมือเปรียบเทียบค่า")]
        [SerializeField] private FlagComparison comparison = FlagComparison.Equal;

        [Tooltip("ค่าเป้าหมายที่ต้องการตรวจสอบ (เช่น 1 สำหรับ true / จำนวนเงิน)")]
        [SerializeField] private int targetValue = 1;

        public override bool IsMet()
        {
            return DialogueConditionUtility.IsFlagMet(flagKey, comparison, targetValue);
        }
    }
}
