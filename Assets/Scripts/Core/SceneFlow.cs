using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectCook.Core
{
    /// <summary>ทางเข้าออกของเกม: เริ่มเกมจากเมนู และปิดเกม</summary>
    public static class SceneFlow
    {
        public const string MainMenuScene = "MainMenu";
        private const string CoreScene = "Core";

        /// <summary>ปิดทุกฉากแล้วเปิด Core ซึ่งพาผู้เล่นไปพื้นที่แรกและโหลด save เอง</summary>
        public static AsyncOperation StartGame() => SceneManager.LoadSceneAsync(CoreScene, LoadSceneMode.Single);

        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
