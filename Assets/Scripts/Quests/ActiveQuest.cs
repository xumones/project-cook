using System;
using System.Collections.Generic;
using UnityEngine.Localization;

namespace ProjectCook.Quests
{
    /// <summary>เควสหนึ่งรายการที่ผู้เล่นรับอยู่: เนื้อหา ผู้ให้ และความคืบหน้าของแต่ละเป้าหมาย</summary>
    public class ActiveQuest
    {
        private readonly int[] progress;

        /// <summary>รหัสของรายการนี้ ใช้ผูกโน้ตเควสกับเควส</summary>
        public string Id { get; }

        public IQuestDefinition Definition { get; }

        /// <summary>รหัสผู้ให้เควส ว่าง = เริ่มเองจากไอเทมหรือเหตุการณ์</summary>
        public string GiverId { get; }

        /// <summary>ชื่อผู้ให้เควส null ถ้าเริ่มเอง</summary>
        public LocalizedString GiverName { get; }

        public ActiveQuest(IQuestDefinition definition, string giverId, LocalizedString giverName)
            : this(definition, giverId, giverName, Guid.NewGuid().ToString("N"), null)
        {
        }

        /// <summary>สร้างคืนจาก save รหัสเดิมทำให้โน้ตที่จดไว้ยังชี้มาที่เควสนี้ได้</summary>
        public ActiveQuest(IQuestDefinition definition, string giverId, LocalizedString giverName, string id, int[] savedProgress)
        {
            Id = id;
            Definition = definition;
            GiverId = giverId ?? string.Empty;
            GiverName = giverName;
            progress = new int[definition.Objectives.Count];
            if (savedProgress != null) Array.Copy(savedProgress, progress, Math.Min(savedProgress.Length, progress.Length));
        }

        public int[] CopyProgress() => (int[])progress.Clone();

        public bool IsComplete
        {
            get
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    if (progress[i] < Definition.Objectives[i].Required) return false;
                }
                return true;
            }
        }

        public int ProgressOf(int objectiveIndex) => progress[objectiveIndex];

        /// <summary>ถามทุกเป้าหมายที่ยังไม่ครบว่าได้ความคืบหน้าเท่าไหร่ คืน true ถ้ามีข้อไหนเดินหน้า</summary>
        public bool Advance(Func<IQuestObjective, int> gainOf)
        {
            bool changed = false;
            for (int i = 0; i < progress.Length; i++)
            {
                IQuestObjective objective = Definition.Objectives[i];
                if (progress[i] >= objective.Required) continue;

                int gain = gainOf(objective);
                if (gain <= 0) continue;

                progress[i] = Math.Min(objective.Required, progress[i] + gain);
                changed = true;
            }
            return changed;
        }

        /// <summary>เป้าหมายส่งอาหารข้อแรกที่ยังไม่ครบ และผู้รับคือคนนี้</summary>
        public bool TryGetPendingDelivery(string recipientId, out int objectiveIndex)
        {
            for (int i = 0; i < progress.Length; i++)
            {
                if (progress[i] >= Definition.Objectives[i].Required) continue;
                if (!(Definition.Objectives[i] is IDeliveryObjective delivery)) continue;

                string recipient = string.IsNullOrEmpty(delivery.RecipientId) ? GiverId : delivery.RecipientId;
                if (recipient != recipientId) continue;

                objectiveIndex = i;
                return true;
            }

            objectiveIndex = -1;
            return false;
        }

        public void CompleteObjective(int objectiveIndex) =>
            progress[objectiveIndex] = Definition.Objectives[objectiveIndex].Required;

        /// <summary>เป้าหมายทุกข้อ ข้อที่ต้องทำหลายครั้งต่อท้ายด้วยความคืบหน้า</summary>
        public string Describe()
        {
            var lines = new List<string>(progress.Length);
            for (int i = 0; i < progress.Length; i++)
            {
                IQuestObjective objective = Definition.Objectives[i];
                lines.Add(objective.Required > 1
                    ? $"{objective.Describe()} {progress[i]}/{objective.Required}"
                    : objective.Describe());
            }
            return string.Join("\n", lines);
        }
    }
}
