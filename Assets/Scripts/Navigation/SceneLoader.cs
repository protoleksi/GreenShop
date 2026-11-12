using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GreenShop
{
    /// <summary>
    /// Загрузка сцен с плавным затемнением. Живёт между сценами (DontDestroyOnLoad).
    /// Размещается на отдельном объекте "SceneLoader" в первой сцене.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [SerializeField] private ScreenFade fade;
        [SerializeField, Min(0f)] private float fadeDuration = 0.35f;

        public event Action<string> SceneLoadStarted;

        public bool IsLoading { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Не удаляем весь объект: на нём могут быть другие полезные компоненты.
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (fade == null)
            {
                fade = ScreenFade.CreateRuntime(transform);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Загрузить сцену по имени (сцена должна быть в Build Settings).</summary>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[SceneLoader] Пустое имя сцены.");
                return;
            }

            if (IsLoading)
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneLoader] Сцена \"{sceneName}\" не добавлена в File > Build Settings > Scenes In Build.");
                return;
            }

            StartCoroutine(LoadRoutine(sceneName, -1));
        }

        /// <summary>Загрузить сцену по индексу из Build Settings.</summary>
        public void LoadScene(int buildIndex)
        {
            if (IsLoading)
            {
                return;
            }

            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"[SceneLoader] Некорректный индекс сцены: {buildIndex}.");
                return;
            }

            StartCoroutine(LoadRoutine(null, buildIndex));
        }

        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        private IEnumerator LoadRoutine(string sceneName, int buildIndex)
        {
            IsLoading = true;
            SceneLoadStarted?.Invoke(sceneName ?? buildIndex.ToString());

            yield return fade.FadeOut(fadeDuration);

            AsyncOperation operation = buildIndex >= 0
                ? SceneManager.LoadSceneAsync(buildIndex)
                : SceneManager.LoadSceneAsync(sceneName);

            if (operation == null)
            {
                Debug.LogError("[SceneLoader] Не удалось начать загрузку сцены.");
                yield return fade.FadeIn(fadeDuration);
                IsLoading = false;
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return fade.FadeIn(fadeDuration);
            IsLoading = false;
        }
    }
}
