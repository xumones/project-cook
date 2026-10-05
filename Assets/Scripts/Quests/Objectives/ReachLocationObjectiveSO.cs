using UnityEngine;
using UnityEngine.Localization;

namespace ProjectCook.Quests
{
    /// <summary>ไปถึงจุดที่มี WorldEventTrigger รหัสนี้</summary>
    [CreateAssetMenu(fileName = "Objective_Reach_", menuName = "ProjectCook/Quests/Reach Location Objective")]
    public class ReachLocationObjectiveSO : QuestObjectiveSO
    {
        [Tooltip("รหัสเหตุการณ์ของ WorldEventTrigger ที่ต้องไปถึง")]
        [SerializeField] private string eventId;

        [Tooltip("ข้อความบนโน้ต เช่น \"ไปดูต้นไม้เก่า\" ว่าง = แสดงรหัสเหตุการณ์")]
        [SerializeField] private LocalizedString description;

        public override int OnWorldEvent(string triggered) => triggered == eventId ? 1 : 0;

        public override string Describe() =>
            description != null && !description.IsEmpty ? description.GetLocalizedString() : eventId;
    }
}
