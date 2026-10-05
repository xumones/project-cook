using UnityEngine;
using UnityEngine.Localization;

namespace ProjectCook.Quests
{
    /// <summary>ไปคุยกับผู้พูดคนนี้ นับตอนเริ่มบทสนทนา</summary>
    [CreateAssetMenu(fileName = "Objective_Talk_", menuName = "ProjectCook/Quests/Talk To Objective")]
    public class TalkToObjectiveSO : QuestObjectiveSO
    {
        [Tooltip("รหัสผู้พูด (npcId ใน NPC Profile)")]
        [SerializeField] private string speakerId;

        [Tooltip("ข้อความบนโน้ต เช่น \"ไปคุยกับป้าขายผัก\" ว่าง = แสดงรหัสผู้พูด")]
        [SerializeField] private LocalizedString description;

        public override int OnTalkedTo(string talkedTo) => talkedTo == speakerId ? 1 : 0;

        public override string Describe() =>
            description != null && !description.IsEmpty ? description.GetLocalizedString() : speakerId;
    }
}
