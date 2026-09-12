using System;
using UnityEngine;

namespace Pinata.UI.Menu
{
    public static class GameSettings
    {
        private const string PrefKeyMasterVolume = "Settings_MasterVolume";
        private const string PrefKeyIsMuted = "Settings_IsMuted";
        private const string PrefKeyQualityLevel = "Settings_QualityLevel";
        private const string PrefKeyFullscreen = "Settings_Fullscreen";
        private const string PrefKeyVSync = "Settings_VSync";
        private const string PrefKeyMouseSensitivity = "Settings_MouseSensitivity";
        private const string PrefKeyInvertY = "Settings_InvertY";
        private const string PrefKeyFieldOfView = "Settings_FieldOfView";

        // Defaults
        public const float DefaultMasterVolume = 0.80f;
        public const bool DefaultIsMuted = false;
        public const bool DefaultFullscreen = true;
        public const bool DefaultVSync = true;
        public const float DefaultMouseSensitivity = 0.15f;
        public const bool DefaultInvertY = false;
        public const float DefaultFieldOfView = 75f;

        // Current values
        private static float _masterVolume;
        private static bool _isMuted;
        private static int _qualityLevel;
        private static bool _isFullscreen;
        private static bool _isVSync;
        private static float _mouseSensitivity;
        private static bool _invertY;
        private static float _fieldOfView;

        public static float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                ApplyAudio();
                PlayerPrefs.SetFloat(PrefKeyMasterVolume, _masterVolume);
            }
        }

        public static bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value;
                ApplyAudio();
                PlayerPrefs.SetInt(PrefKeyIsMuted, _isMuted ? 1 : 0);
            }
        }

        public static int QualityLevel
        {
            get => _qualityLevel;
            set
            {
                _qualityLevel = Mathf.Clamp(value, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
                QualitySettings.SetQualityLevel(_qualityLevel, true);
                PlayerPrefs.SetInt(PrefKeyQualityLevel, _qualityLevel);
            }
        }

        public static bool IsFullscreen
        {
            get => _isFullscreen;
            set
            {
                _isFullscreen = value;
                Screen.fullScreen = _isFullscreen;
                PlayerPrefs.SetInt(PrefKeyFullscreen, _isFullscreen ? 1 : 0);
            }
        }

        public static bool IsVSync
        {
            get => _isVSync;
            set
            {
                _isVSync = value;
                QualitySettings.vSyncCount = _isVSync ? 1 : 0;
                PlayerPrefs.SetInt(PrefKeyVSync, _isVSync ? 1 : 0);
            }
        }

        public static float MouseSensitivity
        {
            get => _mouseSensitivity;
            set
            {
                _mouseSensitivity = Mathf.Clamp(value, 0.02f, 0.50f);
                PlayerPrefs.SetFloat(PrefKeyMouseSensitivity, _mouseSensitivity);
                OnControlsChanged?.Invoke();
            }
        }

        public static bool InvertY
        {
            get => _invertY;
            set
            {
                _invertY = value;
                PlayerPrefs.SetInt(PrefKeyInvertY, _invertY ? 1 : 0);
                OnControlsChanged?.Invoke();
            }
        }

        public static float FieldOfView
        {
            get => _fieldOfView;
            set
            {
                _fieldOfView = Mathf.Clamp(value, 60f, 100f);
                PlayerPrefs.SetFloat(PrefKeyFieldOfView, _fieldOfView);
                ApplyCameraFOV();
            }
        }

        public static event Action OnControlsChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            Load();
        }

        public static void Load()
        {
            _masterVolume = PlayerPrefs.GetFloat(PrefKeyMasterVolume, DefaultMasterVolume);
            _isMuted = PlayerPrefs.GetInt(PrefKeyIsMuted, DefaultIsMuted ? 1 : 0) == 1;
            _qualityLevel = PlayerPrefs.GetInt(PrefKeyQualityLevel, QualitySettings.GetQualityLevel());
            _isFullscreen = PlayerPrefs.GetInt(PrefKeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
            _isVSync = PlayerPrefs.GetInt(PrefKeyVSync, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
            _mouseSensitivity = PlayerPrefs.GetFloat(PrefKeyMouseSensitivity, DefaultMouseSensitivity);
            _invertY = PlayerPrefs.GetInt(PrefKeyInvertY, DefaultInvertY ? 1 : 0) == 1;
            _fieldOfView = PlayerPrefs.GetFloat(PrefKeyFieldOfView, DefaultFieldOfView);

            ApplyAll();
        }

        public static void ResetToDefaults()
        {
            MasterVolume = DefaultMasterVolume;
            IsMuted = DefaultIsMuted;
            QualityLevel = Mathf.Clamp(2, 0, Mathf.Max(0, QualitySettings.names.Length - 1)); // Default Medium/High
            IsFullscreen = DefaultFullscreen;
            IsVSync = DefaultVSync;
            MouseSensitivity = DefaultMouseSensitivity;
            InvertY = DefaultInvertY;
            FieldOfView = DefaultFieldOfView;
            PlayerPrefs.Save();
        }

        public static void ApplyAll()
        {
            ApplyAudio();
            QualitySettings.SetQualityLevel(_qualityLevel, true);
            Screen.fullScreen = _isFullscreen;
            QualitySettings.vSyncCount = _isVSync ? 1 : 0;
            ApplyCameraFOV();
            OnControlsChanged?.Invoke();
        }

        private static void ApplyAudio()
        {
            AudioListener.volume = _isMuted ? 0f : _masterVolume;
        }

        public static void ApplyCameraFOV()
        {
            Camera cam = Camera.main;
            if (cam != null && !cam.orthographic)
            {
                cam.fieldOfView = _fieldOfView;
            }
        }
    }
}
