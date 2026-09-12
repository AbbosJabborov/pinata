using Pinata.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Pinata.Interaction
{
    /// <summary>
    /// Animates the crosshair dot and hover ring when pointing at an interactable.
    /// Can use circle and hollow hole images, or scale/fade a single crosshair element.
    /// </summary>
    public class CrosshairController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private Image circleImage;
        [SerializeField] private Image holeImage;

        [Header("Sizes")]
        [SerializeField] private float normalSize = 10f;
        [SerializeField] private float hoverSize = 18f;

        [Header("Animation")]
        [Tooltip("Higher = snappier transition.")]
        [SerializeField] private float lerpSpeed = 12f;

        // 0 = idle (circle), 1 = hover (hole)
        private float _t;

        private RectTransform _circleRect;
        private RectTransform _holeRect;

        private void Awake()
        {
            if (interactor == null) interactor = PlayerInteractor.Instance;

            if (circleImage != null) _circleRect = circleImage.rectTransform;
            if (holeImage != null) _holeRect = holeImage.rectTransform;

            SetState(0f);
        }

        private void Update()
        {
            if (CursorModeManager.IsUnlocked)
            {
                SetAlpha(circleImage, 0f);
                SetAlpha(holeImage, 0f);
                return;
            }

            if (interactor == null)
            {
                interactor = PlayerInteractor.Instance;
                if (interactor == null) return;
            }

            float target = interactor.IsLookingAtInteractable ? 1f : 0f;
            _t = Mathf.Lerp(_t, target, lerpSpeed * Time.deltaTime);

            SetState(_t);
        }

        private void SetState(float t)
        {
            float size = Mathf.Lerp(normalSize, hoverSize, t);

            if (_circleRect != null)
            {
                _circleRect.sizeDelta = new Vector2(size, size);
                SetAlpha(circleImage, 1f - t);
            }

            if (_holeRect != null)
            {
                _holeRect.sizeDelta = new Vector2(size, size);
                SetAlpha(holeImage, t);
            }
        }

        private static void SetAlpha(Image img, float a)
        {
            if (img == null) return;
            Color c = img.color;
            c.a = a;
            img.color = c;
        }
    }
}
