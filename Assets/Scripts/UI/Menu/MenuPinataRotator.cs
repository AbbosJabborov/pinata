using UnityEngine;

namespace Pinata.UI.Menu
{
    public class MenuPinataRotator : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 18f;
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobFrequency = 1.2f;

        private Vector3 _initialPos;

        private void Start()
        {
            _initialPos = transform.position;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            float offset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            transform.position = _initialPos + new Vector3(0f, offset, 0f);
        }
    }
}
