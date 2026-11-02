using UnityEngine;

namespace ZooWorld.Motion
{
    public interface IAnimalMotor
    {
        public Vector3 Position { get; }
        public Vector3 Velocity { get; set; }
        public bool IsGrounded { get; }
    }
}
