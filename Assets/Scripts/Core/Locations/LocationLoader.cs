using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectCook.Core
{
    /// <summary>ย้ายผู้เล่นระหว่างพื้นที่: ซ่อนหรือปลดฉากเดิม โหลดฉากปลายทางซ้อนกับ Core ตั้งเป็นฉากหลัก แล้ววางผู้เล่นที่จุดเกิด</summary>
    // ฉากหลักเป็นที่อยู่ของวัตถุที่สร้างใหม่และเป็นที่มาของแสงกับท้องฟ้า จึงต้องเป็นฉากพื้นที่เสมอ ไม่ใช่ Core
    public class LocationLoader : SceneSingleton<LocationLoader>
    {
        /// <summary>Editor จดชื่อฉากพื้นที่ที่เปิดค้างไว้ก่อนกด Play ไว้ที่คีย์นี้ เพื่อเริ่มเกมที่พื้นที่นั้น</summary>
        public const string EditorStartSceneKey = "ProjectCook.EditorStartScene";

        [SerializeField] private LocationSO startLocation;

        [Tooltip("พื้นที่ทั้งหมด ใช้หาพื้นที่จากชื่อฉาก")]
        [SerializeField] private LocationSO[] locations;

        [SerializeField] private DevSettingsSO devSettings;

        [Tooltip("ตัวผู้เล่น (root ที่มี CharacterController)")]
        [SerializeField] private Transform player;

        [SerializeField] private ScreenFader fader;

        // root ที่เปิดอยู่ตอนซ่อนฉาก จะได้เปิดคืนเฉพาะตัวเดิม
        private readonly Dictionary<string, List<GameObject>> hiddenRoots = new Dictionary<string, List<GameObject>>();

        private bool isTraveling;

        public LocationSO Current { get; private set; }

        /// <summary>ผู้เล่นถึงพื้นที่ใหม่และพื้นที่พร้อมเล่นแล้ว</summary>
        public event System.Action<LocationSO> Arrived;

        // พื้นที่แรกไม่ผ่าน CanTravelTo: เปิดฉากทดสอบค้างไว้แล้วกด Play ต้องเริ่มที่นั่นได้เสมอ
        private void Start()
        {
            LocationSO start = ResolveStartLocation();
            UnloadOtherOpenLocations(start);
            StartCoroutine(Travel(start, null));
        }

        // กด Play ใน Editor ฉากที่เปิดค้างไว้ถูกโหลดมาทั้งหมด เหลือไว้แค่พื้นที่เริ่มต้นกับ Core
        private void UnloadOtherOpenLocations(LocationSO start)
        {
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene == gameObject.scene || scene.name == start.SceneName || !scene.isLoaded) continue;

                SceneManager.UnloadSceneAsync(scene);
            }
        }

        public bool CanTravelTo(LocationSO destination) =>
            destination != null
            && (!destination.DevOnly || (devSettings != null && devSettings.DevLocationsEnabled))
            && Application.CanStreamedLevelBeLoaded(destination.SceneName);

        /// <summary>ฉากนี้เป็นพื้นที่ข้างนอกบ้านไหม ใช้ตัดสินที่มาของไอเทมที่เกิดในฉากนั้น</summary>
        public bool IsOutside(string sceneName)
        {
            LocationSO location = FindLocation(sceneName);
            return location != null && location.IsOutside;
        }

        public void TravelTo(LocationSO destination, string spawnId)
        {
            if (isTraveling || destination == Current || !CanTravelTo(destination)) return;

            StartCoroutine(Travel(destination, spawnId));
        }

        private IEnumerator Travel(LocationSO destination, string spawnId)
        {
            isTraveling = true;
            GameModeManager.Instance.Enter(this, GameMode.Loading);
            yield return fader.FadeOut();

            if (Current != null) yield return Leave(Current);

            if (hiddenRoots.TryGetValue(destination.SceneName, out List<GameObject> roots))
            {
                foreach (GameObject root in roots)
                {
                    if (root != null) root.SetActive(true);
                }
                hiddenRoots.Remove(destination.SceneName);
            }
            else if (!SceneManager.GetSceneByName(destination.SceneName).isLoaded)
            {
                yield return SceneManager.LoadSceneAsync(destination.SceneName, LoadSceneMode.Additive);
            }

            Scene scene = SceneManager.GetSceneByName(destination.SceneName);
            SceneManager.SetActiveScene(scene);
            Current = destination;
            PlacePlayer(scene, spawnId);

            // รอให้กล้องกับของในฉากใหม่เข้าที่ก่อนเปิดจอ
            yield return null;
            yield return null;

            GameModeManager.Instance.Exit(this);
            Arrived?.Invoke(destination);

            // ผู้เล่นขยับได้ระหว่างจอสว่าง แต่ยังเดินทางซ้อนไม่ได้จนจอสว่างเต็ม
            yield return fader.FadeIn();
            isTraveling = false;
        }

        private IEnumerator Leave(LocationSO location)
        {
            Scene scene = SceneManager.GetSceneByName(location.SceneName);
            if (!location.KeepLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
                yield break;
            }

            var shown = new List<GameObject>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (!root.activeSelf) continue;

                root.SetActive(false);
                shown.Add(root);
            }
            hiddenRoots[location.SceneName] = shown;
        }

        private void PlacePlayer(Scene scene, string spawnId)
        {
            LocationSpawn spawn = FindSpawn(scene, spawnId ?? string.Empty);
            if (spawn == null)
            {
                Debug.LogWarning($"[LocationLoader] ไม่พบจุดเกิด '{spawnId}' ใน {scene.name} ผู้เล่นอยู่ตำแหน่งเดิม", this);
                return;
            }

            PlacePlayerAt(spawn.transform.position);
        }

        /// <summary>ย้ายตัวผู้เล่นไปตำแหน่งนี้ทันที</summary>
        public void PlacePlayerAt(Vector3 position)
        {
            // CharacterController เขียนตำแหน่งเดิมทับ ต้องปิดก่อนย้าย
            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.position = position;
            if (controller != null) controller.enabled = true;
        }

        private static LocationSpawn FindSpawn(Scene scene, string spawnId)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (LocationSpawn spawn in root.GetComponentsInChildren<LocationSpawn>())
                {
                    if (spawn.SpawnId == spawnId) return spawn;
                }
            }
            return null;
        }

        private LocationSO ResolveStartLocation()
        {
#if UNITY_EDITOR
            LocationSO opened = FindLocation(UnityEditor.SessionState.GetString(EditorStartSceneKey, string.Empty));
            if (opened != null) return opened;
#endif
            return startLocation;
        }

        private LocationSO FindLocation(string sceneName)
        {
            foreach (LocationSO location in locations)
            {
                if (location != null && location.SceneName == sceneName) return location;
            }
            return null;
        }
    }
}
