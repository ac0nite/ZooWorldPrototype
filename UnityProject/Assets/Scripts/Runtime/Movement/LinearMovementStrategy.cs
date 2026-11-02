using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Physics;

namespace ZooWorld.Movement
{
    public sealed class LinearMovementStrategy : IMovementStrategy
    {
        private readonly IAnimalMotor _motor;
        private readonly float _speed;
        private Vector3 _direction;
        private bool _started;
        private bool _disposed;

        internal LinearMovementStrategy(MovementContext context, float speed)
        {
            _motor = context.Motor;
            _direction = context.InitialDirection;
            _speed = speed;
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (_disposed || _started)
            {
                return;
            }

            _started = true;
            ApplyDirection();
        }

        public void SteerInward(Vector3 direction)
        {
            if (_disposed)
            {
                return;
            }

            _direction = MovementContext.NormalizeDirection(direction);
            if (_started)
            {
                ApplyDirection();
            }
        }

        private void ApplyDirection()
        {
            Vector3 velocity = _direction * _speed;
            velocity.y = _motor.Velocity.y;
            _motor.Velocity = velocity;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _motor.Velocity = Vector3.zero;
        }
    }
}
