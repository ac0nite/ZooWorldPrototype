using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using ZooWorld.Definitions;

namespace ZooWorld.Systems
{
    public sealed class AnimalSpawner : IStartable, ITickable
    {
        private readonly AnimalCatalog _catalog;
        private readonly SimulationSettings _settings;
        private readonly ISpawnTarget _spawnTarget;
        private readonly ArenaBounds _bounds;
        private readonly System.Random _random;
        private float _elapsed;
        private float _nextInterval;

        public AnimalSpawner(AnimalCatalog catalog, SimulationSettings settings,
            ISpawnTarget spawnTarget, ArenaBounds bounds)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _spawnTarget = spawnTarget ?? throw new ArgumentNullException(nameof(spawnTarget));
            _bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
            _random = new System.Random(unchecked((int)_settings.Seed));
            _nextInterval = SampleInterval();
        }

        public void Start()
        {
            _settings.Validate();
            _catalog.Validate();
        }

        public void Tick()
        {
            Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            _elapsed += deltaTime;
            while (_elapsed >= _nextInterval)
            {
                _elapsed -= _nextInterval;
                _nextInterval = SampleInterval();
                TrySpawn();
            }
        }

        private void TrySpawn()
        {
            if (_spawnTarget.ActiveCount >= _settings.LiveCap)
            {
                return;
            }

            AnimalDefinition definition = PickDefinition();
            float inset = _settings.SpawnInset;
            float range = 1 - inset * 2;
            var normalized = new Vector2(
                inset + (float)_random.NextDouble() * range,
                inset + (float)_random.NextDouble() * range);
            Vector3 position = _bounds.GetPoint(normalized);
            position.y += _settings.SpawnHeight;
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            var direction = new Vector3((float)Math.Cos(angle), 0, (float)Math.Sin(angle));
            _spawnTarget.Rent(definition, position, direction);
        }

        private AnimalDefinition PickDefinition()
        {
            IReadOnlyList<AnimalDefinition> definitions = _catalog.Definitions;
            double total = 0;
            for (var i = 0; i < definitions.Count; i++)
            {
                total += definitions[i].SpawnWeight;
            }

            double roll = _random.NextDouble() * total;
            for (var i = 0; i < definitions.Count; i++)
            {
                roll -= definitions[i].SpawnWeight;
                if (roll < 0)
                {
                    return definitions[i];
                }
            }

            return definitions[definitions.Count - 1];
        }

        private float SampleInterval()
        {
            return _settings.MinSpawnInterval +
                (float)_random.NextDouble() * (_settings.MaxSpawnInterval - _settings.MinSpawnInterval);
        }
    }
}
