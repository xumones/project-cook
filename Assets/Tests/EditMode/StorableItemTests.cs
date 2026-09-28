using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectCook.Items;

namespace ProjectCook.Tests
{
    public class StorableItemTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in objects)
            {
                Object.DestroyImmediate(go);
            }
            objects.Clear();
        }

        // โครงเดียวกับ prefab หนึ่งตัว: สถานะที่ root หนึ่งตัว และที่ลูกอีกหนึ่งตัว
        private StorableItem CreateItem(out FakeSavableState rootState, out FakeSavableState childState)
        {
            var root = new GameObject("item");
            var child = new GameObject("piece");
            child.transform.SetParent(root.transform);
            objects.Add(root);

            StorableItem storable = root.AddComponent<StorableItem>();
            rootState = root.AddComponent<FakeSavableState>();
            childState = child.AddComponent<FakeSavableState>();
            return storable;
        }

        [Test]
        public void CaptureThenRestore_GivesEachComponentItsOwnStateOnANewObject()
        {
            StorableItem source = CreateItem(out FakeSavableState sourceRoot, out FakeSavableState sourceChild);
            sourceRoot.Value = "root";
            sourceChild.Value = "child";

            List<ComponentState> states = source.CaptureStates();
            StorableItem copy = CreateItem(out FakeSavableState copyRoot, out FakeSavableState copyChild);
            copy.RestoreStates(states);

            Assert.AreEqual(2, states.Count);
            Assert.AreEqual("root", copyRoot.Value);
            Assert.AreEqual("child", copyChild.Value);
        }
    }

    public class FakeSavableState : MonoBehaviour, ISavableState
    {
        public string Value;

        public string CaptureState() => Value;

        public void RestoreState(string state) => Value = state;
    }
}
