using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pinata.UI.Menu
{
    public class MenuButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Target Components")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Image accentBar;

        [Header("Hover Settings")]
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float animDuration = 0.25f;
        [SerializeField] private Color normalBgColor = new Color(0.12f, 0.14f, 0.20f, 0.85f);
        [SerializeField] private Color hoverBgColor = new Color(0.20f, 0.24f, 0.35f, 0.95f);
        [SerializeField] private Color normalTextColor = new Color(0.92f, 0.94f, 0.98f, 1f);
        [SerializeField] private Color hoverTextColor = new Color(1f, 0.88f, 0.35f, 1f);
        [SerializeField] private Color accentBarColor = new Color(1f, 0.45f, 0.7f, 1f);

        [Header("State")]
        [SerializeField] private bool isInteractable = true;

        private Vector3 _originalScale = Vector3.one;
        private Tween _scaleTween;
        private Tween _bgTween;
        private Tween _textTween;
        private Tween _barTween;

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();
            if (labelText == null) labelText = GetComponentInChildren<TextMeshProUGUI>();

            _originalScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
            ApplyInitialStyles();
        }

        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
            if (!isInteractable)
            {
                if (backgroundImage != null) backgroundImage.color = new Color(normalBgColor.r, normalBgColor.g, normalBgColor.b, 0.4f);
                if (labelText != null) labelText.color = new Color(normalTextColor.r, normalTextColor.g, normalTextColor.b, 0.4f);
                if (accentBar != null) accentBar.gameObject.SetActive(false);
            }
            else
            {
                ApplyInitialStyles();
            }
        }

        private void ApplyInitialStyles()
        {
            if (backgroundImage != null) backgroundImage.color = normalBgColor;
            if (labelText != null) labelText.color = normalTextColor;
            if (accentBar != null)
            {
                accentBar.color = accentBarColor;
                accentBar.rectTransform.sizeDelta = new Vector2(0, accentBar.rectTransform.sizeDelta.y);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable) return;

            KillTweens();

            _scaleTween = rectTransform.DOScale(_originalScale * hoverScale, animDuration).SetEase(Ease.OutBack).SetUpdate(true);
            
            if (backgroundImage != null)
            {
                _bgTween = backgroundImage.DOColor(hoverBgColor, animDuration).SetUpdate(true);
            }

            if (labelText != null)
            {
                _textTween = labelText.DOColor(hoverTextColor, animDuration).SetUpdate(true);
            }

            if (accentBar != null)
            {
                _barTween = accentBar.rectTransform.DOSizeDelta(new Vector2(8f, accentBar.rectTransform.sizeDelta.y), animDuration)
                    .SetEase(Ease.OutCubic).SetUpdate(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable) return;

            KillTweens();

            _scaleTween = rectTransform.DOScale(_originalScale, animDuration).SetEase(Ease.OutQuad).SetUpdate(true);

            if (backgroundImage != null)
            {
                _bgTween = backgroundImage.DOColor(normalBgColor, animDuration).SetUpdate(true);
            }

            if (labelText != null)
            {
                _textTween = labelText.DOColor(normalTextColor, animDuration).SetUpdate(true);
            }

            if (accentBar != null)
            {
                _barTween = accentBar.rectTransform.DOSizeDelta(new Vector2(0f, accentBar.rectTransform.sizeDelta.y), animDuration)
                    .SetEase(Ease.OutQuad).SetUpdate(true);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isInteractable) return;

            _scaleTween?.Kill();
            _scaleTween = rectTransform.DOScale(_originalScale * 0.96f, 0.1f).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isInteractable) return;

            _scaleTween?.Kill();
            _scaleTween = rectTransform.DOScale(_originalScale * hoverScale, 0.15f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void KillTweens()
        {
            _scaleTween?.Kill();
            _bgTween?.Kill();
            _textTween?.Kill();
            _barTween?.Kill();
        }

        private void OnDestroy()
        {
            KillTweens();
        }
    }
}
