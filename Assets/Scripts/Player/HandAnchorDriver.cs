using UnityEngine;
using Unity.Cinemachine;

namespace ProjectCook.Player
{
    /// <summary>ขับตำแหน่งและการหมุนของจุดยึดมือ (Hand Anchor) ให้ตามการเล็งของกล้องผู้เล่น</summary>
    [DisallowMultipleComponent]
    public class HandAnchorDriver : MonoBehaviour
    {
        [Header("Look Source")]
        [Tooltip("PanTilt ของกล้องผู้เล่น (ตัวเดียวกับที่รับ mouse look) — ลากจาก MainCinemachineCamera")]
        [SerializeField] private CinemachinePanTilt panTilt;

        [Tooltip("ปริภูมิอ้างอิงของมุมเล็ง ปกติคือ Transform แม่ของกล้อง เว้นว่าง = อนุมานจาก panTilt / World")]
        [SerializeField] private Transform referenceSpace;

        [Header("Pivot & Offset")]
        [Tooltip("จุดหมุนของมือ ควรเป็นจุดบนลำตัวที่ไม่ไหวตาม Head Bob เว้นว่าง = ใช้ Transform แม่")]
        [SerializeField] private Transform pivot;

        [Tooltip("ตำแหน่งมือเทียบจุดหมุน (x = ขวา, y = สูง, z = หน้า) ก่อนหมุนตาม yaw")]
        [SerializeField] private Vector3 localOffset = new Vector3(0.25f, 1.2f, 0.45f);

        [Header("Follow Tuning")]
        [Tooltip("สัดส่วนการก้ม-เงยตามกล้อง (1 = ตามเต็ม, 0 = ไม่ก้มตาม)")]
        [Range(0f, 1f)]
        [SerializeField] private float pitchFactor = 0.7f;

        [Tooltip("ความหนืดการตามตำแหน่ง (สูง = ตามติดแทบไม่หน่วง) แนะนำสูงไว้ตอนเดินสำรวจ")]
        [SerializeField] private float positionDamping = 25f;

        [Tooltip("ความหนืดการตามการหมุน (สูง = ตามติดแทบไม่หน่วง)")]
        [SerializeField] private float rotationDamping = 25f;

        private void Awake()
        {
            if (pivot == null)
            {
                pivot = transform.parent;
            }

            if (referenceSpace == null && panTilt != null)
            {
                // CinemachinePanTilt ReferenceFrame = Default + กล้องมี parent => มุมอ้างอิงกับ parent ของกล้อง
                referenceSpace = panTilt.transform.parent;
            }
        }

        private void OnEnable()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (!TryComputeTarget(out Vector3 targetPos, out Quaternion targetRot))
            {
                return;
            }

            // Damping แบบไม่ขึ้นกับ frame rate
            float posT = 1f - Mathf.Exp(-positionDamping * Time.deltaTime);
            float rotT = 1f - Mathf.Exp(-rotationDamping * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, targetPos, posT);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotT);
        }

        /// <summary>ตั้งมือไปที่เป้าหมายทันทีโดยไม่หน่วง</summary>
        private void SnapToTarget()
        {
            if (!TryComputeTarget(out Vector3 targetPos, out Quaternion targetRot))
            {
                return;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;
        }

        private bool TryComputeTarget(out Vector3 targetPos, out Quaternion targetRot)
        {
            targetPos = transform.position;
            targetRot = transform.rotation;

            if (panTilt == null || pivot == null)
            {
                return false;
            }

            float yaw = panTilt.PanAxis.Value;
            float pitch = panTilt.TiltAxis.Value * pitchFactor;

            Quaternion baseRot = referenceSpace != null ? referenceSpace.rotation : Quaternion.identity;

            // ตำแหน่ง: หมุน offset ตาม yaw อย่างเดียว (ไม่เอา pitch) มือจึงอยู่ระดับเดิมเสมอ
            Quaternion yawRot = baseRot * Quaternion.Euler(0f, yaw, 0f);
            targetPos = pivot.position + yawRot * localOffset;

            // การหมุน: ตาม yaw เต็ม + pitch ตามสัดส่วน
            targetRot = baseRot * Quaternion.Euler(pitch, yaw, 0f);
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Transform p = pivot != null ? pivot : transform.parent;
            if (p == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(p.position, 0.03f);
            Gizmos.DrawLine(p.position, transform.position);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.06f);
        }
    }
}
