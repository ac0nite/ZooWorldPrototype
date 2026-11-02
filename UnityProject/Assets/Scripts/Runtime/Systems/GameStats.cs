using System;
using VContainer;
using ZooWorld.Core;
using ZooWorld.Interaction;

namespace ZooWorld.Systems
{
    public sealed class GameStats : IDisposable
    {
        private readonly AnimalEventBus _events;

        public int PreyDeaths { get; private set; }
        public int PredatorDeaths { get; private set; }
        public event Action Changed;

        [Inject]
        public GameStats(AnimalEventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _events.Died += OnAnimalDied;
        }

        public void Dispose()
        {
            _events.Died -= OnAnimalDied;
        }

        private void OnAnimalDied(AnimalDied payload)
        {
            if (payload.Role == AnimalRole.Prey)
            {
                PreyDeaths++;
            }
            else
            {
                PredatorDeaths++;
            }

            Changed?.Invoke();
        }
    }
}
