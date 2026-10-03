using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>ชื่อ Layer ที่โค้ดอ้างถึง รวมไว้ที่เดียวกันพิมพ์ผิดแล้วยังหาต้นตอเจอ</summary>
    // Layer แบ่งตามบทบาททางฟิสิกส์ ไม่ใช่ชนิดของ (ชนิดดูจาก component) การชนตั้งใน Collision Matrix:
    //   ฉากตายตัว    Default, terrain           ชนผู้เล่น
    //   ของใช้ตายตัว  Interactable               เล็งได้ ชนผู้เล่น (สถานี ตู้เย็น NPC)
    //   ของหลวม      Item, SolidFood, NonSolidFood  เล็งได้ ไม่ชนผู้เล่น (ของที่หยิบ/วางได้)
    //   ผู้เล่น       Player
    //   ของในมือ     HeldItem                   ไม่ชนผู้เล่น ระบบย้ายเข้าออกเอง ห้ามตั้งใน prefab
    public static class GameLayers
    {
        public const string HeldItem = "HeldItem";
        public const string Player = "Player";

        /// <summary>ตัวผู้เล่นกับของในมือ การเล็งและการหาที่วางต้องมองข้ามเสมอ</summary>
        public static int PlayerAndHeldMask => MaskOf(Player) | MaskOf(HeldItem);

        /// <summary>ทุก layer ที่ชนกับ layer นี้ตาม Collision Matrix</summary>
        public static int CollisionMaskOf(int layer)
        {
            int mask = 0;
            for (int other = 0; other < 32; other++)
            {
                if (!Physics.GetIgnoreLayerCollision(layer, other)) mask |= 1 << other;
            }
            return mask;
        }

        private static int MaskOf(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? 1 << layer : 0;
        }
    }

    /// <summary>สถานะ "ถูกถืออยู่" ของวัตถุ: ย้ายทั้ง hierarchy ไป Layer HeldItem และปิดฟิสิกส์ แล้วคืนได้ครบด้วย Restore</summary>
    public readonly struct HeldState
    {
        private readonly Transform[] transforms;
        private readonly int[] layers;
        private readonly Rigidbody body;
        private readonly bool wasKinematic;
        private readonly bool usedGravity;

        private HeldState(Transform[] transforms, int[] layers, Rigidbody body, bool wasKinematic, bool usedGravity)
        {
            this.transforms = transforms;
            this.layers = layers;
            this.body = body;
            this.wasKinematic = wasKinematic;
            this.usedGravity = usedGravity;
        }

        /// <summary>ยึดวัตถุ: body เป็น null ได้ถ้าไม่มีฟิสิกส์ ถ้าไม่พบ Layer จะไม่แตะ layer เลย</summary>
        public static HeldState Capture(Transform root, Rigidbody body)
        {
            bool wasKinematic = body != null && body.isKinematic;
            bool usedGravity = body != null && body.useGravity;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                body.useGravity = false;
            }

            int layer = LayerMask.NameToLayer(GameLayers.HeldItem);
            if (layer < 0)
            {
                Debug.LogWarning($"[HeldState] ไม่พบ Layer '{GameLayers.HeldItem}' {root.name} จะคง Layer เดิมไว้", root);
                return new HeldState(null, null, body, wasKinematic, usedGravity);
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            int[] layers = new int[transforms.Length];
            for (int i = 0; i < transforms.Length; i++)
            {
                layers[i] = transforms[i].gameObject.layer;
                transforms[i].gameObject.layer = layer;
            }

            return new HeldState(transforms, layers, body, wasKinematic, usedGravity);
        }

        public void Restore()
        {
            if (transforms != null)
            {
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i] != null) transforms[i].gameObject.layer = layers[i];
                }
            }

            if (body != null)
            {
                body.isKinematic = wasKinematic;
                body.useGravity = usedGravity;
                if (!wasKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
            }
        }
    }
}
