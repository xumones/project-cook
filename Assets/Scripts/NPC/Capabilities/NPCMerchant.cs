using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using ProjectCook.Dialogue;
using ProjectCook.Inventory;

namespace ProjectCook.NPC.Capabilities
{
    /// <summary>ขายของ: เพิ่มตัวเลือกเปิดร้านในเมนูคุย ของที่ขายตั้งที่ Shop บน GameObject เดียวกัน</summary>
    [RequireComponent(typeof(Shop))]
    public class NPCMerchant : NPCCapability
    {
        [SerializeField] private LocalizedString browseLabel;

        private Shop shop;

        protected override void Awake()
        {
            base.Awake();
            shop = GetComponent<Shop>();
        }

        public override void BuildOptions(List<DialogueOption> options)
        {
            options.Add(new DialogueOption { Label = browseLabel, EndsDialogue = true, OnSelected = shop.Open });
        }
    }
}
