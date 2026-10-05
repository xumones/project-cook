using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ProjectCook.Inventory;
using ProjectCook.Items;
using ProjectCook.NPC.Capabilities;

namespace ProjectCook.NPC
{
    /// <summary>โน้ต post-it ของเควส: จำว่าเป็นของ NPC คนไหน เขียนสิ่งที่ขอบนกระดาษ และแปะติดผิวที่มี QuestBoard</summary>
    [RequireComponent(typeof(StorableItem))]
    public class QuestNote : MonoBehaviour, ISavableState, IPlacementReceiver
    {
        // ห่างจากผิวกระดานครึ่งความหนากระดาษ บวกนิดหน่อยไม่ให้จมผิว
        private const float StickOffset = 0.003f;

        [Tooltip("ข้อความบนหน้ากระดาษ")]
        [SerializeField] private TMP_Text label;

        private const string NpcIdKey = "quest_note";

        private string npcId;

        /// <summary>สร้างโน้ตของ NPC นี้เป็นข้อมูล ใช้ใส่ลงที่เก็บโดยไม่ต้องมีวัตถุจริง</summary>
        public static ItemInstance Create(ItemDefinitionSO noteDefinition, string npcId)
        {
            var note = new ItemInstance(noteDefinition);
            note.SetState(NpcIdKey, npcId);
            return note;
        }

        public string StateKey => NpcIdKey;

        public string CaptureState() => npcId;

        public void RestoreState(string state)
        {
            npcId = state;
            Refresh();
        }

        public void OnPlaced(RaycastHit surface)
        {
            if (surface.collider == null || surface.collider.GetComponentInParent<QuestBoard>() == null) return;

            // หันหน้ากระดาษออกจากกระดาน วางชิดผิว แล้วหยุดฟิสิกส์ให้ติดอยู่ตรงนั้น
            transform.SetPositionAndRotation(surface.point + surface.normal * StickOffset, Quaternion.LookRotation(-surface.normal, Vector3.up));
            if (TryGetComponent(out Rigidbody body)) body.isKinematic = true;
        }

        /// <summary>ทิ้งโน้ตของ NPC นี้ทั้งในกระเป๋าผู้เล่นและที่อยู่ในฉาก ที่อื่น (เช่นตู้เย็น) ไม่แตะ</summary>
        public static void Discard(string npcId)
        {
            if (PlayerInventory.Instance != null)
            {
                ItemStorage storage = PlayerInventory.Instance.Storage;
                var notes = new List<InventoryItem>();
                foreach (ItemGrid grid in storage.Grids)
                {
                    foreach (InventoryItem item in grid.Items)
                    {
                        if (IsNoteOf(item, npcId)) notes.Add(item);
                    }
                }
                foreach (InventoryItem note in notes) storage.Remove(note);
            }

            foreach (QuestNote note in FindObjectsByType<QuestNote>(FindObjectsSortMode.None))
            {
                if (note.npcId == npcId) Destroy(note.gameObject);
            }
        }

        private static bool IsNoteOf(InventoryItem item, string npcId) =>
            item.Instance.TryGetState(NpcIdKey, out string owner) && owner == npcId;

        // เขียนจากเควสที่ NPC ให้อยู่ตอนนี้ จึงเปลี่ยนตามภาษาที่เลือกเอง
        private void Refresh()
        {
            NPCQuestGiver giver = FindGiver(npcId);
            IQuestRequest request = giver != null ? giver.Active : null;
            label.text = request != null
                ? $"{giver.DisplayName.GetLocalizedString()}\n{request.Describe()}\n+{request.Reward}"
                : string.Empty;
        }

        private static NPCQuestGiver FindGiver(string npcId)
        {
            foreach (NPCQuestGiver giver in FindObjectsByType<NPCQuestGiver>(FindObjectsSortMode.None))
            {
                if (giver.NpcId == npcId) return giver;
            }
            return null;
        }
    }
}
