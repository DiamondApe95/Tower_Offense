using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerConquest.Core
{
    /// <summary>
    /// SceneFlowController: Verwaltet Szenenübergänge und Ladebildschirme.
    /// Singleton, der zwischen Szenen persistent ist.
    /// </summary>
    public class SceneFlowController : MonoBehaviour
    {
        public static SceneFlowController Instance { get; private set; }

        [Header("Scene Names")]
        public string bootScene = "Boot";
        public string mainMenuScene = "MainMenu";
        public string worldMapScene = "WorldMap";
        public string gameplayScene = "LevelGameplay";

        [Header("Transition Settings")]
        public float fadeInDuration = 0.5f;
        public float fadeOutDuration = 0.5f;
        public float minimumLoadTime = 0.5f;

        [Header("Loading Screen")]
        public GameObject loadingScreenPrefab;
        public CanvasGroup fadeOverlay;

        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;
        public event Action<float> OnLoadProgress;

        private bool isLoading;
        private GameObject loadingScreenInstance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadMainMenu()
        {
            LoadSceneAsync(mainMenuScene);
        }

        public void LoadWorldMap()
        {
            LoadSceneAsync(worldMapScene);
        }

        public void LoadGameplay()
        {
            LoadSceneAsync(gameplayScene);
        }

        public void LoadGameplay(string levelId)
        {
            if (ServiceLocator.TryGet(out Saving.SaveManager saveManager))
            {
                var progress = saveManager.GetOrCreateProgress();
                progress.lastSelectedLevelId = levelId;
                saveManager.SaveProgress(progress);
            }

            LoadSceneAsync(gameplayScene);
        }

        public void LoadScene(string sceneName)
        {
            LoadSceneAsync(sceneName);
        }

        public void ReloadCurrentScene()
        {
            LoadSceneAsync(SceneManager.GetActiveScene().name);
        }

        public void LoadSceneAsync(string sceneName, Action onComplete = null)
        {
            if (isLoading)
            {
                Debug.LogWarning($"SceneFlowController: Already loading a scene. Ignoring request for '{sceneName}'.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"SceneFlowController: Scene '{sceneName}' not found in build settings.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName, onComplete));
        }

        private IEnumerator LoadSceneRoutine(string sceneName, Action onComplete)
        {
            isLoading = true;
            OnSceneLoadStarted?.Invoke(sceneName);

            // Fade out
            if (fadeOverlay != null)
            {
                yield return StartCoroutine(FadeRoutine(0f, 1f, fadeOutDuration));
            }

            // Show loading screen
            if (loadingScreenPrefab != null && loadingScreenInstance == null)
            {
                loadingScreenInstance = Instantiate(loadingScreenPrefab);
                DontDestroyOnLoad(loadingScreenInstance);
            }

            float startTime = Time.realtimeSinceStartup;

            // Load scene async
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                OnLoadProgress?.Invoke(asyncLoad.progress);
                yield return null;
            }

            // Ensure minimum load time
            float elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed < minimumLoadTime)
            {
                yield return new WaitForSecondsRealtime(minimumLoadTime - elapsed);
            }

            OnLoadProgress?.Invoke(1f);
            asyncLoad.allowSceneActivation = true;

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Hide loading screen
            if (loadingScreenInstance != null)
            {
                Destroy(loadingScreenInstance);
                loadingScreenInstance = null;
            }

            // Fade in
            if (fadeOverlay != null)
            {
                yield return StartCoroutine(FadeRoutine(1f, 0f, fadeInDuration));
            }

            isLoading = false;
            OnSceneLoadCompleted?.Invoke(sceneName);
            onComplete?.Invoke();
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            if (fadeOverlay == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                fadeOverlay.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            fadeOverlay.alpha = to;
        }

        public string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        public bool IsInMainMenu()
        {
            return GetCurrentSceneName() == mainMenuScene;
        }

        public bool IsInGameplay()
        {
            return GetCurrentSceneName() == gameplayScene;
        }

        public bool IsLoading => isLoading;
    }
}
