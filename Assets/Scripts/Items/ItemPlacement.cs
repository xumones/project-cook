using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Items
{
    /// <summary>หาท่าวางไอเทมลงฉากโดยไม่ทับสิ่งอื่น: เลื่อนกล่องขนาดเท่าไอเทมจากต้นเส้นเล็งออกไป แล้วหยุดก่อนชน</summary>
    public static class ItemPlacement
    {
        // เว้นห่างจากผิวที่ชนนิดหนึ่ง ไม่ให้ฟิสิกส์นับว่าทับกันตั้งแต่วาง
        private const float Skin = 0.01f;

        // ไม่ชนอะไรเลย = ลอยไว้ไม่ไกลนักแล้วให้ตกเอง
        private const float MissFraction = 0.6f;

        /// <summary>ท่าวาง item ตามเส้นเล็ง ไกลสุด maxDistance พร้อมผิวที่หยุดชน (collider เป็น null ถ้าไม่ชนอะไร) false = ไม่มีที่ว่าง</summary>
        public static bool TryFindPose(GameObject item, Ray aim, float maxDistance, out Vector3 position, out Quaternion rotation, out RaycastHit surface)
        {
            rotation = FacingRotation(aim.direction);
            Bounds bounds = MeasureBounds(item);

            // ตัวผู้เล่นกับของในมือไม่ใช่สิ่งกีดขวาง ไอเทมที่วางไม่ชนกับมันอยู่แล้ว (ดู GameLayers)
            int mask = ~GameLayers.PlayerAndHeldMask;

            // BoxCast มองไม่เห็นสิ่งที่ทับกล่องอยู่แต่แรก (เช่นยืนหน้าชิดผนัง) จึงต้องเช็คแยก
            if (Physics.CheckBox(aim.origin, bounds.extents, rotation, mask, QueryTriggerInteraction.Ignore))
            {
                position = default;
                surface = default;
                return false;
            }

            float travel = Physics.BoxCast(aim.origin, bounds.extents, aim.direction, out surface, rotation, maxDistance, mask, QueryTriggerInteraction.Ignore)
                ? Mathf.Max(surface.distance - Skin, 0f)
                : maxDistance * MissFraction;
            Vector3 boxCenter = aim.GetPoint(travel);
            position = boxCenter - rotation * bounds.center;
            return true;
        }

        // หันตามผู้เล่นแค่แนวนอน ไม่ให้ของเอียงตามมุมก้มเงย
        private static Quaternion FacingRotation(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : Quaternion.identity;
        }

        /// <summary>กล่องที่ครอบ collider ทั้งหมดของไอเทม วัดรอบ pivot แบบยังไม่หมุน ใช้ได้ทั้ง prefab และวัตถุในฉาก</summary>
        private static Bounds MeasureBounds(GameObject item)
        {
            // ตัดตำแหน่งกับการหมุนของไอเทมออก เหลือแค่สเกล ผลจึงเท่ากันไม่ว่าไอเทมอยู่ตรงไหน
            Matrix4x4 toPivot = Matrix4x4.Scale(item.transform.lossyScale) * item.transform.worldToLocalMatrix;

            var bounds = new Bounds();
            bool measured = false;
            foreach (Collider collider in item.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider.isTrigger || !TryGetShapeBounds(collider, out Bounds shape)) continue;

                Matrix4x4 toItem = toPivot * collider.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                    Vector3 point = toItem.MultiplyPoint3x4(shape.center + Vector3.Scale(shape.extents, sign));

                    if (measured)
                    {
                        bounds.Encapsulate(point);
                    }
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        measured = true;
                    }
                }
            }
            return bounds;
        }

        // รูปทรงในพิกัดของ collider เอง
        private static bool TryGetShapeBounds(Collider collider, out Bounds shape)
        {
            switch (collider)
            {
                case BoxCollider box:
                    shape = new Bounds(box.center, box.size);
                    return true;
                case SphereCollider sphere:
                    shape = new Bounds(sphere.center, Vector3.one * sphere.radius * 2f);
                    return true;
                case CapsuleCollider capsule:
                    Vector3 size = Vector3.one * capsule.radius * 2f;
                    size[capsule.direction] = Mathf.Max(capsule.height, capsule.radius * 2f);
                    shape = new Bounds(capsule.center, size);
                    return true;
                case MeshCollider mesh when mesh.sharedMesh != null:
                    shape = mesh.sharedMesh.bounds;
                    return true;
                default:
                    shape = default;
                    return false;
            }
        }
    }
}
