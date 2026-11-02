using System;
using UnityEngine;

namespace ZooWorld.Movement
{
    public interface IMovementStrategy : IDisposable
    {
        public void FixedTick(float fixedDeltaTime);
        public void SteerInward(Vector3 direction);
    }
}
