using UnityEngine;

namespace ZooWorld.Motion
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class RigidbodyAnimalMotor : MonoBehaviour, IAnimalMotor
    {
        [SerializeField, Min(0)] private float _maxGroundedVerticalSpeed = 0.1f;
        [SerializeField, Min(0)] private float _groundProbeExtraDistance = 0.05f;
        [SerializeField, Range(0, 1)] private float _minGroundNormalY = 0.5f;

        private Rigidbody _body;
        private Collider _collider;

        public Vector3 Position => _body.position;
        public Vector3 Velocity
        {
            get => _body.linearVelocity;
            set => _body.linearVelocity = value;
        }

        public bool IsGrounded
        {
            get
            {
                if (Velocity.y > _maxGroundedVerticalSpeed)
                {
                    return false;
                }

                var bounds = _collider.bounds;
                return gameObject.scene.GetPhysicsScene().Raycast(bounds.center, Vector3.down,
                    out var hit, bounds.extents.y + _groundProbeExtraDistance, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore) && hit.normal.y > _minGroundNormalY && hit.rigidbody == null;
            }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
        }
    }
}
