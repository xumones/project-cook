using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ProjectCook.Core;

namespace ProjectCook.Editor
{
    /// <summary>กด Play จากฉากพื้นที่ไหนก็ได้: เปิด Core เพิ่มให้ก่อนเข้า Play แล้วปิดคืนตอนออก เล่นจากฉากในหน่วยความจำจึงไม่ต้องบันทึกก่อน</summary>
    [InitializeOnLoad]
    internal static class CoreSceneBootstrap
    {
        private const string CoreScenePath = "Assets/Scenes/Core.unity";
        private const string AddedCoreKey = "ProjectCook.BootstrapAddedCore";

        static CoreSceneBootstrap()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode) PrepareCore();
            else if (change == PlayModeStateChange.EnteredEditMode) RemoveAddedCore();
        }

        private static void PrepareCore()
        {
            // ค่าค้างจากวิธีเดิมที่บังคับเริ่มจากไฟล์ Core
            EditorSceneManager.playModeStartScene = null;

            // เริ่มจากเมนูให้เดินตามเกมจริง: ปุ่ม Play โหลด Core เอง และเริ่มที่พื้นที่แรก
            if (SceneManager.GetSceneByName(SceneFlow.MainMenuScene).isLoaded)
            {
                SessionState.SetString(LocationLoader.EditorStartSceneKey, string.Empty);
                SessionState.SetBool(AddedCoreKey, false);
                return;
            }

            SessionState.SetString(LocationLoader.EditorStartSceneKey, OpenLocationSceneName());

            bool added = !SceneManager.GetSceneByPath(CoreScenePath).isLoaded;
            if (added) EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Additive);
            SessionState.SetBool(AddedCoreKey, added);
        }

        private static void RemoveAddedCore()
        {
            if (!SessionState.GetBool(AddedCoreKey, false)) return;

            SessionState.SetBool(AddedCoreKey, false);
            Scene core = SceneManager.GetSceneByPath(CoreScenePath);
            if (core.isLoaded && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(core, true);
        }

        private static string OpenLocationSceneName()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.path != CoreScenePath) return scene.name;
            }
            return string.Empty;
        }
    }
}
