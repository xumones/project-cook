using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Cooking;
using ProjectCook.Dialogue;
using ProjectCook.Items;

namespace ProjectCook.NPC.Capabilities
{
    /// <summary>รับของที่ผู้เล่นถือมาส่ง ถ้ามี IDeliveryHandler บนตัว (เช่นเควส) จะให้ตัวนั้นรับช่วง ไม่มีก็รับไว้เฉย ๆ</summary>
    public class NPCReceiver : NPCCapability
    {
        [SerializeField] private LocalizedString deliverLabel;

        private IDeliveryHandler[] handlers;

        protected override void Awake()
        {
            base.Awake();
            handlers = GetComponents<IDeliveryHandler>();
        }

        public override void BuildOptions(List<DialogueOption> options)
        {
            if (!EquipmentManager.TryGetHeld(out ServingFoodContainer dish) || !dish.HasContents) return;

            // คำตอบตัดสินตอนกดเลือก เพราะ handler เป็นคนบอกว่ารับแบบไหน
            var option = new DialogueOption { Label = deliverLabel };
            option.OnSelected = () => option.Response = Deliver(dish);
            options.Add(option);
        }

        private LocalizedString Deliver(ServingFoodContainer dish)
        {
            for (int i = 0; i < handlers.Length; i++)
            {
                if (handlers[i].TryHandleDelivery(dish, out LocalizedString response)) return response;
            }

            dish.ConsumeAll();
            Npc.Profile.Received.TryPick(out LocalizedString line);
            return line;
        }
    }
}
