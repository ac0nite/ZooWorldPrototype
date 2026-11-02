using UnityEngine;

namespace ZooWorld.Physics
{
    public interface IAnimalMotor
    {
        public Vector3 Position { get; }
        public Vector3 Velocity { get; set; }
        public bool IsGrounded { get; }
    }
}
