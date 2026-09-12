using System;
using Pinata.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinata.Gameplay
{
    /// <summary>
    /// Upgrade terminal. Opens/closes purely through the IInteractable + PlayerInteractor
    /// raycast flow (no separate proximity trigger) so there's a single source of truth
    /// for "is the player interacting with this".
    /// </summary>
    public class UpgradeKiosk : MonoBehaviour, IInteractable
    {
        public static UpgradeKiosk Instance { get; private set; }

        private bool _isOpen = false;

        public bool IsOpen => _isOpen;

        public event Action<bool> OnKioskStateChanged; // (isOpen)

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_isOpen && !Pinata.UI.PauseMenu.IsPaused && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseKiosk();
            }
        }

        public void ToggleKiosk()
        {
            if (_isOpen) CloseKiosk();
            else OpenKiosk();
        }

        public void OpenKiosk()
        {
            _isOpen = true;
            Pinata.Core.CursorModeManager.RequestUnlock(this);
            OnKioskStateChanged?.Invoke(true);
        }

        public void CloseKiosk()
        {
            _isOpen = false;
            Pinata.Core.CursorModeManager.ReleaseUnlock(this);
            OnKioskStateChanged?.Invoke(false);
        }

        private void OnDisable()
        {
            if (_isOpen)
            {
                _isOpen = false;
                Pinata.Core.CursorModeManager.ReleaseUnlock(this);
                OnKioskStateChanged?.Invoke(false);
            }
        }

        #region IInteractable Implementation
        public string InteractableName => "Upgrade Terminal";

        public System.Collections.Generic.List<InputPrompt> GetPrompts
        {
            get
            {
                return new System.Collections.Generic.List<InputPrompt>
                {
                    new InputPrompt(InputPrompt.IconType.KeyE, _isOpen ? "Close Terminal" : "Open Upgrades")
                };
            }
        }

        public float HoldDuration => 0f;

        public void OnInteractStart(PlayerInteractor interactor) { }
        public void OnInteractCanceled(PlayerInteractor interactor) { }

        public void OnInteractComplete(PlayerInteractor interactor)
        {
            ToggleKiosk();
        }
        #endregion
    }
}
