using Pinata.Core;
using Pinata.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Pinata.UI
{
    /// <summary>
    /// Pause menu: freezes time, frees the cursor via CursorModeManager, and yields
    /// Escape to the upgrade kiosk if that's open instead of fighting it for the key.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }

        [Header("References")]
        [SerializeField] private GameObject panelRoot;

        [Header("Buttons (wire in Editor)")]
        [SerializeField] private UnityEngine.UI.Button resumeButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;

        private float _previousTimeScale = 1f;

        private void Awake()
        {
            IsPaused = false;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (quitButton != null) quitButton.onClick.AddListener(QuitToDesktop);
        }

        private void OnDisable()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (quitButton != null) quitButton.onClick.RemoveListener(QuitToDesktop);
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

            // Let the kiosk close itself first if it's the thing currently open.
            if (!IsPaused && UpgradeKiosk.Instance != null && UpgradeKiosk.Instance.IsOpen) return;

            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            if (panelRoot != null) panelRoot.SetActive(true);
            CursorModeManager.RequestUnlock(this);
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;

            Time.timeScale = _previousTimeScale > 0f ? _previousTimeScale : 1f;

            if (panelRoot != null) panelRoot.SetActive(false);
            CursorModeManager.ReleaseUnlock(this);
        }

        public void QuitToDesktop()
        {
            // Time.timeScale must be restored before quitting/loading, or the next
            // scene (or editor) can inherit a frozen timescale.
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
