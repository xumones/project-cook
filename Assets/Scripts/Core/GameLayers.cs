using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>ชื่อ Layer ที่โค้ดอ้างถึง รวมไว้ที่เดียวกันพิมพ์ผิดแล้วยังหาต้นตอเจอ</summary>
    public static class GameLayers
    {
        /// <summary>Layer ชั่วคราวของไอเทมระหว่างอยู่ในมือ ถูกตัดออกจากการเล็งและไม่ชนกับผู้เล่น (ตั้งใน Collision Matrix)</summary>
        public const string HeldItem = "HeldItem";
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
