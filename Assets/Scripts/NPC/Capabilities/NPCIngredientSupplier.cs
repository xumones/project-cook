using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Core;
using ProjectCook.Cooking;
using ProjectCook.Dialogue;
using ProjectCook.Inventory;

namespace ProjectCook.NPC.Capabilities
{
    /// <summary>ให้วัตถุดิบในเมนูที่ปลดล็อกแล้ว ส่งเข้าที่เก็บของที่กำหนด (ตู้เย็น)</summary>
    public class NPCIngredientSupplier : NPCCapability
    {
        [Tooltip("วัตถุดิบที่ NPC ตัวนี้ยอมให้")]
        [SerializeField] private List<IngredientDataSO> menu = new List<IngredientDataSO>();

        [Tooltip("ที่เก็บของที่วัตถุดิบถูกส่งไป")]
        [SerializeField] private StorageContainer deliverTo;

        [Tooltip("Smart String ที่รับชื่อวัตถุดิบเป็น {0}")]
        [SerializeField] private LocalizedString requestLabel;

        [Tooltip("คำตอบเมื่อที่เก็บเต็ม")]
        [SerializeField] private LocalizedString fullResponse;

        [Tooltip("คำตอบเมื่อเก็บเข้าที่เก็บแล้ว Smart String ที่รับชื่อวัตถุดิบเป็น {0}")]
        [SerializeField] private LocalizedString storedResponse;

        public override void BuildOptions(List<DialogueOption> options)
        {
            if (deliverTo == null) return;

            GameProgress progress = GameProgress.Instance;
            for (int i = 0; i < menu.Count; i++)
            {
                IngredientDataSO data = menu[i];
                if (data == null || !progress.IsIngredientUnlocked(data.ItemId)) continue;

                // คำตอบตัดสินตอนกดเลือก เพราะต้องรู้ก่อนว่าที่เก็บรับได้ไหม
                var option = new DialogueOption { Label = WithName(requestLabel, data) };
                option.OnSelected = () => option.Response = Give(data);
                options.Add(option);
            }
        }

        // สร้าง LocalizedString ใหม่ทุกครั้ง เพราะ argument ของ Smart String ผูกกับ instance
        private static LocalizedString WithName(LocalizedString template, IngredientDataSO data)
        {
            var text = new LocalizedString(template.TableReference, template.TableEntryReference);
            text.Arguments = new object[] { data.DisplayName.GetLocalizedString() };
            return text;
        }

        private LocalizedString Give(IngredientDataSO data)
        {
            if (!deliverTo.Storage.TryAdd(new InventoryItem(data, null))) return fullResponse;

            return WithName(storedResponse, data);
        }
    }
}
