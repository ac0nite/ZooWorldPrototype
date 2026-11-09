using UnityEngine;

namespace ZooWorld.Motion
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class RigidbodyAnimalMotor : MonoBehaviour, IAnimalMotor
    {
        private const float MaxGroundedVerticalSpeed = 0.1f;
        private const float GroundProbeExtraDistance = 0.05f;
        private const float MinGroundNormalY = 0.5f;
        private Rigidbody _body;
        private Collider _collider;

        public Vector3 Position => Body.position;
        public Vector3 Velocity
        {
            get => Body.linearVelocity;
            set
            {
                if (_body != null)
                {
                    _body.linearVelocity = value;
                }
            }
        }

        public bool IsGrounded
        {
            get
            {
                if (Velocity.y > MaxGroundedVerticalSpeed)
                {
                    return false;
                }

                Bounds bounds = _collider.bounds;
                return gameObject.scene.GetPhysicsScene().Raycast(bounds.center, Vector3.down,
                    out RaycastHit hit, bounds.extents.y + GroundProbeExtraDistance, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore) && hit.normal.y > MinGroundNormalY && hit.rigidbody == null;
            }
        }

        private Rigidbody Body
        {
            get
            {
                if (_body == null)
                {
                    CacheComponents();
                }

                return _body;
            }
        }

        private void Awake()
        {
            CacheComponents();
        }

        private void CacheComponents()
        {
            _body = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
        }
    }
}
