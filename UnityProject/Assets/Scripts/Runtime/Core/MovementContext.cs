using System;
using UnityEngine;
using ZooWorld.Physics;

namespace ZooWorld.Core
{
    public sealed class MovementContext
    {
        public IAnimalMotor Motor { get; }
        public Vector3 InitialDirection { get; }
        public float Gravity { get; }

        public MovementContext(IAnimalMotor motor, Vector3 initialDirection, float gravity)
        {
            Motor = motor ?? throw new ArgumentNullException(nameof(motor));
            InitialDirection = NormalizeDirection(initialDirection);
            if (float.IsNaN(gravity) || float.IsInfinity(gravity) || gravity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gravity));
            }

            Gravity = gravity;
        }

        internal static Vector3 NormalizeDirection(Vector3 direction)
        {
            direction.y = 0;
            if (float.IsNaN(direction.x) || float.IsInfinity(direction.x) ||
                float.IsNaN(direction.z) || float.IsInfinity(direction.z) || direction.sqrMagnitude < 0.000001f)
            {
                throw new ArgumentException("Movement requires a finite non-zero planar direction.", nameof(direction));
            }

            return direction.normalized;
        }
    }
}
