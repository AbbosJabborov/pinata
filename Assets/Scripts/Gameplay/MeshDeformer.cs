using UnityEngine;

namespace Pinata.Gameplay
{
    [RequireComponent(typeof(MeshFilter))]
    public class MeshDeformer : MonoBehaviour
    {
        [Header("Deformation Settings")]
        [SerializeField] private float springForce = 20f;
        [SerializeField] private float damping = 5.5f;
        [SerializeField] private float permanentDentRatio = 0.72f;
        [SerializeField] private float maxDentDepth = 0.20f;

        private Mesh _deformedMesh;
        private Vector3[] _pristineVertices;
        private Vector3[] _targetVertices;
        private Vector3[] _displacedVertices;
        private Vector3[] _vertexVelocities;
        private bool _isDirty = false;

        public bool IsDeformed => _isDirty;

        private void Awake()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;

            // Instantiate unique mesh instance so we don't modify the shared asset
            _deformedMesh = filter.mesh;
            _pristineVertices = _deformedMesh.vertices;

            int count = _pristineVertices.Length;
            _targetVertices = new Vector3[count];
            _displacedVertices = new Vector3[count];
            _vertexVelocities = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                _targetVertices[i] = _pristineVertices[i];
                _displacedVertices[i] = _pristineVertices[i];
                _vertexVelocities[i] = Vector3.zero;
            }
        }

        public void Deform(Vector3 worldPoint, Vector3 worldForce, float radius = 0.55f, float forceScale = 0.04f)
        {
            if (_deformedMesh == null || _displacedVertices == null) return;

            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            Vector3 localForce = transform.InverseTransformDirection(worldForce) * forceScale;

            // Cap local force to avoid inside-out mesh inversion
            if (localForce.magnitude > maxDentDepth)
            {
                localForce = localForce.normalized * maxDentDepth;
            }

            for (int i = 0; i < _displacedVertices.Length; i++)
            {
                float dist = Vector3.Distance(_displacedVertices[i], localPoint);
                if (dist < radius)
                {
                    // Smooth cubic falloff curve
                    float falloff = 1f - (dist / radius);
                    float smooth = falloff * falloff * (3f - 2f * falloff);

                    Vector3 delta = localForce * smooth;

                    // Permanent papier-mâché dent updates baseline
                    _targetVertices[i] += delta * permanentDentRatio;

                    // Clamp to maximum dent depth from pristine shape
                    Vector3 totalDent = _targetVertices[i] - _pristineVertices[i];
                    if (totalDent.magnitude > maxDentDepth)
                    {
                        _targetVertices[i] = _pristineVertices[i] + totalDent.normalized * maxDentDepth;
                    }

                    // Dynamic elastic impulse
                    _vertexVelocities[i] += delta * (1f - permanentDentRatio) * 18f;
                }
            }

            _isDirty = true;
        }

        private void Update()
        {
            if (!_isDirty || _deformedMesh == null) return;

            float dt = Time.deltaTime;
            float maxMovement = 0f;

            for (int i = 0; i < _displacedVertices.Length; i++)
            {
                Vector3 velocity = _vertexVelocities[i];
                Vector3 displacement = _displacedVertices[i] - _targetVertices[i];

                // Damped harmonic oscillation toward target dented shape
                velocity -= displacement * springForce * dt;
                velocity *= Mathf.Clamp01(1f - damping * dt);

                _vertexVelocities[i] = velocity;
                _displacedVertices[i] += velocity * dt;

                float moveMag = velocity.sqrMagnitude + displacement.sqrMagnitude;
                if (moveMag > maxMovement) maxMovement = moveMag;
            }

            _deformedMesh.vertices = _displacedVertices;
            _deformedMesh.RecalculateNormals();
            _deformedMesh.RecalculateBounds();

            // Settle down once vertex velocities and offsets are micro-small
            if (maxMovement < 0.00004f)
            {
                for (int i = 0; i < _displacedVertices.Length; i++)
                {
                    _displacedVertices[i] = _targetVertices[i];
                    _vertexVelocities[i] = Vector3.zero;
                }
                _deformedMesh.vertices = _displacedVertices;
                _deformedMesh.RecalculateNormals();
                _deformedMesh.RecalculateBounds();
                _isDirty = false;
            }
        }

        public void ResetDeformation()
        {
            if (_deformedMesh == null || _pristineVertices == null) return;

            for (int i = 0; i < _pristineVertices.Length; i++)
            {
                _targetVertices[i] = _pristineVertices[i];
                _displacedVertices[i] = _pristineVertices[i];
                _vertexVelocities[i] = Vector3.zero;
            }

            _deformedMesh.vertices = _pristineVertices;
            _deformedMesh.RecalculateNormals();
            _deformedMesh.RecalculateBounds();
            _isDirty = false;
        }
    }
}
