using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Motion;

namespace ZooWorld.Movement
{
    public sealed class LinearMovementStrategy : IMovementStrategy
    {
        private const float MinPlanarSpeedSqr = 0.0025f;
        private const float StopRecoverySeconds = 0.3f;
        private const float ReboundHoldSeconds = 0.3f;
        private const float SpeedTolerance = 0.01f;
        private const float InwardDotThreshold = 0.5f;

        private readonly IAnimalMotor _motor;
        private readonly float _speed;
        private Vector3 _direction;
        private bool _started;
        private float _stoppedTime;
        private float _deflectionTime;
        private bool _disposed;

        internal LinearMovementStrategy(MovementContext context, float speed)
        {
            _motor = context.Motor;
            _direction = context.InitialDirection;
            _speed = speed;
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (_disposed)
            {
                return;
            }

            if (!_started)
            {
                _started = true;
                ApplySpeed(_speed);
                return;
            }

            Vector3 planarVelocity = _motor.Velocity;
            planarVelocity.y = 0;
            float planarSpeed = planarVelocity.magnitude;
            if (planarSpeed * planarSpeed >= MinPlanarSpeedSqr)
            {
                _stoppedTime = 0;
                _direction = planarVelocity / planarSpeed;
                if (Mathf.Abs(planarSpeed - _speed) > SpeedTolerance)
                {
                    _deflectionTime += fixedDeltaTime;
                    if (_deflectionTime >= ReboundHoldSeconds)
                    {
                        ApplySpeed(Mathf.MoveTowards(planarSpeed, _speed, _speed * fixedDeltaTime));
                    }
                }
                else
                {
                    _deflectionTime = 0;
                }

                return;
            }

            _stoppedTime += fixedDeltaTime;
            _deflectionTime = 0;
            if (_stoppedTime >= StopRecoverySeconds)
            {
                ApplySpeed(_speed);
                _stoppedTime = 0;
            }
        }

        public void SteerInward(Vector3 direction)
        {
            if (_disposed)
            {
                return;
            }

            if (!MovementContext.TryNormalizeDirection(direction, out Vector3 normalized))
            {
                return;
            }

            _direction = normalized;
            if (!_started)
            {
                return;
            }

            Vector3 planarVelocity = _motor.Velocity;
            planarVelocity.y = 0;
            float planarSpeed = planarVelocity.magnitude;
            if (planarSpeed * planarSpeed < MinPlanarSpeedSqr)
            {
                ApplySpeed(_speed);
            }
            else if (Vector3.Dot(planarVelocity / planarSpeed, _direction) < InwardDotThreshold)
            {
                ApplySpeed(planarSpeed);
            }
        }

        private void ApplySpeed(float speed)
        {
            Vector3 velocity = _direction * speed;
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
