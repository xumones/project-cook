using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;
using ProjectCook.Items;

namespace ProjectCook.Inventory
{
    /// <summary>ร้านขายของ: ของที่ขายแสดงเป็นที่เก็บหนึ่งชุด หยิบออกไป = ซื้อ ของไม่มีวันหมด และไม่รับของจากผู้เล่น</summary>
    [DisallowMultipleComponent]
    public class Shop : MonoBehaviour, IStorageRules
    {
        [Serializable]
        private struct StockEntry
        {
            public ItemDefinitionSO item;

            [Tooltip("ขายหลังปลดล็อกแล้วเท่านั้น (เช่น วัตถุดิบที่ได้จากการรับเควส)")]
            public bool requiresUnlock;
        }

        [Tooltip("ชื่อบนหัวหน้าต่าง")]
        [SerializeField] private string title = "SHOP";

        [Tooltip("ของที่ขาย เรียงตามลำดับที่วางในร้าน ราคามาจากข้อมูลไอเทม")]
        [SerializeField] private List<StockEntry> stock = new List<StockEntry>();

        [SerializeField] private CompartmentLayout[] compartments =
        {
            new CompartmentLayout { size = new Vector2Int(4, 4) }
        };

        private ItemStorage storage;

        private void Awake()
        {
            storage = new ItemStorage(ItemGrid.FromLayouts(compartments), this);
        }

        /// <summary>เติมของตามที่ปลดล็อกล่าสุดแล้วเปิดหน้าต่างร้าน</summary>
        public void Open()
        {
            Restock();
            storage.RequestOpen(title);
        }

        public bool CanRelease(InventoryItem item) =>
            PlayerWallet.Instance != null && PlayerWallet.Instance.CanAfford(PriceOf(item));

        public bool CanAccept(InventoryItem item) => false;

        public int PriceOf(InventoryItem item) => item.Definition.Price;

        public void OnReleased(InventoryItem item)
        {
            PlayerWallet.Instance.TrySpend(PriceOf(item));
            Restock();
        }

        // วางใหม่ทั้งร้าน ช่องที่ถูกซื้อไปจึงกลับมาเต็มที่เดิม
        private void Restock()
        {
            storage.Clear();

            GameProgress progress = GameProgress.Instance;
            foreach (StockEntry entry in stock)
            {
                ItemDefinitionSO item = entry.item;
                if (item == null || (entry.requiresUnlock && !progress.IsItemUnlocked(item.ItemId))) continue;

                if (!storage.TryAdd(new InventoryItem(item, null)))
                {
                    Debug.LogWarning($"[Shop] {name} ที่วางของไม่พอ {item.ItemId} ไม่ได้ขึ้นร้าน", this);
                }
            }
        }
    }
}
