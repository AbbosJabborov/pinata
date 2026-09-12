using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pinata.UI.Menu
{
    public class LoadingScreenController : MonoBehaviour
    {
        public static LoadingScreenController Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressPercentageText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI tipText;
        [SerializeField] private RectTransform spinnerIcon;

        [Header("Timing")]
        [SerializeField] private float minLoadDuration = 1.4f;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Tips")]
        [SerializeField] private string[] tips = new string[]
        {
            "TIP: Smash piñatas with your Bat to break them open and scatter treats!",
            "TIP: Sort candies into matching colored baskets to earn massive bonus payouts!",
            "TIP: Equip your Vacuum tool (Slot 4) to rapidly clean up large candy spills on the floor!",
            "TIP: Visit the Upgrade Kiosk to boost your backpack capacity, bat power, and vacuum suction!",
            "TIP: Fully fill baskets to 15/15 capacity before selling for maximum profits!"
        };

        private Tween _fadeTween;
        private Tween _spinnerTween;
        private bool _isLoading;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        public void LoadScene(string sceneName)
        {
            if (_isLoading) return;
            _isLoading = true;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            // Pick a random tip
            if (tipText != null && tips != null && tips.Length > 0)
            {
                tipText.text = tips[Random.Range(0, tips.Length)];
            }

            if (statusText != null)
            {
                statusText.text = "ENTERING WAREHOUSE...";
            }

            // Spinner animation
            if (spinnerIcon != null)
            {
                _spinnerTween?.Kill();
                _spinnerTween = spinnerIcon.DORotate(new Vector3(0, 0, -360f), 1.5f, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart)
                    .SetUpdate(true);
            }

            // Reset progress
            if (progressBar != null) progressBar.value = 0f;
            if (progressPercentageText != null) progressPercentageText.text = "0%";

            // Fade in loading screen
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                _fadeTween?.Kill();
                yield return canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.InOutQuad).WaitForCompletion();
            }

            // Begin async load
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            float startTime = Time.unscaledTime;
            float displayProgress = 0f;

            while (displayProgress < 1f)
            {
                float elapsedTime = Time.unscaledTime - startTime;
                float timeRatio = Mathf.Clamp01(elapsedTime / minLoadDuration);

                // Unity's asyncLoad stops at 0.9 until allowSceneActivation is true
                float rawProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

                // Smoothly ease displayProgress toward target
                float target = Mathf.Min(rawProgress, (timeRatio >= 1f) ? 1f : 0.95f);
                if (timeRatio >= 1f && rawProgress >= 1f)
                {
                    target = 1f;
                }

                displayProgress = Mathf.MoveTowards(displayProgress, target, Time.unscaledDeltaTime * 1.5f);

                if (progressBar != null)
                {
                    progressBar.value = displayProgress;
                }

                if (progressPercentageText != null)
                {
                    progressPercentageText.text = $"{Mathf.RoundToInt(displayProgress * 100f)}%";
                }

                if (displayProgress >= 1f && timeRatio >= 1f)
                {
                    break;
                }

                yield return null;
            }

            if (statusText != null)
            {
                statusText.text = "READY!";
            }

            yield return new WaitForSecondsRealtime(0.2f);

            // Allow scene activation
            asyncLoad.allowSceneActivation = true;

            // Wait for scene to fully load
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Optional: If DontDestroyOnLoad, fade out. Since it's in MenuScene, GameScene loads cleanly now!
        }

        private void OnDestroy()
        {
            _fadeTween?.Kill();
            _spinnerTween?.Kill();
        }
    }
}
