using System;
using VContainer.Unity;

namespace ZooWorld.Tests
{
    public sealed class LifecycleProbe : IStartable, ITickable, IDisposable
    {
        public int Starts { get; private set; }
        public int Ticks { get; private set; }
        public int Disposals { get; private set; }
        public void Start() => Starts++;
        public void Tick() => Ticks++;
        public void Dispose() => Disposals++;
    }
}
