using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectCook.Core
{
    /// <summary>ไฟล์ save ก้อนเดียวของเกม: รวมส่วนที่แต่ละระบบลงทะเบียนไว้ save อัตโนมัติเฉพาะตอนอยู่ในพื้นที่ที่ไม่ใช่ข้างนอก</summary>
    // ตอนพัฒนา save ปิดเป็นค่าเริ่มต้น กด Play แล้วเริ่มใหม่ทุกครั้ง ปุ่ม Dev/ToggleSave เปิดปิดและจำค่าข้ามการกด Play เกมจริง save เสมอ
    public class SaveService : SceneSingleton<SaveService>
    {
        private const int Version = 1;
        private const string DevEnabledKey = "ProjectCook.DevSaveEnabled";

        [Serializable]
        private class SaveFile
        {
            public int version;
            public List<Section> sections = new List<Section>();
        }

        [Serializable]
        private class Section
        {
            public string key;
            public string data;
        }

        [Tooltip("ปุ่มเปิดปิดการ save ตอนพัฒนา (Dev/ToggleSave)")]
        [SerializeField] private InputActionReference toggleAction;

        [Tooltip("save อัตโนมัติทุกกี่วินาทีระหว่างอยู่ในพื้นที่ที่ save ได้")]
        [Min(5f)]
        [SerializeField] private float autosaveInterval = 30f;

        private readonly Dictionary<string, ISaveSection> sections = new Dictionary<string, ISaveSection>();

        // ข้อมูลที่อ่านจากไฟล์แต่เจ้าของยังไม่ลงทะเบียน (เช่น ของในบ้านก่อนบ้านโหลดเสร็จ) และต้องเขียนกลับแม้ยังไม่มีใครรับ
        private readonly Dictionary<string, string> unclaimed = new Dictionary<string, string>();

        private LocationLoader loader;
        private float nextAutosave;

        /// <summary>เปิดการ save อยู่ไหม ปิด = ไม่อ่านไม่เขียนไฟล์</summary>
        public bool IsEnabled { get; private set; }

        public event Action EnabledChanged;

        private static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            IsEnabled = !Debug.isDebugBuild || PlayerPrefs.GetInt(DevEnabledKey, 0) == 1;
            if (IsEnabled) Load();
        }

        private void Start()
        {
            loader = LocationLoader.Instance;
            if (loader != null) loader.Arrived += HandleArrived;
            nextAutosave = Time.unscaledTime + autosaveInterval;
        }

        protected override void OnDestroy()
        {
            if (loader != null) loader.Arrived -= HandleArrived;
            base.OnDestroy();
        }

        /// <summary>ลงทะเบียนส่วนของระบบหนึ่ง ถ้าไฟล์มีข้อมูลของส่วนนี้จะคืนให้ทันที</summary>
        public void Register(ISaveSection section)
        {
            sections[section.SectionKey] = section;
            if (unclaimed.TryGetValue(section.SectionKey, out string data))
            {
                unclaimed.Remove(section.SectionKey);
                section.Restore(data);
            }
        }

        public void Unregister(ISaveSection section)
        {
            if (sections.TryGetValue(section.SectionKey, out ISaveSection registered) && registered == section)
            {
                sections.Remove(section.SectionKey);
            }
        }

        private void Update()
        {
            if (Debug.isDebugBuild && toggleAction != null && toggleAction.action.WasPressedThisFrame()) ToggleDevSave();

            if (IsEnabled && CanSaveHere && Time.unscaledTime >= nextAutosave) Save();
        }

        private void OnApplicationQuit()
        {
            if (IsEnabled && CanSaveHere) Save();
        }

        // ข้างนอกไม่ save ตาย/ออกเกมกลางทาง = กลับไปที่ save ล่าสุดในบ้าน
        private bool CanSaveHere =>
            loader != null && loader.Current != null && !loader.Current.IsOutside
            && GameModeManager.Instance != null && GameModeManager.Instance.CurrentMode != GameMode.Loading;

        private void HandleArrived(LocationSO location)
        {
            if (IsEnabled && !location.IsOutside) Save();
        }

        public void Save()
        {
            var file = new SaveFile { version = Version };
            foreach (KeyValuePair<string, ISaveSection> section in sections)
            {
                file.sections.Add(new Section { key = section.Key, data = section.Value.Capture() });
            }
            foreach (KeyValuePair<string, string> data in unclaimed)
            {
                file.sections.Add(new Section { key = data.Key, data = data.Value });
            }

            // เขียนลงไฟล์ชั่วคราวก่อน ไฟล์จริงจะไม่เสียถ้าเกมดับกลางการเขียน
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(file));
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(temp, FilePath);

            nextAutosave = Time.unscaledTime + autosaveInterval;
        }

        private void Load()
        {
            if (!File.Exists(FilePath)) return;

            SaveFile file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(FilePath));
            if (file == null || file.version != Version)
            {
                Debug.LogWarning($"[SaveService] ไฟล์ save เวอร์ชัน {file?.version} ไม่ตรงกับ {Version} เริ่มเกมใหม่แทน", this);
                return;
            }

            foreach (Section section in file.sections)
            {
                unclaimed[section.key] = section.data;
            }
        }

        // เปิด = เริ่มบันทึกตั้งแต่ตอนนี้ ไม่โหลด save เก่าที่อาจเข้ากับโค้ดใหม่ไม่ได้
        private void ToggleDevSave()
        {
            IsEnabled = !IsEnabled;
            PlayerPrefs.SetInt(DevEnabledKey, IsEnabled ? 1 : 0);
            PlayerPrefs.Save();

            if (IsEnabled)
            {
                unclaimed.Clear();
                if (File.Exists(FilePath)) File.Delete(FilePath);
                if (CanSaveHere) Save();
            }

            Debug.Log($"[SaveService] Dev save {(IsEnabled ? "เปิด" : "ปิด")} ({FilePath})", this);
            EnabledChanged?.Invoke();
        }
    }
}
