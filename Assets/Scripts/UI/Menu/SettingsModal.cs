using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinata.UI.Menu
{
    public class SettingsModal : MonoBehaviour
    {
        [Header("Modal Animation References")]
        [SerializeField] private CanvasGroup backdropCanvasGroup;
        [SerializeField] private RectTransform windowRect;
        [SerializeField] private CanvasGroup windowCanvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backdropButton;
        [SerializeField] private Button resetDefaultsButton;

        [Header("Audio Controls")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeValueText;
        [SerializeField] private Button muteToggleButton;
        [SerializeField] private TextMeshProUGUI muteToggleStatusText;
        [SerializeField] private Image muteToggleIndicator;

        [Header("Display / Graphics Controls")]
        [SerializeField] private Button[] qualityButtons;
        [SerializeField] private Image[] qualityButtonImages;
        [SerializeField] private TextMeshProUGUI[] qualityButtonTexts;
        [SerializeField] private Button fullscreenToggleButton;
        [SerializeField] private TextMeshProUGUI fullscreenStatusText;
        [SerializeField] private Image fullscreenIndicator;
        [SerializeField] private Button vsyncToggleButton;
        [SerializeField] private TextMeshProUGUI vsyncStatusText;
        [SerializeField] private Image vsyncIndicator;

        [Header("Controls / Gameplay")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TextMeshProUGUI sensitivityValueText;
        [SerializeField] private Button invertYToggleButton;
        [SerializeField] private TextMeshProUGUI invertYStatusText;
        [SerializeField] private Image invertYIndicator;
        [SerializeField] private Slider fovSlider;
        [SerializeField] private TextMeshProUGUI fovValueText;

        [Header("Styling Colors")]
        [SerializeField] private Color toggleActiveColor = new Color(0.2f, 0.9f, 0.5f, 1f); // Mint green
        [SerializeField] private Color toggleInactiveColor = new Color(0.35f, 0.40f, 0.52f, 0.5f);
        [SerializeField] private Color qualityActiveColor = new Color(0.78f, 0.45f, 1f, 1f); // Electric purple
        [SerializeField] private Color qualityInactiveColor = new Color(0.16f, 0.19f, 0.28f, 0.85f);

        [Header("Animation Settings")]
        [SerializeField] private float animDuration = 0.3f;

        private Tween _fadeTween;
        private Tween _scaleTween;
        private Tween _windowFadeTween;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);
            if (resetDefaultsButton != null) resetDefaultsButton.onClick.AddListener(OnResetDefaultsClicked);

            HookUIListeners();

            // Start closed
            if (backdropCanvasGroup != null)
            {
                backdropCanvasGroup.alpha = 0f;
                backdropCanvasGroup.blocksRaycasts = false;
            }

            if (windowCanvasGroup != null)
            {
                windowCanvasGroup.alpha = 0f;
                windowCanvasGroup.blocksRaycasts = false;
            }

            if (windowRect != null)
            {
                windowRect.localScale = Vector3.one * 0.85f;
            }

            gameObject.SetActive(false);
        }

        private void HookUIListeners()
        {
            // Audio
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
            }

            if (muteToggleButton != null)
            {
                muteToggleButton.onClick.AddListener(OnMuteToggleClicked);
            }

            // Quality
            if (qualityButtons != null)
            {
                for (int i = 0; i < qualityButtons.Length; i++)
                {
                    int level = i;
                    if (qualityButtons[i] != null)
                    {
                        qualityButtons[i].onClick.AddListener(() => OnQualityButtonClicked(level));
                    }
                }
            }

            // Display toggles
            if (fullscreenToggleButton != null)
            {
                fullscreenToggleButton.onClick.AddListener(OnFullscreenToggleClicked);
            }

            if (vsyncToggleButton != null)
            {
                vsyncToggleButton.onClick.AddListener(OnVSyncToggleClicked);
            }

            // Controls
            if (sensitivitySlider != null)
            {
                sensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderChanged);
            }

            if (invertYToggleButton != null)
            {
                invertYToggleButton.onClick.AddListener(OnInvertYToggleClicked);
            }

            if (fovSlider != null)
            {
                fovSlider.onValueChanged.AddListener(OnFOVSliderChanged);
            }
        }

        public void Open()
        {
            _isOpen = true;
            gameObject.SetActive(true);

            // Sync UI elements with current GameSettings
            SyncAllUIFromSettings();

            KillTweens();

            if (backdropCanvasGroup != null)
            {
                backdropCanvasGroup.blocksRaycasts = true;
                _fadeTween = backdropCanvasGroup.DOFade(1f, animDuration).SetUpdate(true);
            }

            if (windowRect != null)
            {
                windowRect.localScale = Vector3.one * 0.85f;
                _scaleTween = windowRect.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack, 1.2f).SetUpdate(true);
            }

            if (windowCanvasGroup != null)
            {
                windowCanvasGroup.blocksRaycasts = true;
                _windowFadeTween = windowCanvasGroup.DOFade(1f, animDuration * 0.8f).SetUpdate(true);
            }
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;

            KillTweens();

            if (backdropCanvasGroup != null)
            {
                backdropCanvasGroup.blocksRaycasts = false;
                _fadeTween = backdropCanvasGroup.DOFade(0f, animDuration * 0.7f).SetUpdate(true);
            }

            if (windowCanvasGroup != null)
            {
                windowCanvasGroup.blocksRaycasts = false;
                _windowFadeTween = windowCanvasGroup.DOFade(0f, animDuration * 0.7f).SetUpdate(true);
            }

            if (windowRect != null)
            {
                _scaleTween = windowRect.DOScale(Vector3.one * 0.85f, animDuration * 0.7f)
                    .SetEase(Ease.InBack, 1.1f)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (!_isOpen) gameObject.SetActive(false);
                    });
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void SyncAllUIFromSettings()
        {
            GameSettings.Load();

            // Master Volume
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
            }
            UpdateVolumeLabel(GameSettings.MasterVolume);

            // Mute
            UpdateToggleUI(muteToggleIndicator, muteToggleStatusText, GameSettings.IsMuted ? "MUTED" : "UNMUTED", !GameSettings.IsMuted);

            // Quality
            UpdateQualityButtonsUI(GameSettings.QualityLevel);

            // Fullscreen
            UpdateToggleUI(fullscreenIndicator, fullscreenStatusText, GameSettings.IsFullscreen ? "ON" : "OFF", GameSettings.IsFullscreen);

            // VSync
            UpdateToggleUI(vsyncIndicator, vsyncStatusText, GameSettings.IsVSync ? "ON" : "OFF", GameSettings.IsVSync);

            // Sensitivity
            if (sensitivitySlider != null)
            {
                sensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            }
            UpdateSensitivityLabel(GameSettings.MouseSensitivity);

            // Invert Y
            UpdateToggleUI(invertYIndicator, invertYStatusText, GameSettings.InvertY ? "INVERTED" : "NORMAL", GameSettings.InvertY);

            // FOV
            if (fovSlider != null)
            {
                fovSlider.SetValueWithoutNotify(GameSettings.FieldOfView);
            }
            UpdateFOVLabel(GameSettings.FieldOfView);
        }

        private void OnVolumeSliderChanged(float val)
        {
            GameSettings.MasterVolume = val;
            UpdateVolumeLabel(val);
        }

        private void UpdateVolumeLabel(float val)
        {
            if (masterVolumeValueText != null)
            {
                masterVolumeValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
            }
        }

        private void OnMuteToggleClicked()
        {
            GameSettings.IsMuted = !GameSettings.IsMuted;
            UpdateToggleUI(muteToggleIndicator, muteToggleStatusText, GameSettings.IsMuted ? "MUTED" : "UNMUTED", !GameSettings.IsMuted);
            AnimateButtonPunch(muteToggleButton != null ? muteToggleButton.transform : null);
        }

        private void OnQualityButtonClicked(int level)
        {
            GameSettings.QualityLevel = level;
            UpdateQualityButtonsUI(level);
            if (qualityButtons != null && level >= 0 && level < qualityButtons.Length)
            {
                AnimateButtonPunch(qualityButtons[level].transform);
            }
        }

        private void UpdateQualityButtonsUI(int activeLevel)
        {
            if (qualityButtons == null || qualityButtonImages == null) return;

            for (int i = 0; i < qualityButtons.Length; i++)
            {
                bool isActive = (i == activeLevel);
                if (i < qualityButtonImages.Length && qualityButtonImages[i] != null)
                {
                    qualityButtonImages[i].color = isActive ? qualityActiveColor : qualityInactiveColor;
                }
                if (qualityButtonTexts != null && i < qualityButtonTexts.Length && qualityButtonTexts[i] != null)
                {
                    qualityButtonTexts[i].color = isActive ? Color.white : new Color(0.7f, 0.72f, 0.8f, 0.75f);
                }
            }
        }

        private void OnFullscreenToggleClicked()
        {
            GameSettings.IsFullscreen = !GameSettings.IsFullscreen;
            UpdateToggleUI(fullscreenIndicator, fullscreenStatusText, GameSettings.IsFullscreen ? "ON" : "OFF", GameSettings.IsFullscreen);
            AnimateButtonPunch(fullscreenToggleButton != null ? fullscreenToggleButton.transform : null);
        }

        private void OnVSyncToggleClicked()
        {
            GameSettings.IsVSync = !GameSettings.IsVSync;
            UpdateToggleUI(vsyncIndicator, vsyncStatusText, GameSettings.IsVSync ? "ON" : "OFF", GameSettings.IsVSync);
            AnimateButtonPunch(vsyncToggleButton != null ? vsyncToggleButton.transform : null);
        }

        private void OnSensitivitySliderChanged(float val)
        {
            GameSettings.MouseSensitivity = val;
            UpdateSensitivityLabel(val);
        }

        private void UpdateSensitivityLabel(float val)
        {
            if (sensitivityValueText != null)
            {
                sensitivityValueText.text = $"{val:0.00}";
            }
        }

        private void OnInvertYToggleClicked()
        {
            GameSettings.InvertY = !GameSettings.InvertY;
            UpdateToggleUI(invertYIndicator, invertYStatusText, GameSettings.InvertY ? "INVERTED" : "NORMAL", GameSettings.InvertY);
            AnimateButtonPunch(invertYToggleButton != null ? invertYToggleButton.transform : null);
        }

        private void OnFOVSliderChanged(float val)
        {
            GameSettings.FieldOfView = val;
            UpdateFOVLabel(val);
        }

        private void UpdateFOVLabel(float val)
        {
            if (fovValueText != null)
            {
                fovValueText.text = $"{Mathf.RoundToInt(val)}°";
            }
        }

        private void OnResetDefaultsClicked()
        {
            GameSettings.ResetToDefaults();
            SyncAllUIFromSettings();

            if (resetDefaultsButton != null)
            {
                resetDefaultsButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f, 10, 1).SetUpdate(true);
            }
        }

        private void UpdateToggleUI(Image indicator, TextMeshProUGUI label, string text, bool isOn)
        {
            if (indicator != null)
            {
                indicator.color = isOn ? toggleActiveColor : toggleInactiveColor;
            }
            if (label != null)
            {
                label.text = text;
                label.color = isOn ? Color.white : new Color(0.7f, 0.72f, 0.8f, 0.7f);
            }
        }

        private void AnimateButtonPunch(Transform target)
        {
            if (target != null)
            {
                target.DOKill();
                target.DOPunchScale(Vector3.one * 0.08f, 0.18f, 8, 1).SetUpdate(true);
            }
        }

        private void KillTweens()
        {
            _fadeTween?.Kill();
            _scaleTween?.Kill();
            _windowFadeTween?.Kill();
        }

        private void OnDestroy()
        {
            KillTweens();
        }
    }
}
