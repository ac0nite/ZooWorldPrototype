using UnityEngine;
using VContainer;
using ZooWorld.Physics;

namespace ZooWorld.Tests
{
    public sealed class PoolTestMotor : MonoBehaviour, IAnimalMotor
    {
        public int InjectionCount { get; private set; }
        public int EnableCount { get; private set; }
        public bool InitializedBeforeEnable { get; private set; }
        public LifecycleProbe Dependency { get; private set; }
        public Vector3 Position => transform.position;
        public Vector3 Velocity
        {
            get => GetComponent<Rigidbody>().linearVelocity;
            set => GetComponent<Rigidbody>().linearVelocity = value;
        }
        public bool IsGrounded => true;

        [Inject]
        public void Construct(LifecycleProbe dependency)
        {
            Dependency = dependency;
            InjectionCount++;
        }

        private void OnEnable()
        {
            EnableCount++;
            var animal = GetComponent<ZooWorld.Core.Animal>();
            InitializedBeforeEnable = Dependency != null && InjectionCount == 1 && animal.IsAlive &&
                animal.ActivationId > 0 && animal.Definition != null && animal.Interaction != null;
        }
    }
}
