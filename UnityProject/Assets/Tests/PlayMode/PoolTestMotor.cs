using UnityEngine;
using VContainer;
using ZooWorld.Physics;

namespace ZooWorld.Tests
{
    public sealed class PoolTestMotor : MonoBehaviour, IAnimalMotor
    {
        public int InjectionCount { get; private set; }
        public bool InitializedBeforeEnable { get; private set; }
        public Vector3 Position => transform.position;
        public Vector3 Velocity
        {
            get => GetComponent<Rigidbody>().linearVelocity;
            set => GetComponent<Rigidbody>().linearVelocity = value;
        }
        public bool IsGrounded => true;

        [Inject]
        public void Construct()
        {
            InjectionCount++;
        }

        private void OnEnable()
        {
            var animal = GetComponent<ZooWorld.Core.Animal>();
            InitializedBeforeEnable = InjectionCount == 1 && animal.IsAlive && animal.ActivationId > 0;
        }
    }
}
