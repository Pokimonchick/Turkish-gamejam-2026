using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneTransition
{
    public const string LoadingSceneName = "Loading";

    private const float FadeDuration = 0.35f;
    private const float PostSceneLoadDelay = 0.05f;
    private const int OverlaySortingOrder = 32767;

    private static TransitionRunner runner;

    public static string TargetSceneKey { get; private set; }
    public static bool IsTransitioning { get; private set; }

    public static void Load(string sceneKey)
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            Debug.LogWarning("Target scene key is empty.");
            return;
        }

        TargetSceneKey = sceneKey;
        Time.timeScale = 1f;

        EnsureRunner().LoadLoadingSceneWithFade();
    }

    public static IEnumerator FadeToBlack()
    {
        yield return EnsureRunner().FadeTo(1f, FadeDuration);
    }

    public static void FadeFromBlackAfterSceneLoad(string sceneKey)
    {
        EnsureRunner().FadeFromBlackAfterSceneLoad(sceneKey);
    }

    private static TransitionRunner EnsureRunner()
    {
        if (runner != null)
        {
            return runner;
        }

        GameObject runnerObject = new GameObject("[Scene Transition]");
        Object.DontDestroyOnLoad(runnerObject);
        runner = runnerObject.AddComponent<TransitionRunner>();
        return runner;
    }

    private sealed class TransitionRunner : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private Coroutine currentRoutine;

        public void LoadLoadingSceneWithFade()
        {
            if (currentRoutine != null)
            {
                StopCoroutine(currentRoutine);
            }

            currentRoutine = StartCoroutine(LoadLoadingSceneRoutine());
        }

        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            EnsureOverlay();
            IsTransitioning = true;
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            if (duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
            }
            else
            {
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                    yield return null;
                }

                canvasGroup.alpha = targetAlpha;
            }

            if (Mathf.Approximately(targetAlpha, 0f))
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
                canvasGroup.gameObject.SetActive(false);
                IsTransitioning = false;
            }
        }

        public void FadeFromBlackAfterSceneLoad(string sceneKey)
        {
            if (currentRoutine != null)
            {
                StopCoroutine(currentRoutine);
            }

            currentRoutine = StartCoroutine(FadeFromBlackAfterSceneLoadRoutine(sceneKey));
        }

        private IEnumerator LoadLoadingSceneRoutine()
        {
            yield return FadeTo(1f, FadeDuration);

            SceneManager.LoadScene(LoadingSceneName);

            yield return null;
            yield return new WaitForSecondsRealtime(PostSceneLoadDelay);
            yield return FadeTo(0f, FadeDuration);
            currentRoutine = null;
        }

        private IEnumerator FadeFromBlackAfterSceneLoadRoutine(string sceneKey)
        {
            EnsureOverlay();
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            IsTransitioning = true;

            while (!IsActiveScene(sceneKey))
            {
                yield return null;
            }

            yield return null;
            yield return new WaitForSecondsRealtime(PostSceneLoadDelay);
            yield return FadeTo(0f, FadeDuration);
            currentRoutine = null;
        }

        private static bool IsActiveScene(string sceneKey)
        {
            if (string.IsNullOrWhiteSpace(sceneKey))
            {
                return true;
            }

            Scene activeScene = SceneManager.GetActiveScene();

            return activeScene.name == sceneKey
                || activeScene.path == sceneKey
                || sceneKey.EndsWith("/" + activeScene.name + ".unity");
        }

        private void EnsureOverlay()
        {
            if (canvasGroup != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("Scene Transition Canvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            canvasObject.AddComponent<GraphicRaycaster>();

            canvasGroup = canvasObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            GameObject imageObject = new GameObject("Fade Overlay");
            imageObject.transform.SetParent(canvasObject.transform, false);

            RectTransform imageRect = imageObject.AddComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            Image image = imageObject.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            canvasObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (runner == this)
            {
                runner = null;
                IsTransitioning = false;
            }
        }
    }
}
