using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>ชื่อ Layer ที่โค้ดอ้างถึง รวมไว้ที่เดียวกันพิมพ์ผิดแล้วยังหาต้นตอเจอ</summary>
    public static class GameLayers
    {
        /// <summary>Layer ชั่วคราวของไอเทมระหว่างอยู่ในมือ ถูกตัดออกจากการเล็งและไม่ชนกับผู้เล่น (ตั้งใน Collision Matrix)</summary>
        public const string HeldItem = "HeldItem";
    }

    /// <summary>จำ Layer เดิมของทั้ง hierarchy ไว้ตอนย้ายไป Layer อื่นชั่วคราว แล้วคืนให้ครบทุกตัว</summary>
    public readonly struct LayerSnapshot
    {
        private readonly Transform[] transforms;
        private readonly int[] layers;

        private LayerSnapshot(Transform[] transforms, int[] layers)
        {
            this.transforms = transforms;
            this.layers = layers;
        }

        /// <summary>ย้ายทั้ง hierarchy ไป Layer ที่ระบุ คืน snapshot ไว้ใช้คืนค่า ถ้าไม่พบ Layer จะไม่แตะอะไรเลย</summary>
        public static LayerSnapshot Apply(Transform root, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"[LayerSnapshot] ไม่พบ Layer '{layerName}' {root.name} จะคง Layer เดิมไว้", root);
                return default;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            int[] layers = new int[transforms.Length];

            for (int i = 0; i < transforms.Length; i++)
            {
                layers[i] = transforms[i].gameObject.layer;
                transforms[i].gameObject.layer = layer;
            }

            return new LayerSnapshot(transforms, layers);
        }

        public void Restore()
        {
            if (transforms == null) return;

            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null)
                {
                    transforms[i].gameObject.layer = layers[i];
                }
            }
        }
    }
}
