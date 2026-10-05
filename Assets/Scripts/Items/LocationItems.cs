using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectCook.Core;

namespace ProjectCook.Items
{
    /// <summary>save ไอเทมทุกชิ้นที่วางอยู่ในพื้นที่นี้ ทั้งที่วางไว้ตั้งแต่แรกและที่ผู้เล่นทิ้งไว้ ใส่หนึ่งตัวในฉากพื้นที่ที่ save ได้ (เช่น บ้าน)</summary>
    // ไอเทมที่วางไว้ตั้งแต่แรกต้องมี PersistentId ไม่งั้นแยกไม่ออกจากของที่ผู้เล่นทิ้งไว้ แล้วจะถูกสร้างซ้ำตอนโหลด
    public class LocationItems : MonoBehaviour, ISaveSection
    {
        [Serializable]
        private class PlacedItem
        {
            public string persistentId;
            public ItemSaveData item;
            public Vector3 position;
            public Quaternion rotation;
        }

        [Serializable]
        private class SaveData
        {
            public List<PlacedItem> items = new List<PlacedItem>();
        }

        private void Awake()
        {
            foreach (Item item in ItemsInScene())
            {
                if (!item.TryGetComponent(out PersistentId _))
                {
                    Debug.LogError($"[LocationItems] {item.name} วางไว้ในฉากแต่ไม่มี PersistentId จะถูกสร้างซ้ำตอนโหลด save", item);
                }
            }
        }

        private void Start() => SaveService.Instance?.Register(this);

        private void OnDestroy() => SaveService.Instance?.Unregister(this);

        public string SectionKey => "items:" + gameObject.scene.name;

        public string Capture()
        {
            var data = new SaveData();
            foreach (Item item in ItemsInScene())
            {
                if (item.Location != ItemLocation.World) continue;

                data.items.Add(new PlacedItem
                {
                    persistentId = item.TryGetComponent(out PersistentId id) ? id.Id : string.Empty,
                    item = ItemSaveData.From(item.Capture()),
                    position = item.transform.position,
                    rotation = item.transform.rotation
                });
            }
            return JsonUtility.ToJson(data);
        }

        public void Restore(string json)
        {
            var authored = new Dictionary<string, Item>();
            foreach (Item item in ItemsInScene())
            {
                if (item.TryGetComponent(out PersistentId id)) authored[id.Id] = item;
            }

            foreach (PlacedItem placed in JsonUtility.FromJson<SaveData>(json).items)
            {
                ItemInstance instance = placed.item.ToInstance();
                if (instance == null) continue;

                if (!string.IsNullOrEmpty(placed.persistentId) && authored.TryGetValue(placed.persistentId, out Item existing))
                {
                    authored.Remove(placed.persistentId);
                    existing.transform.SetPositionAndRotation(placed.position, placed.rotation);
                    existing.Restore(instance);
                    continue;
                }

                Item spawned = Item.Spawn(instance, placed.position, placed.rotation);
                if (spawned != null) SceneManager.MoveGameObjectToScene(spawned.gameObject, gameObject.scene);
            }

            // ของที่วางไว้ตั้งแต่แรกแต่ไม่อยู่ใน save = ผู้เล่นเอาไปแล้ว
            foreach (Item taken in authored.Values)
            {
                Destroy(taken.gameObject);
            }
        }

        private List<Item> ItemsInScene()
        {
            var items = new List<Item>();
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                items.AddRange(root.GetComponentsInChildren<Item>(true));
            }
            return items;
        }
    }
}
