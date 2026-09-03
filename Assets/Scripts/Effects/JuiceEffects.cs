using System.Collections;
using UnityEngine;

namespace Pinata.Effects
{
    public class JuiceEffects : MonoBehaviour
    {
        public static JuiceEffects Instance { get; private set; }

        [Header("Camera Shake Settings")]
        [SerializeField] private Transform cameraTransform;

        private Vector3 _originalCamLocalPos;
        private Coroutine _shakeCoroutine;
        private Coroutine _hitstopCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Application.runInBackground = true;
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            if (cameraTransform != null)
            {
                _originalCamLocalPos = cameraTransform.localPosition;
            }
        }

        public void SetCameraTransform(Transform cam)
        {
            cameraTransform = cam;
            if (cameraTransform != null)
            {
                _originalCamLocalPos = cameraTransform.localPosition;
            }
        }

        public void DoHitstop(float duration = 0.045f)
        {
            if (_hitstopCoroutine != null)
            {
                StopCoroutine(_hitstopCoroutine);
            }
            _hitstopCoroutine = StartCoroutine(HitstopRoutine(duration));
        }

        private IEnumerator HitstopRoutine(float duration)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1.0f;
            _hitstopCoroutine = null;
        }

        public void ShakeCamera(float duration = 0.15f, float magnitude = 0.12f)
        {
            if (cameraTransform == null) return;

            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                cameraTransform.localPosition = _originalCamLocalPos;
            }
            _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0.0f;
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                cameraTransform.localPosition = _originalCamLocalPos + new Vector3(x, y, 0);

                elapsed += Time.unscaledDeltaTime;
                magnitude = Mathf.Lerp(magnitude, 0f, elapsed / duration);
                yield return null;
            }

            cameraTransform.localPosition = _originalCamLocalPos;
            _shakeCoroutine = null;
        }
    }
}
