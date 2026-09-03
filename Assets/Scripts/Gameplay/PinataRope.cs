using UnityEngine;

namespace Pinata.Gameplay
{
    [RequireComponent(typeof(LineRenderer))]
    public class PinataRope : MonoBehaviour
    {
        [Header("Rope Anchors")]
        [SerializeField] private Transform topAnchor;
        [SerializeField] private Transform bottomAnchor;
        [SerializeField] private int segmentCount = 10;
        [SerializeField] private float slack = 0.05f;

        private LineRenderer _lineRenderer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer != null)
            {
                _lineRenderer.useWorldSpace = true;
                _lineRenderer.positionCount = segmentCount;
                _lineRenderer.startWidth = 0.04f;
                _lineRenderer.endWidth = 0.04f;
                _lineRenderer.widthMultiplier = 1.0f;
            }
        }

        public void SetAnchors(Transform top, Transform bottom)
        {
            topAnchor = top;
            bottomAnchor = bottom;
        }

        private void LateUpdate()
        {
            if (topAnchor == null || bottomAnchor == null || _lineRenderer == null) return;

            Vector3 start = topAnchor.position;
            Vector3 end = bottomAnchor.position;

            for (int i = 0; i < segmentCount; i++)
            {
                float t = i / (float)(segmentCount - 1);
                Vector3 pos = Vector3.Lerp(start, end, t);

                // Subtle catenary droop in middle
                float sag = Mathf.Sin(t * Mathf.PI) * slack;
                pos.y -= sag;

                _lineRenderer.SetPosition(i, pos);
            }
        }
    }
}
