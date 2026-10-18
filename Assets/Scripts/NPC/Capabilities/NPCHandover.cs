using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Cooking;
using ProjectCook.Dialogue;
using ProjectCook.Inventory;
using ProjectCook.Items;
using ProjectCook.Quests;

namespace ProjectCook.NPC.Capabilities
{
    /// <summary>ให้ของ: ผู้เล่นเลือกของใส่กล่องแล้วยืนยัน อาหารในภาชนะไปให้เควสอาหารที่รอ NPC นี้อยู่ ภาชนะกลับไปหาผู้เล่น ของอื่นเป็นของขวัญ</summary>
    // ใส่เฉพาะ NPC ที่เกี่ยวกับเควส ตัวที่แค่พูดในฉากไม่ต้องมี
    [RequireComponent(typeof(HandoverBox))]
    public class NPCHandover : NPCCapability
    {
        [SerializeField] private LocalizedString giveLabel;

        [Tooltip("ขอบคุณเมื่อได้ของที่ไม่เกี่ยวกับเควส ใช้เมื่อ Profile ไม่ได้ตั้ง Gift Thanks ไว้")]
        [SerializeField] private LocalizedString defaultThanks;

        private HandoverBox box;

        // ประโยคที่ NPC จะพูดเมื่อกลับมาคุยหลังหน้าต่างปิด null = ไม่ได้ยืนยัน
        private LocalizedString reply;

        protected override void Awake()
        {
            base.Awake();
            box = GetComponent<HandoverBox>();
        }

        public override void BuildOptions(List<DialogueOption> options)
        {
            options.Add(new DialogueOption { Label = giveLabel, EndsDialogue = true, OnSelected = () => box.Open(Receive, ResumeTalking) });
        }

        private void Receive(List<ItemInstance> items)
        {
            var dishes = new List<IReadOnlyList<IngredientSnapshot>>();
            var food = new List<ItemInstance>();
            foreach (ItemInstance item in items)
            {
                // ภาชนะเป็นของผู้เล่นเสมอ NPC รับไปแค่ของข้างใน
                if (!ServingFoodContainer.TryTakeContents(item, food)) continue;

                PlayerItemTransfer.Instance.GiveBack(item);
                if (food.Count > 0) dishes.Add(Snapshots(food));
            }

            reply = ReplyTo(dishes, items.Count);
        }

        // ได้เกรด F เควสยังค้าง แต่ของที่ให้มาแล้วไม่คืน
        private LocalizedString ReplyTo(List<IReadOnlyList<IngredientSnapshot>> dishes, int itemCount)
        {
            QuestLog log = QuestLog.Instance;
            if (dishes.Count > 0 && log != null && log.TryServe(Npc.Profile.NpcId, dishes, out ActiveQuest quest, out DishScore score))
            {
                return quest.Definition.ResultText(score.Grade);
            }

            if (itemCount == 0) return null;
            return Npc.Lines.TryPick(Npc.Profile.GiftThanks, out LocalizedString thanks) ? thanks : defaultThanks;
        }

        private static List<IngredientSnapshot> Snapshots(List<ItemInstance> food)
        {
            var snapshots = new List<IngredientSnapshot>(food.Count);
            foreach (ItemInstance ingredient in food)
            {
                if (IngredientSnapshot.TryFrom(ingredient, out IngredientSnapshot snapshot)) snapshots.Add(snapshot);
            }
            return snapshots;
        }

        private void ResumeTalking()
        {
            LocalizedString line = reply;
            reply = null;
            if (line != null) DialogueRunner.Instance?.Begin(Npc, line);
        }
    }
}
