using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectCook.Inventory;
using ProjectCook.Items;

namespace ProjectCook.Tests
{
    public class ItemGridTests
    {
        private readonly List<ItemDefinitionSO> definitions = new List<ItemDefinitionSO>();

        [TearDown]
        public void TearDown()
        {
            foreach (ItemDefinitionSO definition in definitions)
            {
                Object.DestroyImmediate(definition);
            }
            definitions.Clear();
        }

        private InventoryItem CreateItem(int width, int height)
        {
            var definition = ScriptableObject.CreateInstance<ItemDefinitionSO>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("gridSize").vector2IntValue = new Vector2Int(width, height);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            definitions.Add(definition);
            return new InventoryItem(definition, System.Array.Empty<ComponentState>());
        }

        [Test]
        public void Place_InEmptyGrid_OccupiesOnlyItsCells()
        {
            var grid = new ItemGrid(new Vector2Int(4, 2));
            InventoryItem item = CreateItem(2, 1);

            Assert.IsTrue(grid.TryPlace(item, new Vector2Int(1, 0), false));
            Assert.AreSame(item, grid.GetItemAt(new Vector2Int(1, 0)));
            Assert.AreSame(item, grid.GetItemAt(new Vector2Int(2, 0)));
            Assert.IsNull(grid.GetItemAt(new Vector2Int(0, 0)));
            Assert.IsNull(grid.GetItemAt(new Vector2Int(3, 0)));
            Assert.IsNull(grid.GetItemAt(new Vector2Int(1, 1)));
        }

        [Test]
        public void Place_Overlapping_IsRejected()
        {
            var grid = new ItemGrid(new Vector2Int(4, 2));
            InventoryItem big = CreateItem(2, 2);
            InventoryItem small = CreateItem(1, 1);

            Assert.IsTrue(grid.TryPlace(big, Vector2Int.zero, false));
            Assert.IsFalse(grid.TryPlace(small, new Vector2Int(1, 1), false));
            Assert.IsTrue(grid.TryPlace(small, new Vector2Int(2, 0), false));
        }

        [Test]
        public void Place_OutOfBounds_IsRejected()
        {
            var grid = new ItemGrid(new Vector2Int(4, 2));
            InventoryItem item = CreateItem(2, 1);

            Assert.IsFalse(grid.TryPlace(item, new Vector2Int(3, 0), false));
            Assert.IsFalse(grid.TryPlace(item, new Vector2Int(-1, 0), false));
            Assert.IsFalse(grid.TryPlace(item, new Vector2Int(0, 2), false));
            Assert.AreEqual(0, grid.Items.Count);
        }

        [Test]
        public void Place_Rotated_SwapsWidthAndHeight()
        {
            var grid = new ItemGrid(new Vector2Int(4, 2));
            InventoryItem tall = CreateItem(1, 3);

            Assert.IsFalse(grid.TryPlace(tall, Vector2Int.zero, false));
            Assert.IsTrue(grid.TryPlace(tall, Vector2Int.zero, true));
            Assert.AreEqual(new Vector2Int(3, 1), tall.Size);
            Assert.AreSame(tall, grid.GetItemAt(new Vector2Int(2, 0)));
            Assert.IsNull(grid.GetItemAt(new Vector2Int(0, 1)));
        }

        [Test]
        public void Place_SameItemAgain_MovesItWithoutCollidingWithItself()
        {
            var grid = new ItemGrid(new Vector2Int(4, 2));
            InventoryItem item = CreateItem(2, 1);
            grid.TryPlace(item, Vector2Int.zero, false);

            Assert.IsTrue(grid.TryPlace(item, new Vector2Int(1, 0), false));
            Assert.IsNull(grid.GetItemAt(new Vector2Int(0, 0)));
            Assert.AreSame(item, grid.GetItemAt(new Vector2Int(2, 0)));
            Assert.AreEqual(1, grid.Items.Count);
        }

        [Test]
        public void Add_FillsFreeSpaceLeftToRightThenTopToBottom()
        {
            var grid = new ItemGrid(new Vector2Int(4, 2));
            InventoryItem first = CreateItem(2, 1);
            InventoryItem second = CreateItem(2, 1);
            InventoryItem third = CreateItem(1, 1);

            Assert.IsTrue(grid.TryAdd(first));
            Assert.IsTrue(grid.TryAdd(second));
            Assert.IsTrue(grid.TryAdd(third));
            Assert.AreEqual(new Vector2Int(0, 0), first.Position);
            Assert.AreEqual(new Vector2Int(2, 0), second.Position);
            Assert.AreEqual(new Vector2Int(0, 1), third.Position);
        }

        [Test]
        public void Add_RotatesWhenOnlyTheRotatedShapeFits()
        {
            var grid = new ItemGrid(new Vector2Int(4, 2));
            InventoryItem tall = CreateItem(1, 3);

            Assert.IsTrue(grid.TryAdd(tall));
            Assert.IsTrue(tall.IsRotated);
            Assert.AreEqual(Vector2Int.zero, tall.Position);
        }

        [Test]
        public void Add_WhenFull_ReturnsFalse()
        {
            var grid = new ItemGrid(new Vector2Int(2, 2));

            Assert.IsTrue(grid.TryAdd(CreateItem(2, 2)));
            Assert.IsFalse(grid.TryAdd(CreateItem(1, 1)));
            Assert.AreEqual(1, grid.Items.Count);
        }

        [Test]
        public void Remove_FreesItsCells()
        {
            var grid = new ItemGrid(new Vector2Int(2, 2));
            InventoryItem first = CreateItem(2, 2);
            grid.TryAdd(first);

            Assert.IsTrue(grid.Remove(first));
            Assert.IsNull(grid.GetItemAt(Vector2Int.zero));
            Assert.IsTrue(grid.TryAdd(CreateItem(2, 2)));
        }
    }
}
