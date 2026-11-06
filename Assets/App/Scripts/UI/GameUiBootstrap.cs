using UnityEngine;
using UnityEngine.SceneManagement;

namespace App.UI
{
    public static class GameUiBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
        }
        static void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Title" && scene.name != "Main") return;
            if (Object.FindFirstObjectByType<GameSessionUI>() != null) return;
            new GameObject("FPSshogi UI", typeof(RectTransform)).AddComponent<GameSessionUI>();
        }
    }
}
