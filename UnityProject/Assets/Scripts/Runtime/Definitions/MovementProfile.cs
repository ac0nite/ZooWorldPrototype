using System;
using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Movement;

namespace ZooWorld.Definitions
{
    public abstract class MovementProfile : ScriptableObject
    {
        public abstract void Validate();
        public abstract IMovementStrategy CreateStrategy(MovementContext context);

        protected void RequirePositive(float value, string parameter)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
            {
                throw new InvalidOperationException($"Movement profile '{name}' requires positive finite {parameter}.");
            }
        }
    }
}
