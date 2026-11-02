using UnityEngine;

namespace ZooWorld.Physics
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class RigidbodyAnimalMotor : MonoBehaviour, IAnimalMotor
    {
        private Rigidbody _body;
        private Collider _collider;

        public Vector3 Position => Body.position;
        public Vector3 Velocity
        {
            get => Body.linearVelocity;
            set => Body.linearVelocity = value;
        }

        public bool IsGrounded
        {
            get
            {
                if (Velocity.y > 0.1f)
                {
                    return false;
                }

                Bounds bounds = _collider.bounds;
                return gameObject.scene.GetPhysicsScene().Raycast(bounds.center, Vector3.down,
                    out RaycastHit hit, bounds.extents.y + 0.05f, UnityEngine.Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore) && hit.normal.y > 0.5f && hit.rigidbody == null;
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
