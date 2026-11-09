using System;
using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Movement;

namespace ZooWorld.Definitions
{
    public abstract class MovementProfile : ScriptableObject
    {
        [SerializeField] private MovementTuning _tuning;

        protected MovementTuning Tuning => _tuning != null ? _tuning : MovementTuning.Defaults;

        public abstract void Validate();
        public abstract IMovementStrategy CreateStrategy(MovementContext context);

        protected void ValidateTuning()
        {
            if (_tuning != null)
            {
                _tuning.Validate();
            }
        }

        protected void RequirePositive(float value, string parameter)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
            {
                throw new InvalidOperationException($"Movement profile '{name}' requires positive finite {parameter}.");
            }
        }
    }
}
