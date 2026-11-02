using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Physics;

namespace ZooWorld.Movement
{
    public sealed class JumpMovementStrategy : IMovementStrategy
    {
        private readonly IAnimalMotor _motor;
        private readonly float _verticalSpeed;
        private readonly float _horizontalSpeed;
        private readonly float _waitInterval;
        private Vector3 _direction;
        private float _remainingWait;
        private bool _launched;
        private bool _leftGround;
        private bool _disposed;

        internal JumpMovementStrategy(MovementContext context, float distance, float height, float waitInterval)
        {
            _motor = context.Motor;
            _direction = context.InitialDirection;
            _verticalSpeed = Mathf.Sqrt(2 * context.Gravity * height);
            _horizontalSpeed = distance / (2 * _verticalSpeed / context.Gravity);
            _waitInterval = waitInterval;
            _remainingWait = waitInterval;
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (_disposed)
            {
                return;
            }

            if (_launched)
            {
                if (!_motor.IsGrounded)
                {
                    _leftGround = true;
                }
                else if (_leftGround)
                {
                    _launched = false;
                    _leftGround = false;
                    _remainingWait = _waitInterval;
                    _motor.Velocity = new Vector3(0, _motor.Velocity.y, 0);
                }

                return;
            }

            if (!_motor.IsGrounded)
            {
                return;
            }

            _remainingWait -= fixedDeltaTime;
            if (_remainingWait > 0)
            {
                return;
            }

            Vector3 velocity = _direction * _horizontalSpeed;
            velocity.y = _verticalSpeed;
            _motor.Velocity = velocity;
            _launched = true;
        }

        public void SteerInward(Vector3 direction)
        {
            if (_disposed)
            {
                return;
            }

            _direction = MovementContext.NormalizeDirection(direction);
            if (!_launched)
            {
                return;
            }

            Vector3 velocity = _direction * _horizontalSpeed;
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
