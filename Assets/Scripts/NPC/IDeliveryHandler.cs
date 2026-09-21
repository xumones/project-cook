using UnityEngine.Localization;
using ProjectCook.Cooking;

namespace ProjectCook.NPC
{
    /// <summary>รับช่วงการส่งของจาก NPCReceiver เช่น เควสรับจานไปประเมินแทนการรับเฉย ๆ</summary>
    public interface IDeliveryHandler
    {
        /// <summary>คืน true ถ้ารับไปแล้ว พร้อมคำตอบของ NPC</summary>
        bool TryHandleDelivery(ServingFoodContainer dish, out LocalizedString response);
    }
}
