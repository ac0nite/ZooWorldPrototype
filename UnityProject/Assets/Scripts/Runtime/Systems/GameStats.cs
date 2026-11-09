using System;
using UnityEngine;
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
            _events = events;
            _events.Died += OnAnimalDied;
        }

        public void Dispose()
        {
            _events.Died -= OnAnimalDied;
        }

        private void OnAnimalDied(AnimalDied payload)
        {
            switch (payload.Role)
            {
                case AnimalRole.Prey:
                    PreyDeaths++;
                    break;
                case AnimalRole.Predator:
                    PredatorDeaths++;
                    break;
                default:
                    Debug.LogError($"GameStats: unknown animal role {payload.Role}; death not counted.");
                    return;
            }

            Changed?.Invoke();
        }
    }
}
