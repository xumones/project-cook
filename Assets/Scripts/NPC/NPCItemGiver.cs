using UnityEngine;

namespace ProjectCook.NPC
{
    /// <summary>
    /// รับ Action ID "GIVE_ITEM" จากตัวเลือกบทสนทนาแล้วทำการมอบไอเทมให้ผู้เล่น
    /// (ตอนนี้เป็นแค่ placeholder log ไว้ทดสอบ flow ก่อนใส่ Instantiate จริง)
    /// </summary>
    public class NPCItemGiver : MonoBehaviour, INPCActionHandler
    {
        private const string GiveItemActionID = "GIVE_ITEM";

        public void HandleAction(string actionID)
        {
            if (actionID != GiveItemActionID) return;

            Debug.Log("create item");
        }
    }
}
