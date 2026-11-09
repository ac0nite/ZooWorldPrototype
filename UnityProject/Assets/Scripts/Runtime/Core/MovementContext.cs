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

        private MovementContext(IAnimalMotor motor, Vector3 initialDirection, float gravity, ArenaBounds bounds)
        {
            Motor = motor;
            InitialDirection = initialDirection;
            Gravity = gravity;
            Bounds = bounds;
        }

        public static bool TryCreate(IAnimalMotor motor, Vector3 initialDirection, float gravity,
            ArenaBounds bounds, out MovementContext context)
        {
            context = null;
            if (motor == null || float.IsNaN(gravity) || float.IsInfinity(gravity) || gravity <= 0 ||
                !TryNormalizeDirection(initialDirection, out Vector3 normalized))
            {
                return false;
            }

            context = new MovementContext(motor, normalized, gravity, bounds);
            return true;
        }

        internal static bool TryNormalizeDirection(Vector3 direction, out Vector3 normalized)
        {
            direction.y = 0;
            normalized = Vector3.zero;
            if (float.IsNaN(direction.x) || float.IsInfinity(direction.x) ||
                float.IsNaN(direction.z) || float.IsInfinity(direction.z) ||
                direction.sqrMagnitude < MinDirectionSqrMagnitude)
            {
                return false;
            }

            normalized = direction.normalized;
            return true;
        }
    }
}
