using System.Collections;
using Pinata.Player;
using UnityEngine;

namespace Pinata.Candy
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class CandyItem : MonoBehaviour
    {
        [Header("Candy Config")]
        [SerializeField] private CandyType type = CandyType.Mint;
        [SerializeField] private int baseCashValue = 2;
        [SerializeField] private int baseRepValue = 1;
        [SerializeField] private Vector3 originalScale = new Vector3(0.25f, 0.25f, 0.25f);

        private Rigidbody _rb;
        private Collider _col;
        private bool _isBeingCollected = false;

        public CandyType Type => type;
        public int CashValue => baseCashValue;
        public int RepValue => baseRepValue;
        public Rigidbody Rigidbody => _rb;
        public bool IsBeingCollected => _isBeingCollected;
        public Vector3 OriginalScale => originalScale;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
            if (transform.localScale != Vector3.zero)
            {
                originalScale = transform.localScale;
            }

            int candyLayer = LayerMask.NameToLayer("Candy");
            if (candyLayer != -1 && gameObject.layer == 0) gameObject.layer = candyLayer;

            IgnorePlayerCollision();
        }

        private void IgnorePlayerCollision()
        {
            if (_col != null && FirstPersonPlayer.Instance != null && FirstPersonPlayer.Instance.Controller != null)
            {
                Physics.IgnoreCollision(_col, FirstPersonPlayer.Instance.Controller, true);
            }
        }

        private void Update()
        {
            if (transform.position.y < -0.5f && !_isBeingCollected)
            {
                transform.position = new Vector3(transform.position.x, 0.2f, transform.position.z);
                if (_rb != null && !_rb.isKinematic)
                {
                    _rb.linearVelocity = new Vector3(_rb.linearVelocity.x * 0.5f, 1.5f, _rb.linearVelocity.z * 0.5f);
                }
            }
        }

        public void Initialize(CandyType newType, int cash, int rep, Vector3 scale)
        {
            type = newType;
            baseCashValue = cash;
            baseRepValue = rep;
            originalScale = scale;
            transform.localScale = scale;
            ResetState();
        }

        public void Initialize(CandyType newType, int cash, int rep)
        {
            Vector3 s = transform.localScale != Vector3.zero ? transform.localScale : originalScale;
            Initialize(newType, cash, rep, s);
        }

        public void ResetState()
        {
            _isBeingCollected = false;
            if (originalScale == Vector3.zero)
            {
                originalScale = transform.localScale != Vector3.zero ? transform.localScale : new Vector3(0.25f, 0.25f, 0.25f);
            }
            transform.localScale = originalScale;
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.useGravity = true;
            }
            if (_col != null)
            {
                _col.enabled = true;
                IgnorePlayerCollision();
            }
        }

        public void StartSuction(Vector3 targetPos, float speed)
        {
            if (_isBeingCollected) return;
            Vector3 direction = (targetPos - transform.position).normalized;
            _rb.AddForce(direction * speed, ForceMode.Acceleration);
        }

        public void Collect(System.Action<CandyItem> onComplete)
        {
            if (_isBeingCollected) return;
            _isBeingCollected = true;
            StartCoroutine(ShrinkAndCollectRoutine(onComplete));
        }

        private IEnumerator ShrinkAndCollectRoutine(System.Action<CandyItem> onComplete)
        {
            if (_col != null) _col.enabled = false;
            if (_rb != null) _rb.isKinematic = true;

            float duration = 0.15f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            transform.localScale = Vector3.zero;
            onComplete?.Invoke(this);
        }
    }
}
