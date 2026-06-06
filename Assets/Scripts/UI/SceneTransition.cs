using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransition
{
    public const string LoadingSceneName = "Loading";

    public static string TargetSceneKey { get; private set; }

    public static void Load(string sceneKey)
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            Debug.LogWarning("Target scene key is empty.");
            return;
        }

        TargetSceneKey = sceneKey;

        // Если переход был из паузы, возвращаем нормальное время
        Time.timeScale = 1f;

        SceneManager.LoadScene(LoadingSceneName);
    }
}