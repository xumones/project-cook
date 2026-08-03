using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Dialogue.Conditions
{
    /// <summary>
    /// ScriptableObject สำหรับตรวจสอบเงื่อนไขความก้าวหน้าในเกมจาก GameStateManager
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameFlagCondition", menuName = "Dialogue/Conditions/Game Flag Condition")]
    public class GameFlagConditionSO : DialogConditionSO
    {
        [Tooltip("ชื่อ Flag Key ในเกมที่ต้องการตรวจสอบ (เช่น 'cooked_steak', 'player_gold', 'talked_to_bob')")]
        [SerializeField] private string flagKey;

        [Tooltip("เครื่องมือเปรียบเทียบค่า")]
        [SerializeField] private FlagComparison comparison = FlagComparison.Equal;

        [Tooltip("ค่าเป้าหมายที่ต้องการตรวจสอบ (เช่น 1 สำหรับ true / จำนวนเงิน)")]
        [SerializeField] private int targetValue = 1;

        public override bool IsMet()
        {
            if (GameStateManager.Instance == null)
            {
                Debug.LogWarning("[GameFlagConditionSO] ไม่พบ GameStateManager ใน Scene!");
                return false;
            }

            return GameStateManager.Instance.CheckFlag(flagKey, comparison, targetValue);
        }
    }
}
