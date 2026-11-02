using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Physics;

namespace ZooWorld.Movement
{
    public sealed class LinearMovementStrategy : IMovementStrategy
    {
        readonly IAnimalMotor motor;
        readonly float speed;
        Vector3 direction;
        bool started;
        bool disposed;

        internal LinearMovementStrategy(MovementContext context, float speed)
        {
            motor = context.Motor;
            direction = context.InitialDirection;
            this.speed = speed;
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (disposed || started) return;
            started = true;
            ApplyDirection();
        }

        public void SteerInward(Vector3 direction)
        {
            if (disposed) return;
            this.direction = MovementContext.NormalizeDirection(direction);
            if (started) ApplyDirection();
        }

        void ApplyDirection()
        {
            var velocity = direction * speed;
            velocity.y = motor.Velocity.y;
            motor.Velocity = velocity;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            motor.Velocity = Vector3.zero;
        }
    }
}
