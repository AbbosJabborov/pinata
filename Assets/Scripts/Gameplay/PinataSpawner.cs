using System.Collections;
using UnityEngine;

namespace Pinata.Gameplay
{
    public class PinataSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private PinataHealth pinataPrefab;
        [SerializeField] private Transform ceilingAnchor;
        [SerializeField] private float respawnDelay = 2.0f;
        [SerializeField] private float hangingDistance = 3.2f;

        [Header("Rope")]
        [SerializeField] private PinataRope rope;

        private PinataHealth _currentPinata;

        public PinataRope Rope { get => rope; set => rope = value; }

        private void Start()
        {
            if (ceilingAnchor == null)
            {
                ceilingAnchor = transform;
            }

            // If an initial pinata was pre-spawned in the scene, connect it
            if (_currentPinata == null)
            {
                _currentPinata = GetComponentInChildren<PinataHealth>();
            }

            if (_currentPinata != null)
            {
                _currentPinata.OnDestroyed += HandlePinataDestroyed;
                SetupJointAndRope(_currentPinata);
            }
            else
            {
                SpawnPinata();
            }
        }

        public void SpawnPinata()
        {
            Vector3 spawnPos = ceilingAnchor.position + Vector3.down * hangingDistance;

            if (_currentPinata != null)
            {
                Destroy(_currentPinata.gameObject);
                _currentPinata = null;
            }

            if (pinataPrefab != null)
            {
                _currentPinata = Instantiate(pinataPrefab, spawnPos, Quaternion.identity, transform);
                _currentPinata.OnDestroyed += HandlePinataDestroyed;
                SetupJointAndRope(_currentPinata);
            }
        }

        private void SetupJointAndRope(PinataHealth pinata)
        {
            Rigidbody anchorRb = ceilingAnchor.GetComponent<Rigidbody>();
            if (anchorRb == null)
            {
                anchorRb = ceilingAnchor.gameObject.AddComponent<Rigidbody>();
                anchorRb.isKinematic = true;
            }

            var joint = pinata.GetComponent<ConfigurableJoint>();
            if (joint == null)
            {
                joint = pinata.gameObject.AddComponent<ConfigurableJoint>();
            }

            joint.connectedBody = anchorRb;
            // The pivot is at the ceiling anchor above the pinata
            joint.anchor = new Vector3(0, hangingDistance, 0);
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = Vector3.zero;

            // Lock linear motion to pivot around anchor
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            // Allow full 3D angular freedom with limits
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;

            var highLimit = joint.highAngularXLimit;
            highLimit.limit = 80f;
            joint.highAngularXLimit = highLimit;

            var lowLimit = joint.lowAngularXLimit;
            lowLimit.limit = -80f;
            joint.lowAngularXLimit = lowLimit;

            var yLimit = joint.angularYLimit;
            yLimit.limit = 75f;
            joint.angularYLimit = yLimit;

            var zLimit = joint.angularZLimit;
            zLimit.limit = 80f;
            joint.angularZLimit = zLimit;

            // Restoration spring & damper
            joint.rotationDriveMode = RotationDriveMode.Slerp;
            var slerp = joint.slerpDrive;
            slerp.positionSpring = 45f;
            slerp.positionDamper = 3.2f;
            slerp.maximumForce = 500f;
            joint.slerpDrive = slerp;

            // Connect dynamic visual rope
            if (rope != null)
            {
                rope.SetAnchors(ceilingAnchor, pinata.AttachmentPoint);
                rope.gameObject.SetActive(true);
            }
        }

        private void HandlePinataDestroyed()
        {
            if (_currentPinata != null)
            {
                _currentPinata.OnDestroyed -= HandlePinataDestroyed;
            }
            if (rope != null)
            {
                rope.gameObject.SetActive(false);
            }
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            SpawnPinata();
        }
    }
}
