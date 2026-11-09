using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Motion;
using ZooWorld.Definitions;

namespace ZooWorld.Movement
{
    public sealed class LinearMovementStrategy : IMovementStrategy
    {
        private readonly IAnimalMotor _motor;
        private readonly float _speed;
        private readonly MovementTuning _tuning;
        private readonly float _minPlanarSpeedSqr;
        private Vector3 _direction;
        private bool _started;
        private float _stoppedTime;
        private float _deflectionTime;
        private bool _disposed;

        internal LinearMovementStrategy(MovementContext context, float speed, MovementTuning tuning)
        {
            _motor = context.Motor;
            _direction = context.InitialDirection;
            _speed = speed;
            _tuning = tuning;
            _minPlanarSpeedSqr = tuning.MinPlanarSpeed * tuning.MinPlanarSpeed;
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
            if (planarSpeed * planarSpeed >= _minPlanarSpeedSqr)
            {
                _stoppedTime = 0;
                _direction = planarVelocity / planarSpeed;
                if (Mathf.Abs(planarSpeed - _speed) > _tuning.SpeedTolerance)
                {
                    _deflectionTime += fixedDeltaTime;
                    if (_deflectionTime >= _tuning.ReboundHoldSeconds)
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
            if (_stoppedTime >= _tuning.StopRecoverySeconds)
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
            if (planarSpeed * planarSpeed < _minPlanarSpeedSqr)
            {
                ApplySpeed(_speed);
            }
            else if (Vector3.Dot(planarVelocity / planarSpeed, _direction) < _tuning.InwardDotThreshold)
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
