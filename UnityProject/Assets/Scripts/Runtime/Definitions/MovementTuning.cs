using System;
using UnityEngine;

namespace ZooWorld.Definitions
{
    [CreateAssetMenu(menuName = "Zoo World/Movement Tuning")]
    public sealed class MovementTuning : ScriptableObject
    {
        [SerializeField, Min(0)] private float _minPlanarSpeed = 0.05f;
        [SerializeField, Min(0)] private float _stopRecoverySeconds = 0.3f;
        [SerializeField, Min(0)] private float _reboundHoldSeconds = 0.3f;
        [SerializeField, Min(0)] private float _speedTolerance = 0.01f;
        [SerializeField, Range(0, 1)] private float _inwardDotThreshold = 0.5f;
        [SerializeField, Min(0)] private float _minAirborneSeconds = 0.1f;
        [SerializeField, Min(0)] private float _maxLandingVelocityY = 0.1f;

        private static MovementTuning _defaults;

        internal static MovementTuning Defaults =>
            _defaults != null ? _defaults : _defaults = CreateInstance<MovementTuning>();

        public float MinPlanarSpeed => _minPlanarSpeed;
        public float StopRecoverySeconds => _stopRecoverySeconds;
        public float ReboundHoldSeconds => _reboundHoldSeconds;
        public float SpeedTolerance => _speedTolerance;
        public float InwardDotThreshold => _inwardDotThreshold;
        public float MinAirborneSeconds => _minAirborneSeconds;
        public float MaxLandingVelocityY => _maxLandingVelocityY;

        public void Validate()
        {
            RequireFiniteNonNegative(_minPlanarSpeed, nameof(_minPlanarSpeed));
            RequireFiniteNonNegative(_stopRecoverySeconds, nameof(_stopRecoverySeconds));
            RequireFiniteNonNegative(_reboundHoldSeconds, nameof(_reboundHoldSeconds));
            RequireFiniteNonNegative(_speedTolerance, nameof(_speedTolerance));
            RequireFiniteNonNegative(_minAirborneSeconds, nameof(_minAirborneSeconds));
            RequireFiniteNonNegative(_maxLandingVelocityY, nameof(_maxLandingVelocityY));
            if (float.IsNaN(_inwardDotThreshold) || float.IsInfinity(_inwardDotThreshold) ||
                _inwardDotThreshold < 0 || _inwardDotThreshold > 1)
            {
                throw new InvalidOperationException(
                    $"Movement tuning '{name}' requires an inward dot threshold within [0, 1].");
            }
        }

        private void RequireFiniteNonNegative(float value, string parameter)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
            {
                throw new InvalidOperationException(
                    $"Movement tuning '{name}' requires a finite non-negative {parameter}.");
            }
        }
    }
}
