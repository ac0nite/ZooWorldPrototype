using UnityEngine;

namespace ZooWorld.Physics
{
    public interface IAnimalMotor
    {
        Vector3 Position { get; }
        Vector3 Velocity { get; set; }
        bool IsGrounded { get; }
    }
}
