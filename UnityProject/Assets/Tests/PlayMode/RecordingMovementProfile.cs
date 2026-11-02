using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Definitions;
using ZooWorld.Movement;

namespace ZooWorld.Tests
{
    public sealed class RecordingMovementProfile : MovementProfile
    {
        public RecordingStrategy LastStrategy { get; private set; }

        public override void Validate()
        {
        }

        public override IMovementStrategy CreateStrategy(MovementContext context)
        {
            LastStrategy = new RecordingStrategy();
            return LastStrategy;
        }

        public sealed class RecordingStrategy : IMovementStrategy
        {
            public int Ticks { get; private set; }
            public bool Disposed { get; private set; }

            public void FixedTick(float fixedDeltaTime)
            {
                Ticks++;
            }

            public void SteerInward(Vector3 direction)
            {
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
