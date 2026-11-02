using System;
using UnityEngine;
using ZooWorld.Motion;
using ZooWorld.Systems;

namespace ZooWorld.Core
{
    public sealed class MovementContext
    {
        private const float MinDirectionSqrMagnitude = 0.000001f;
        public IAnimalMotor Motor { get; }
        public Vector3 InitialDirection { get; }
        public float Gravity { get; }
        public ArenaBounds Bounds { get; }

        public MovementContext(IAnimalMotor motor, Vector3 initialDirection, float gravity, ArenaBounds bounds = null)
        {
            Motor = motor ?? throw new ArgumentNullException(nameof(motor));
            InitialDirection = NormalizeDirection(initialDirection);
            if (float.IsNaN(gravity) || float.IsInfinity(gravity) || gravity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gravity));
            }

            Gravity = gravity;
            Bounds = bounds;
        }

        internal static Vector3 NormalizeDirection(Vector3 direction)
        {
            direction.y = 0;
            if (float.IsNaN(direction.x) || float.IsInfinity(direction.x) ||
                float.IsNaN(direction.z) || float.IsInfinity(direction.z) || direction.sqrMagnitude < MinDirectionSqrMagnitude)
            {
                throw new ArgumentException("Movement requires a finite non-zero planar direction.", nameof(direction));
            }

            return direction.normalized;
        }
    }
}
