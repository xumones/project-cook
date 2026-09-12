using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;
using ProjectCook.Cooking;
using ProjectCook.Dialogue;
using ProjectCook.Items;

namespace ProjectCook.NPC
{
    /// <summary>มอบวัตถุดิบตามเมนูของ NPC ลงจานที่ผู้เล่นถืออยู่ เมื่อเลือกตัวเลือก "GIVE_ITEM:&lt;ingredientId&gt;"</summary>
    public class NPCItemGiver : MonoBehaviour, IDialogueActionHandler, IDialogueStateListener
    {
        [Tooltip("วัตถุดิบที่ NPC ตัวนี้ยอมให้ ขอชนิดที่ไม่อยู่ในนี้จะถูกปฏิเสธ")]
        [SerializeField] private List<IngredientDataSO> menu = new List<IngredientDataSO>();

        private const string GiveItemPrefix = "GIVE_ITEM:";

        /// <summary>Flag ที่บทสนทนาใช้ซ่อนตัวเลือกขอของ</summary>
        public const string HoldingDishWithSpaceFlag = "holding_dish_with_space";

        // "ขอชนิดนี้จาก NPC ตัวนี้ได้" = อยู่ในเมนู และ ปลดล็อกแล้ว
        // ล้างของ NPC ก่อนหน้าทิ้งทุกครั้ง ไม่งั้นเมนูของ NPC#1 จะไปโผล่ตอนคุยกับ NPC#2 เพราะใช้ JSON ร่วมกัน
        private const string CanRequestPrefix = "can_request_";

        public void RefreshDialogueState()
        {
            GameFlagsManager flags = GameFlagsManager.Instance;
            if (flags == null) return;

            bool hasSpace = EquipmentManager.TryGetHeld(out ServingFoodContainer dish) && !dish.IsFull;
            flags.SetFlag(HoldingDishWithSpaceFlag, hasSpace ? 1 : 0);

            flags.ClearWithPrefix(CanRequestPrefix);
            for (int i = 0; i < menu.Count; i++)
            {
                if (menu[i] == null) continue;

                string id = menu[i].IngredientId;
                bool unlocked = flags.GetFlag(NPCQuestGiver.UnlockedFlag(id)) > 0;
                flags.SetFlag(CanRequestPrefix + id, unlocked ? 1 : 0);
            }
        }

        public void HandleAction(string actionId)
        {
            if (string.IsNullOrEmpty(actionId) || !actionId.StartsWith(GiveItemPrefix)) return;

            string ingredientId = actionId.Substring(GiveItemPrefix.Length);
            IngredientDataSO data = FindInMenu(ingredientId);
            if (data == null)
            {
                Debug.LogWarning($"[NPCItemGiver] {name} ไม่มี '{ingredientId}' ในเมนู", this);
                return;
            }

            if (data.Prefab == null)
            {
                Debug.LogWarning($"[NPCItemGiver] '{ingredientId}' ยังไม่ได้ตั้ง Prefab ใน IngredientDataSO", data);
                return;
            }

            if (!EquipmentManager.TryGetHeld(out ServingFoodContainer dish) || dish.IsFull)
            {
                Debug.Log($"[NPCItemGiver] ผู้เล่นไม่ได้ถือจานที่ยังมีที่ว่าง ให้ '{ingredientId}' ไม่ได้");
                return;
            }

            // ยื่นให้จานตรง ๆ แทนปล่อยให้ trigger จับ จะได้นับได้ทันทีก่อนบทสนทนาแสดงตัวเลือกรอบถัดไป
            GameObject item = Instantiate(data.Prefab);
            if (!dish.TryReceive(item))
            {
                Destroy(item);
            }
        }

        private IngredientDataSO FindInMenu(string ingredientId)
        {
            for (int i = 0; i < menu.Count; i++)
            {
                if (menu[i] != null && menu[i].IngredientId == ingredientId) return menu[i];
            }
            return null;
        }
    }
}
