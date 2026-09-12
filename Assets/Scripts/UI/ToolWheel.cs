using DG.Tweening;
using Pinata.Tools;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pinata.UI
{
    /// <summary>
    /// 3-slot rotating tool wheel, adapted from the earlier 4-item ItemsCircleRotator.
    /// The ring sits low enough that only the top slot clears the canvas cutoff line -
    /// that's the equipped tool. Previous/Next rotates the ring 120 degrees and
    /// hands the new top slot to ToolManager.
    ///
    /// Slot order is fixed at Hand(0) / Broom(1) / Bat(2) placed 120 degrees apart
    /// around itemsCircle in the Editor, starting with Hand at the top (0 degrees).
    /// Locked slots (see ToolManager.IsSlotUnlocked) are skipped when cycling.
    /// </summary>
    public class ToolWheel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ToolManager toolManager;
        [SerializeField] private GameObject itemsCircle;

        [Header("Rotation")]
        [SerializeField] private float rotationDuration = 0.5f;
        private const float StepAngle = 120f; // 360 / 3 slots

        [Header("Slot Icons (order must match ToolManager's tool list: Hand, Broom, Bat)")]
        [SerializeField] private Image[] slotIcons = new Image[3];
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);

        private Tween _currentTween;
        private int _currentIndex = 0; // which tool is currently at the top slot

        private void Awake()
        {
            if (toolManager == null) toolManager = ToolManager.Instance;
        }

        private void OnEnable()
        {
            if (toolManager != null)
            {
                toolManager.OnUnlockedCountChanged += HandleUnlockedCountChanged;
            }
            RefreshLockVisuals();
        }

        private void OnDisable()
        {
            if (toolManager != null)
            {
                toolManager.OnUnlockedCountChanged -= HandleUnlockedCountChanged;
            }
            _currentTween?.Kill();
            _currentTween = null;
        }

        public void OnPrevious(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            TryRotate(-1);
        }

        public void OnNext(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            TryRotate(1);
        }

        private void TryRotate(int direction)
        {
            if (_currentTween != null && _currentTween.IsActive() && _currentTween.IsPlaying()) return;
            if (toolManager == null || toolManager.UnlockedCount <= 1) return; // nothing to rotate to yet

            int unlocked = toolManager.UnlockedCount;
            int nextIndex = ((_currentIndex + direction) % unlocked + unlocked) % unlocked;

            float startZ = itemsCircle.transform.rotation.eulerAngles.z;
            // Ring rotates opposite the logical index step so the correct icon lands on top.
            float targetZ = startZ - (direction * StepAngle);

            _currentTween = itemsCircle.transform
                .DORotate(new Vector3(0, 0, targetZ), rotationDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    _currentTween = null;
                    _currentIndex = nextIndex;
                    toolManager.SelectSlot(_currentIndex);
                });
        }

        private void HandleUnlockedCountChanged(int unlockedCount)
        {
            RefreshLockVisuals();
        }

        private void RefreshLockVisuals()
        {
            if (slotIcons == null || toolManager == null) return;

            for (int i = 0; i < slotIcons.Length; i++)
            {
                if (slotIcons[i] == null) continue;
                slotIcons[i].color = toolManager.IsSlotUnlocked(i) ? unlockedColor : lockedColor;
            }
        }
    }
}
