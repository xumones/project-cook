using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectCook.Items;

namespace ProjectCook.Tests
{
    public class ItemStateTests
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

        // โครงเดียวกับ prefab หนึ่งตัว: สถานะที่ root หนึ่งตัว และที่ลูกอีกหนึ่งตัว คนละรหัส
        private Item CreateItem(out FakeSavableState rootState, out OtherFakeSavableState childState)
        {
            var root = new GameObject("item");
            var child = new GameObject("piece");
            child.transform.SetParent(root.transform);
            objects.Add(root);

            Item item = root.AddComponent<Item>();
            rootState = root.AddComponent<FakeSavableState>();
            childState = child.AddComponent<OtherFakeSavableState>();
            return item;
        }

        [Test]
        public void CaptureThenRestore_GivesEachComponentItsOwnStateOnANewObject()
        {
            Item source = CreateItem(out FakeSavableState sourceRoot, out OtherFakeSavableState sourceChild);
            sourceRoot.Value = "root";
            sourceChild.Value = "child";

            ItemInstance instance = source.Capture();
            Item copy = CreateItem(out FakeSavableState copyRoot, out OtherFakeSavableState copyChild);
            copy.Restore(instance);

            Assert.AreEqual("root", copyRoot.Value);
            Assert.AreEqual("child", copyChild.Value);
        }

        [Test]
        public void Restore_KeepsTheSameIdentityAndFlags()
        {
            Item source = CreateItem(out _, out _);
            source.Instance.AddFlag(ItemFlags.FoundOutside);

            ItemInstance instance = source.Capture();
            Item copy = CreateItem(out _, out _);
            copy.Restore(instance);

            Assert.AreSame(instance, copy.Instance);
            Assert.IsTrue(copy.Instance.HasFlag(ItemFlags.FoundOutside));
        }

        [Test]
        public void Restore_LeavesComponentsWithoutSavedStateUntouched()
        {
            var instance = new ItemInstance(null);
            instance.SetState("other", "x");

            Item item = CreateItem(out FakeSavableState rootState, out _);
            rootState.Value = "default";
            item.Restore(instance);

            Assert.AreEqual("default", rootState.Value);
        }
    }

    public class FakeSavableState : MonoBehaviour, ISavableState
    {
        public string Value;

        public string StateKey => "fake";

        public string CaptureState() => Value;

        public void RestoreState(string state) => Value = state;
    }

    public class OtherFakeSavableState : MonoBehaviour, ISavableState
    {
        public string Value;

        public string StateKey => "other_fake";

        public string CaptureState() => Value;

        public void RestoreState(string state) => Value = state;
    }
}
