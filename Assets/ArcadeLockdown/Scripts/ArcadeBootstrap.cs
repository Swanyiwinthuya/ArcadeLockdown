using UnityEngine;

namespace ArcadeLockdown
{
    /// <summary>
    /// Starts the complete game when the ArcadeLockdown scene loads. The room layout
    /// is generated at runtime and then populated with the packaged CC0 art assets.
    /// </summary>
    public static class ArcadeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneCallback()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Time.timeScale = 1f;
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            StartGame();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "ArcadeLockdown")
                return;

            if (Object.FindAnyObjectByType<ArcadeGameManager>() != null)
                return;

            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;

            GameObject game = new GameObject("ARCADE LOCKDOWN - Game Manager");
            ArcadeGameManager manager = game.AddComponent<ArcadeGameManager>();
            ArcadeWorldBuilder.Build(manager);
            manager.FinishSetup();
        }
    }
}
