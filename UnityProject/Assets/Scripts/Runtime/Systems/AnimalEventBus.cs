using System;
using ZooWorld.Interaction;

namespace ZooWorld.Systems
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class AnimalEventBus
    {
        public event Action<AnimalDied> Died;
        public event Action<AnimalConsumed> Consumed;

        internal void PublishDied(AnimalDied payload)
        {
            Died?.Invoke(payload);
        }

        internal void PublishConsumed(AnimalConsumed payload)
        {
            Consumed?.Invoke(payload);
        }
    }
}
