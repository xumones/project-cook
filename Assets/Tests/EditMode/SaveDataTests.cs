using NUnit.Framework;
using UnityEngine;
using ProjectCook.Inventory;
using ProjectCook.Items;

namespace ProjectCook.Tests
{
    // ใช้ ItemDatabase จริงใน Resources เพราะ save อ้างไอเทมด้วยรหัสผ่านฐานข้อมูลนั้น
    public class SaveDataTests
    {
        private static ItemDefinitionSO Egg => ItemDatabaseSO.Instance.Find("egg_1");

        [Test]
        public void ItemSaveData_RoundTrip_KeepsTypeFlagsAndStates()
        {
            var original = new ItemInstance(Egg);
            original.AddFlag(ItemFlags.FoundOutside);
            original.SetState("cook_times", "{\"omni\":3}");

            string json = JsonUtility.ToJson(ItemSaveData.From(original));
            ItemInstance restored = JsonUtility.FromJson<ItemSaveData>(json).ToInstance();

            Assert.AreSame(Egg, restored.Definition);
            Assert.IsTrue(restored.HasFlag(ItemFlags.FoundOutside));
            Assert.IsTrue(restored.TryGetState("cook_times", out string state));
            Assert.AreEqual("{\"omni\":3}", state);
        }

        [Test]
        public void StorageSaveData_RoundTrip_KeepsPositionAndRotation()
        {
            var layout = new[] { new CompartmentLayout { size = new Vector2Int(4, 2) } };
            var grids = ItemGrid.FromLayouts(layout);
            var item = new InventoryItem(new ItemInstance(Egg));
            Assert.IsTrue(grids[0].TryPlace(item, new Vector2Int(2, 1), true));

            string json = JsonUtility.ToJson(StorageSaveData.From(grids));
            var restored = ItemGrid.FromLayouts(layout);
            JsonUtility.FromJson<StorageSaveData>(json).RestoreInto(restored);

            Assert.AreEqual(1, restored[0].Items.Count);
            Assert.AreEqual(new Vector2Int(2, 1), restored[0].Items[0].Position);
            Assert.IsTrue(restored[0].Items[0].IsRotated);
        }
    }
}
