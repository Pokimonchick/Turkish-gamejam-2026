using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LoadingController : MonoBehaviour
{
    [Header("Loading")]
    [SerializeField] private float minimumLoadingTime = 1f;

    [Header("Spinner")]
    [SerializeField] private RectTransform spinner;
    [SerializeField] private float spinnerSpeed = 240f;

    private void Start()
    {
        StartCoroutine(LoadTargetScene());
    }

    private void Update()
    {
        if (spinner != null)
        {
            spinner.Rotate(0f, 0f, -spinnerSpeed * Time.unscaledDeltaTime);
        }
    }

    private IEnumerator LoadTargetScene()
    {
        string targetSceneKey = SceneTransition.TargetSceneKey;

        if (string.IsNullOrWhiteSpace(targetSceneKey))
        {
            Debug.LogError("No target scene key was set before opening Loading scene.");
            yield break;
        }

        AsyncOperation loadingOperation = SceneManager.LoadSceneAsync(targetSceneKey);
        loadingOperation.allowSceneActivation = false;

        float timer = 0f;

        while (loadingOperation.progress < 0.9f || timer < minimumLoadingTime)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        loadingOperation.allowSceneActivation = true;
    }
}