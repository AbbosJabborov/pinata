using UnityEngine;
using UnityEngine.UI;

namespace Pinata.UI
{
    public class CrosshairUI : MonoBehaviour
    {
        [Header("Crosshair Visuals")]
        [SerializeField] private RectTransform dot;
        [SerializeField] private RectTransform ring;
        [SerializeField] private Image ringImage;

        private Vector2 _defaultRingSize = new Vector2(24f, 24f);
        private Vector2 _expandedRingSize = new Vector2(42f, 42f);
        private float _currentExpansion = 0f;

        private void Start()
        {
            if (ring != null)
            {
                _defaultRingSize = ring.sizeDelta;
                _expandedRingSize = _defaultRingSize * 1.6f;
            }
        }

        private void Update()
        {
            if (ring != null)
            {
                _currentExpansion = Mathf.MoveTowards(_currentExpansion, 0f, Time.deltaTime * 4f);
                ring.sizeDelta = Vector2.Lerp(_defaultRingSize, _expandedRingSize, _currentExpansion);
            }
        }

        public void Pulse()
        {
            _currentExpansion = 1.0f;
        }
    }
}
