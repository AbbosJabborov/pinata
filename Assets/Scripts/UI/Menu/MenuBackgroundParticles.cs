using UnityEngine;
using UnityEngine.UI;

namespace Pinata.UI.Menu
{
    public class MenuBackgroundParticles : MonoBehaviour
    {
        [System.Serializable]
        private class Floater
        {
            public RectTransform rect;
            public Image image;
            public float speed;
            public float swayFrequency;
            public float swayAmplitude;
            public float seed;
            public Vector2 startPos;
        }

        [SerializeField] private int floaterCount = 18;
        [SerializeField] private Color[] candyColors = new Color[]
        {
            new Color(1f, 0.4f, 0.65f, 0.25f), // Pink
            new Color(0.3f, 0.85f, 1f, 0.22f), // Cyan / Blue
            new Color(1f, 0.85f, 0.25f, 0.25f), // Yellow
            new Color(0.4f, 0.95f, 0.5f, 0.22f), // Green
            new Color(0.85f, 0.45f, 1f, 0.22f), // Purple
            new Color(1f, 0.6f, 0.2f, 0.22f)   // Orange
        };

        private Floater[] _floaters;
        private RectTransform _parentRect;

        private void Start()
        {
            _parentRect = GetComponent<RectTransform>();
            CreateFloaters();
        }

        private void CreateFloaters()
        {
            _floaters = new Floater[floaterCount];
            float width = _parentRect != null ? _parentRect.rect.width : 1920f;
            float height = _parentRect != null ? _parentRect.rect.height : 1080f;

            for (int i = 0; i < floaterCount; i++)
            {
                GameObject obj = new GameObject($"Floater_{i}");
                obj.transform.SetParent(transform, false);

                var rect = obj.AddComponent<RectTransform>();
                float size = Random.Range(24f, 80f);
                rect.sizeDelta = new Vector2(size, size);

                float startX = Random.Range(-width * 0.5f, width * 0.5f);
                float startY = Random.Range(-height * 0.5f, height * 0.5f);
                rect.anchoredPosition = new Vector2(startX, startY);

                var img = obj.AddComponent<Image>();
                Color c = candyColors[Random.Range(0, candyColors.Length)];
                img.color = c;
                img.raycastTarget = false;

                _floaters[i] = new Floater
                {
                    rect = rect,
                    image = img,
                    speed = Random.Range(20f, 50f),
                    swayFrequency = Random.Range(0.8f, 2.0f),
                    swayAmplitude = Random.Range(15f, 40f),
                    seed = Random.Range(0f, 100f),
                    startPos = rect.anchoredPosition
                };
            }
        }

        private void Update()
        {
            if (_floaters == null || _parentRect == null) return;

            float height = _parentRect.rect.height;
            float halfHeight = height * 0.5f;
            float time = Time.unscaledTime;

            for (int i = 0; i < _floaters.Length; i++)
            {
                var f = _floaters[i];
                if (f.rect == null) continue;

                Vector2 pos = f.rect.anchoredPosition;
                pos.y += f.speed * Time.unscaledDeltaTime;
                pos.x = f.startPos.x + Mathf.Sin((time + f.seed) * f.swayFrequency) * f.swayAmplitude;

                if (pos.y > halfHeight + 60f)
                {
                    pos.y = -halfHeight - 60f;
                    f.startPos.x = Random.Range(-_parentRect.rect.width * 0.5f, _parentRect.rect.width * 0.5f);
                }

                f.rect.anchoredPosition = pos;
            }
        }
    }
}
