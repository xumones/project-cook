using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectCook.Cinematics;

namespace ProjectCook.Tests
{
    public class CutsceneQueueTests
    {
        private readonly List<Object> assets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in assets) Object.DestroyImmediate(asset);
            assets.Clear();
        }

        private CutsceneSO Cutscene(string id, bool playOnce)
        {
            var cutscene = ScriptableObject.CreateInstance<CutsceneSO>();
            assets.Add(cutscene);

            var serialized = new SerializedObject(cutscene);
            serialized.FindProperty("cutsceneId").stringValue = id;
            serialized.FindProperty("playOnce").boolValue = playOnce;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return cutscene;
        }

        [Test]
        public void PlayOnce_CannotBeRequestedAgainAfterCompleting()
        {
            var queue = new CutsceneQueue();
            CutsceneSO intro = Cutscene("intro", playOnce: true);

            Assert.IsTrue(queue.Request(intro));
            Assert.IsFalse(queue.Request(intro), "คำขอที่ยังค้างอยู่ต้องไม่ถูกเปิดซ้ำ");

            queue.Complete(intro);
            Assert.IsEmpty(queue.Pending);
            Assert.IsFalse(queue.Request(intro), "เล่นครั้งเดียวที่ดูแล้วต้องขอใหม่ไม่ได้");
        }

        [Test]
        public void Repeatable_CanBeRequestedAgainAfterCompleting()
        {
            var queue = new CutsceneQueue();
            CutsceneSO homecoming = Cutscene("homecoming", playOnce: false);

            queue.Request(homecoming);
            queue.Complete(homecoming);

            Assert.IsTrue(queue.Request(homecoming));
        }

        [Test]
        public void Pending_StaysUntilCompleted_InRequestOrder()
        {
            var queue = new CutsceneQueue();
            CutsceneSO first = Cutscene("first", playOnce: true);
            CutsceneSO second = Cutscene("second", playOnce: true);

            queue.Request(first);
            queue.Request(second);
            CollectionAssert.AreEqual(new[] { "first", "second" }, queue.Pending);

            queue.Complete(first);
            CollectionAssert.AreEqual(new[] { "second" }, queue.Pending);
        }

        [Test]
        public void Trigger_MatchesOnlySameKindAndId()
        {
            CutsceneSO cutscene = Cutscene("after_chef", playOnce: true);
            var serialized = new SerializedObject(cutscene);
            serialized.FindProperty("trigger").enumValueIndex = (int)CutsceneTrigger.QuestCompleted;
            serialized.FindProperty("triggerId").stringValue = "chef_01";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsTrue(cutscene.IsTriggeredBy(CutsceneTrigger.QuestCompleted, "chef_01"));
            Assert.IsFalse(cutscene.IsTriggeredBy(CutsceneTrigger.QuestStarted, "chef_01"), "ชนิดข่าวต่างกันต้องไม่ตรง");
            Assert.IsFalse(cutscene.IsTriggeredBy(CutsceneTrigger.QuestCompleted, "chef_02"));
        }

        [Test]
        public void Trigger_EmptyIdNeverMatches()
        {
            CutsceneSO cutscene = Cutscene("unset", playOnce: true);

            Assert.IsFalse(cutscene.IsTriggeredBy(CutsceneTrigger.WorldEvent, string.Empty),
                "เควสสุ่มส่งรหัสว่าง ต้องไม่ไปกระตุ้น cutscene ที่ยังไม่ได้ตั้งรหัส");
        }

        [Test]
        public void SaveData_RoundTrip_KeepsPendingAndSeen()
        {
            var queue = new CutsceneQueue();
            CutsceneSO seen = Cutscene("seen", playOnce: true);
            CutsceneSO waiting = Cutscene("waiting", playOnce: true);
            queue.Request(seen);
            queue.Complete(seen);
            queue.Request(waiting);

            string json = JsonUtility.ToJson(queue.Capture());
            var restored = new CutsceneQueue();
            restored.Restore(JsonUtility.FromJson<CutsceneQueue.SaveData>(json));

            CollectionAssert.AreEqual(new[] { "waiting" }, restored.Pending);
            Assert.IsFalse(restored.Request(seen), "รายการที่ดูแล้วต้องติดมากับ save");
        }
    }
}
