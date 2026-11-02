using System;
using UnityEngine;

namespace ZooWorld.Movement
{
    public interface IMovementStrategy : IDisposable
    {
        void FixedTick(float fixedDeltaTime);
        void SteerInward(Vector3 direction);
    }
}
