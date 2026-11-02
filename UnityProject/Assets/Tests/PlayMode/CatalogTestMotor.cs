using UnityEngine;
using ZooWorld.Motion;

namespace ZooWorld.Tests
{
    public sealed class CatalogTestMotor : MonoBehaviour, IAnimalMotor
    {
        public Vector3 Position => transform.position;
        public Vector3 Velocity { get; set; }
        public bool IsGrounded => true;
    }
}
