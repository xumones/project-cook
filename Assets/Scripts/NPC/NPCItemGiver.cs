using UnityEngine;
using ProjectCook.Dialogue;

namespace ProjectCook.NPC
{
    /// <summary>รับ Action ID "GIVE_ITEM" จากตัวเลือกบทสนทนาแล้วทำการมอบไอเทมให้ผู้เล่น</summary>
    public class NPCItemGiver : MonoBehaviour, IDialogueActionHandler
    {
        private const string GiveItemActionID = "GIVE_ITEM";

        public void HandleAction(string actionId)
        {
            if (actionId != GiveItemActionID) return;

            Debug.Log("create item");
        }
    }
}
