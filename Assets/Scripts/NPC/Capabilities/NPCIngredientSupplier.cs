using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Core;
using ProjectCook.Cooking;
using ProjectCook.Dialogue;
using ProjectCook.Items;

namespace ProjectCook.NPC.Capabilities
{
    /// <summary>ให้วัตถุดิบในเมนูที่ปลดล็อกแล้ว ลงจานที่ผู้เล่นถืออยู่</summary>
    public class NPCIngredientSupplier : NPCCapability
    {
        [Tooltip("วัตถุดิบที่ NPC ตัวนี้ยอมให้")]
        [SerializeField] private List<IngredientDataSO> menu = new List<IngredientDataSO>();

        [Tooltip("Smart String ที่รับชื่อวัตถุดิบเป็น {0}")]
        [SerializeField] private LocalizedString requestLabel;

        public override void BuildOptions(List<DialogueOption> options)
        {
            if (!EquipmentManager.TryGetHeld(out ServingFoodContainer dish) || dish.IsFull) return;

            GameProgress progress = GameProgress.Instance;
            for (int i = 0; i < menu.Count; i++)
            {
                IngredientDataSO data = menu[i];
                if (data == null || !progress.IsIngredientUnlocked(data.ItemId)) continue;

                Npc.Profile.Thanks.TryPick(out LocalizedString thanks);
                options.Add(new DialogueOption
                {
                    Label = LabelFor(data),
                    Response = thanks,
                    OnSelected = () => Give(data)
                });
            }
        }

        // สร้าง LocalizedString ใหม่ต่อรายการ เพราะ argument ของ Smart String ผูกกับ instance
        private LocalizedString LabelFor(IngredientDataSO data)
        {
            var label = new LocalizedString(requestLabel.TableReference, requestLabel.TableEntryReference);
            label.Arguments = new object[] { data.DisplayName.GetLocalizedString() };
            return label;
        }

        private void Give(IngredientDataSO data)
        {
            if (data.Prefab == null)
            {
                Debug.LogWarning($"[NPCIngredientSupplier] '{data.ItemId}' ยังไม่ได้ตั้ง Prefab", data);
                return;
            }

            if (!EquipmentManager.TryGetHeld(out ServingFoodContainer dish) || dish.IsFull) return;

            // ยื่นให้จานตรง ๆ แทนปล่อยให้ trigger จับ จะได้นับได้ทันทีก่อนเมนูรอบถัดไป
            GameObject item = Instantiate(data.Prefab);
            if (!dish.TryReceive(item))
            {
                Destroy(item);
            }
        }
    }
}
