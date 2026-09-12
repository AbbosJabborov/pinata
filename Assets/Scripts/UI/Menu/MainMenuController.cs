using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pinata.UI.Menu
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Transition Target")]
        [SerializeField] private string targetSceneName = "GameScene";

        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Button Animators")]
        [SerializeField] private MenuButtonAnimator newGameAnimator;
        [SerializeField] private MenuButtonAnimator continueAnimator;
        [SerializeField] private MenuButtonAnimator settingsAnimator;
        [SerializeField] private MenuButtonAnimator quitAnimator;

        [Header("Modals & Panels")]
        [SerializeField] private SettingsModal settingsModal;
        [SerializeField] private LoadingScreenController loadingScreen;
        [SerializeField] private CanvasGroup screenFader;
        [SerializeField] private TextMeshProUGUI notificationToast;

        [Header("Menu Elements for Entrance Animation")]
        [SerializeField] private RectTransform titleTransform;
        [SerializeField] private CanvasGroup titleCanvasGroup;
        [SerializeField] private RectTransform buttonsContainer;
        [SerializeField] private CanvasGroup buttonsCanvasGroup;

        private bool _isTransitioning;
        private Tween _toastTween;

        private void Awake()
        {
            // Set up Screen Fader
            if (screenFader != null)
            {
                screenFader.alpha = 1f;
                screenFader.blocksRaycasts = true;
            }

            // Hook button listeners
            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
                // Style continue as disabled/coming soon for now as requested
                if (continueAnimator != null)
                {
                    continueAnimator.SetInteractable(false);
                }
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }

            if (notificationToast != null)
            {
                notificationToast.alpha = 0f;
            }
        }

        private void Start()
        {
            PlayEntranceAnimation();
        }

        private void PlayEntranceAnimation()
        {
            // Fade in from screen fader
            if (screenFader != null)
            {
                screenFader.DOFade(0f, 0.7f).SetEase(Ease.OutQuad).OnComplete(() =>
                {
                    screenFader.blocksRaycasts = false;
                });
            }

            // Animate Title
            if (titleTransform != null && titleCanvasGroup != null)
            {
                titleCanvasGroup.alpha = 0f;
                Vector2 originalTitlePos = titleTransform.anchoredPosition;
                titleTransform.anchoredPosition = originalTitlePos + new Vector2(0f, 60f);

                titleCanvasGroup.DOFade(1f, 0.8f).SetEase(Ease.OutQuad);
                titleTransform.DOAnchorPos(originalTitlePos, 0.8f).SetEase(Ease.OutBack, 1.3f);
            }

            // Animate Button Column
            if (buttonsContainer != null && buttonsCanvasGroup != null)
            {
                buttonsCanvasGroup.alpha = 0f;
                Vector2 originalButtonsPos = buttonsContainer.anchoredPosition;
                buttonsContainer.anchoredPosition = originalButtonsPos + new Vector2(-50f, 0f);

                buttonsCanvasGroup.DOFade(1f, 0.9f).SetDelay(0.15f).SetEase(Ease.OutQuad);
                buttonsContainer.DOAnchorPos(originalButtonsPos, 0.9f).SetDelay(0.15f).SetEase(Ease.OutBack, 1.2f);
            }
        }

        private void OnNewGameClicked()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            // Tactile punch scale
            if (newGameButton != null)
            {
                newGameButton.transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 10, 1);
            }

            if (loadingScreen != null)
            {
                loadingScreen.LoadScene(targetSceneName);
            }
            else
            {
                StartCoroutine(TransitionToGameRoutine());
            }
        }

        private IEnumerator TransitionToGameRoutine()
        {
            // Block further clicks
            if (screenFader != null)
            {
                screenFader.blocksRaycasts = true;
                yield return screenFader.DOFade(1f, 0.5f).SetEase(Ease.InOutQuad).WaitForCompletion();
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
            }

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
            while (asyncLoad != null && !asyncLoad.isDone)
            {
                yield return null;
            }
        }

        private void OnContinueClicked()
        {
            if (_isTransitioning) return;

            // Continue is not linked yet as per request
            ShowToast("Save game feature coming soon!");
            if (continueButton != null)
            {
                continueButton.transform.DOShakePosition(0.25f, new Vector3(8f, 0f, 0f), 15);
            }
        }

        private void OnSettingsClicked()
        {
            if (_isTransitioning) return;

            if (settingsModal != null)
            {
                settingsModal.Open();
            }
        }

        private void OnQuitClicked()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            if (screenFader != null)
            {
                screenFader.blocksRaycasts = true;
                screenFader.DOFade(1f, 0.4f).OnComplete(QuitGame);
            }
            else
            {
                QuitGame();
            }
        }

        private void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void ShowToast(string message)
        {
            if (notificationToast == null) return;

            notificationToast.text = message;
            _toastTween?.Kill();
            notificationToast.alpha = 1f;

            _toastTween = DOVirtual.DelayedCall(1.6f, () =>
            {
                notificationToast.DOFade(0f, 0.4f);
            });
        }

        private void OnDestroy()
        {
            _toastTween?.Kill();
        }
    }
}
