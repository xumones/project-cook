using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Core;
using ProjectCook.Cooking;
using ProjectCook.Dialogue;
using ProjectCook.Inventory;
using ProjectCook.Items;
using ProjectCook.Quests;

namespace ProjectCook.NPC.Capabilities
{
    /// <summary>ทางเข้าเควสในบทสนทนา: ให้เควสทีละอันต่อ NPC (เนื้อเรื่องตามลำดับก่อน หมดแล้วสุ่มจากวัตถุดิบ) และรับจานที่ผู้เล่นถือมาส่ง</summary>
    // ความคืบหน้า การจบเควส และรางวัลเป็นของ QuestLog ตัวนี้แค่แปลงเป็นตัวเลือกในเมนูคุย
    public class NPCQuestGiver : NPCCapability
    {
        [Tooltip("เควสเนื้อเรื่องตามลำดับ ทำครั้งเดียว ตัวแรกที่ยังไม่จบคือเควสที่เสนอ")]
        [SerializeField] private List<QuestSO> storyQuests = new List<QuestSO>();

        [Tooltip("วัตถุดิบที่ NPC นี้สั่งได้ในเควสสุ่มหลังเนื้อเรื่องจบ ว่าง = ไม่มีเควสสุ่ม")]
        [SerializeField] private List<IngredientDataSO> orderIngredients = new List<IngredientDataSO>();

        [Header("Quest Note")]
        [Tooltip("ข้อมูลไอเทมโน้ต post-it ที่ให้ผู้เล่นตอนรับเควส")]
        [SerializeField] private ItemDefinitionSO noteDefinition;

        [Tooltip("ตอบเมื่อกระเป๋าเต็มจนรับโน้ตไม่ได้ เควสจะยังไม่ถูกรับ")]
        [SerializeField] private LocalizedString bagFullText;

        [Header("Labels")]
        [SerializeField] private LocalizedString askLabel;
        [SerializeField] private LocalizedString reminderLabel;
        [SerializeField] private LocalizedString deliverLabel;

        private readonly List<IngredientSnapshot> dishBuffer = new List<IngredientSnapshot>();

        private string NpcId => Npc.Profile.NpcId;

        // แคชไว้ตอนลงทะเบียน จะได้ถอนได้แม้ QuestLog ถูกทำลายก่อน
        private QuestLog log;

        // เควสสุ่มสร้างจากประโยคของ NPC ตัวนี้ จึงต้องให้ NPC เป็นคนสร้างคืนจาก save
        private void Start()
        {
            log = QuestLog.Instance;
            log?.RegisterResolver(ResolveOrder);
        }

        private void OnDestroy() => log?.UnregisterResolver(ResolveOrder);

        private IQuestDefinition ResolveOrder(SavedQuest saved) =>
            saved.kind == IngredientOrder.Kind && saved.giverId == NpcId
                ? IngredientOrder.FromSave(saved.definition, Npc.Profile, Npc.Lines)
                : null;

        public override void BuildOptions(List<DialogueOption> options)
        {
            QuestLog log = QuestLog.Instance;
            if (log == null) return;

            ActiveQuest given = log.FindByGiver(NpcId);
            if (given == null)
            {
                IQuestDefinition next = NextRequest(log);
                if (next != null)
                {
                    var ask = new DialogueOption { Label = askLabel, Shot = DialogueShot.CloseUp };
                    ask.OnSelected = () => ask.Response = Accept(log, next);
                    options.Add(ask);
                }
            }
            else
            {
                options.Add(new DialogueOption { Label = reminderLabel, Response = given.Definition.ReminderText });
            }

            // ส่งได้ทั้งเควสของตัวเองและเควสของคนอื่นที่ระบุให้มาส่งที่นี่
            if (log.TryFindDelivery(NpcId, out ActiveQuest delivery)
                && EquipmentManager.TryGetHeld(out ServingFoodContainer dish) && dish.HasContents)
            {
                // คำตอบขึ้นกับผลประเมิน จึงตั้งตอนกดเลือก
                var option = new DialogueOption { Label = deliverLabel };
                option.OnSelected = () => option.Response = Deliver(log, delivery, dish);
                options.Add(option);
            }
        }

        private IQuestDefinition NextRequest(QuestLog log)
        {
            foreach (QuestSO quest in storyQuests)
            {
                if (quest != null && log.CanStart(quest)) return quest;
            }

            // ขอได้เฉพาะเควสที่เงินตอนนี้พอซื้อวัตถุดิบ
            return PlayerWallet.Instance != null
                ? IngredientOrder.TryCreate(orderIngredients, PlayerWallet.Instance.Money, Npc.Profile, Npc.Lines)
                : null;
        }

        // รับเควสได้เมื่อใส่โน้ตลงกระเป๋าได้เท่านั้น
        private LocalizedString Accept(QuestLog log, IQuestDefinition request)
        {
            var quest = new ActiveQuest(request, NpcId, Npc.Profile.DisplayName);
            if (!PlayerInventory.Instance.Storage.TryAdd(new InventoryItem(QuestNote.Create(noteDefinition, quest)))) return bagFullText;

            log.Begin(quest);
            return request.OfferText;
        }

        // ไม่ผ่าน = จานยังอยู่กับผู้เล่น แก้แล้วส่งใหม่ได้
        private LocalizedString Deliver(QuestLog log, ActiveQuest quest, ServingFoodContainer dish)
        {
            dish.GetSnapshot(dishBuffer);
            QualityGrade grade = log.Deliver(quest, NpcId, dishBuffer).Grade;
            if (grade != QualityGrade.F) dish.ConsumeAll();

            return quest.Definition.ResultText(grade);
        }
    }
}
