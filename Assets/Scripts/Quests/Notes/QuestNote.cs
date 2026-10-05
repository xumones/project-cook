using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ProjectCook.Inventory;
using ProjectCook.Items;

namespace ProjectCook.Quests
{
    /// <summary>โน้ต post-it ของเควส: จำว่าเป็นของเควสไหน เขียนผู้ให้ สิ่งที่ต้องทำ และรางวัล และแปะติดผิวที่มี QuestBoard</summary>
    [RequireComponent(typeof(StorableItem))]
    public class QuestNote : MonoBehaviour, ISavableState, IPlacementReceiver
    {
        private const string QuestKey = "quest_note";

        // ห่างจากผิวกระดานครึ่งความหนากระดาษ บวกนิดหน่อยไม่ให้จมผิว
        private const float StickOffset = 0.003f;

        // โน้ตที่อยู่ในฉากตอนนี้ แทนการค้นทั้งฉากตอนต้องทิ้ง
        private static readonly List<QuestNote> placed = new List<QuestNote>();

        [Tooltip("ข้อความบนหน้ากระดาษ")]
        [SerializeField] private TMP_Text label;

        private string questId;

        // แคชไว้ตอนสมัครฟัง จะได้ถอนได้แม้ QuestLog ถูกทำลายก่อน
        private QuestLog log;

        /// <summary>สร้างโน้ตของเควสนี้เป็นข้อมูล ใช้ใส่ลงที่เก็บโดยไม่ต้องมีวัตถุจริง</summary>
        public static ItemInstance Create(ItemDefinitionSO noteDefinition, ActiveQuest quest)
        {
            var note = new ItemInstance(noteDefinition);
            note.SetState(QuestKey, quest.Id);
            return note;
        }

        /// <summary>ทิ้งโน้ตของเควสนี้ทั้งในตัวผู้เล่นและที่อยู่ในฉาก ที่เก็บอื่น (เช่นตู้เย็น) ไม่แตะ</summary>
        public static void Discard(string questId)
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.Storage.RemoveAll(item => item.Instance.TryGetState(QuestKey, out string id) && id == questId);
            }

            for (int i = placed.Count - 1; i >= 0; i--)
            {
                if (placed[i].questId == questId) Destroy(placed[i].gameObject);
            }
        }

        public string StateKey => QuestKey;

        // จดว่าแปะบอร์ดอยู่ด้วย ไม่งั้นโหลด save แล้วโน้ตบนบอร์ดจะร่วงลงพื้น
        public string CaptureState() => IsStuck ? questId + StuckSuffix : questId;

        public void RestoreState(string state)
        {
            bool stuck = state.EndsWith(StuckSuffix);
            questId = stuck ? state.Substring(0, state.Length - StuckSuffix.Length) : state;
            if (stuck && TryGetComponent(out Rigidbody body)) body.isKinematic = true;
            Refresh();
        }

        private const string StuckSuffix = "|stuck";

        private bool IsStuck => TryGetComponent(out Rigidbody body) && body.isKinematic;

        private void OnEnable()
        {
            placed.Add(this);

            log = QuestLog.Instance;
            if (log == null) return;

            // เควสจบไปตอนพื้นที่นี้ถูกซ่อนอยู่ (เช่น ผู้เล่นอยู่ข้างนอก) โน้ตจึงพลาดการทิ้ง
            if (questId != null && log.Find(questId) == null)
            {
                Destroy(gameObject);
                return;
            }

            log.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            placed.Remove(this);
            if (log != null) log.Changed -= Refresh;
        }

        public void OnPlaced(RaycastHit surface)
        {
            bool onBoard = surface.collider != null && surface.collider.GetComponentInParent<QuestBoard>() != null;

            // โน้ตที่เคยแปะบอร์ดพกสถานะ "ติดอยู่" มาด้วย วางที่อื่นต้องคืนฟิสิกส์ให้ตกตามปกติ
            if (TryGetComponent(out Rigidbody body)) body.isKinematic = onBoard;
            if (!onBoard) return;

            // หันหน้ากระดาษออกจากกระดาน วางชิดผิวให้ติดอยู่ตรงนั้น
            transform.SetPositionAndRotation(surface.point + surface.normal * StickOffset, Quaternion.LookRotation(-surface.normal, Vector3.up));
        }

        // เขียนจากเควสตอนนี้ จึงเปลี่ยนตามความคืบหน้าและภาษาที่เลือกเอง
        private void Refresh()
        {
            ActiveQuest quest = log != null && questId != null ? log.Find(questId) : null;
            if (quest == null)
            {
                label.text = string.Empty;
                return;
            }

            string giver = quest.GiverName != null && !quest.GiverName.IsEmpty ? quest.GiverName.GetLocalizedString() + "\n" : string.Empty;
            label.text = $"{giver}{quest.Describe()}\n+{quest.Definition.Reward}";
        }
    }
}
