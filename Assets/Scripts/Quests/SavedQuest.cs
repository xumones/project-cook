using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace ProjectCook.Quests
{
    /// <summary>เควสที่รับอยู่หนึ่งรายการในรูปที่เขียนลงไฟล์ save ได้ เนื้อหาเควสเก็บเป็นชนิด + ข้อมูลให้ผู้สร้างคืน</summary>
    [Serializable]
    public class SavedQuest
    {
        public string id;
        public string kind;
        public string definition;
        public string giverId;
        public string giverTable;
        public string giverEntry;
        public int[] progress;

        public static SavedQuest From(ActiveQuest quest)
        {
            var saved = new SavedQuest
            {
                id = quest.Id,
                kind = quest.Definition.SaveKind,
                definition = quest.Definition.SaveDefinition(),
                giverId = quest.GiverId,
                progress = quest.CopyProgress()
            };

            if (quest.GiverName != null && !quest.GiverName.IsEmpty)
            {
                saved.giverTable = quest.GiverName.TableReference.TableCollectionName;
                TableEntryReference entry = quest.GiverName.TableEntryReference;
                saved.giverEntry = entry.ReferenceType == TableEntryReference.Type.Id ? entry.KeyId.ToString() : entry.Key;
            }
            return saved;
        }

        public ActiveQuest ToQuest(IQuestDefinition resolved) =>
            new ActiveQuest(resolved, giverId, GiverName(), id, progress);

        private LocalizedString GiverName()
        {
            if (string.IsNullOrEmpty(giverTable)) return null;

            TableEntryReference entry = long.TryParse(giverEntry, out long keyId) ? keyId : giverEntry;
            return new LocalizedString(giverTable, entry);
        }
    }
}
