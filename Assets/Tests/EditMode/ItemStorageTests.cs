using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectCook.Inventory;
using ProjectCook.Items;

namespace ProjectCook.Tests
{
    public class ItemStorageTests
    {
        private readonly List<Object> objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in objects) Object.DestroyImmediate(created);
            objects.Clear();
        }

        // prefab ที่มี StorableItem = เก็บได้ ไม่มี = ถือได้อย่างเดียว (เช่นจาน)
        private InventoryItem CreateItem(bool storable)
        {
            var prefab = new GameObject("prefab");
            prefab.AddComponent<Item>();
            if (storable) prefab.AddComponent<StorableItem>();

            var definition = ScriptableObject.CreateInstance<ItemDefinitionSO>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("prefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            objects.Add(prefab);
            objects.Add(definition);
            return new InventoryItem(new ItemInstance(definition));
        }

        private static ItemStorage CreateStorage(IStorageRules rules = null) =>
            new ItemStorage(new[] { new ItemGrid(new Vector2Int(4, 4)) }, rules);

        [Test]
        public void FromHand_PlainStorage_TakesOnlyStorableItems()
        {
            ItemStorage fridge = CreateStorage();

            Assert.IsTrue(fridge.CanAcceptFromHand(CreateItem(storable: true)));
            Assert.IsFalse(fridge.CanAcceptFromHand(CreateItem(storable: false)));
        }

        [Test]
        public void FromHand_StorageThatAcceptsHeldItems_TakesUnstorableItems()
        {
            ItemStorage box = CreateStorage(new FakeRules { AcceptsHeldItems = true });

            Assert.IsTrue(box.CanAcceptFromHand(CreateItem(storable: false)));
        }

        [Test]
        public void FromHand_StorageThatRefusesEverything_TakesNothing()
        {
            ItemStorage shop = CreateStorage(new FakeRules { Accepts = false, AcceptsHeldItems = true });

            Assert.IsFalse(shop.CanAcceptFromHand(CreateItem(storable: true)));
            Assert.IsFalse(shop.CanAcceptFromHand(CreateItem(storable: false)));
        }

        [Test]
        public void TryPlace_PutsTheItemAtThatCellAndAnnouncesTheChange()
        {
            ItemStorage storage = CreateStorage();
            InventoryItem item = CreateItem(storable: true);
            bool changed = false;
            storage.Changed += () => changed = true;

            Assert.IsTrue(storage.TryPlace(item, storage.Grids[0], new Vector2Int(2, 3), false));
            Assert.AreSame(item, storage.Grids[0].GetItemAt(new Vector2Int(2, 3)));
            Assert.IsTrue(changed);
        }

        [Test]
        public void TryPlace_IntoAGridOfAnotherStorage_IsRejected()
        {
            ItemStorage storage = CreateStorage();
            ItemStorage other = CreateStorage();

            Assert.IsFalse(storage.TryPlace(CreateItem(storable: true), other.Grids[0], Vector2Int.zero, false));
        }

        private class FakeRules : IStorageRules
        {
            public bool Accepts = true;

            public bool AcceptsHeldItems { get; set; }

            public bool CanRelease(InventoryItem item) => true;

            public bool CanAccept(InventoryItem item) => Accepts;

            public int PriceOf(InventoryItem item) => 0;

            public void OnReleased(InventoryItem item)
            {
            }
        }
    }
}
