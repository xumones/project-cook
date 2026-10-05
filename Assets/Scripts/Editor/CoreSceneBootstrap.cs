using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ProjectCook.Core;

namespace ProjectCook.Editor
{
    /// <summary>กด Play จากฉากไหนก็เริ่มที่ Core เสมอ แล้วให้ LocationLoader เริ่มที่พื้นที่ที่เปิดค้างไว้ใน Editor</summary>
    [InitializeOnLoad]
    internal static class CoreSceneBootstrap
    {
        private const string CoreScenePath = "Assets/Scenes/Core.unity";

        static CoreSceneBootstrap()
        {
            // รอให้ AssetDatabase พร้อมก่อน ไม่งั้นโหลดฉากไม่ขึ้นตอน Editor เพิ่งเปิด
            EditorApplication.delayCall += SetStartScene;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void SetStartScene() =>
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(CoreScenePath);

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;

            // ฉากตอนเล่นโหลดจากไฟล์ ไม่ใช่จากที่เปิดค้างไว้ ถ้าไม่บันทึกก่อน สิ่งที่เพิ่งแก้จะไม่อยู่ในเกม
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorApplication.isPlaying = false;
                return;
            }

            SessionState.SetString(LocationLoader.EditorStartSceneKey, OpenLocationSceneName());
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
