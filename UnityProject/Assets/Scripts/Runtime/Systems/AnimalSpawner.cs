using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using ZooWorld.Definitions;

namespace ZooWorld.Systems
{
    public sealed class AnimalSpawner : IStartable, ITickable
    {
        private const int MaxSpawnsPerTick = 3;
        private const int MaxSpawnPositionAttempts = 4;

        private readonly AnimalCatalog _catalog;
        private readonly SimulationSettings _settings;
        private readonly ISpawnTarget _spawnTarget;
        private readonly ArenaBounds _bounds;
        private readonly System.Random _random;
        private float _elapsed;
        private float _nextInterval;
        private bool _boundsProjectionFailed;

        public AnimalSpawner(AnimalCatalog catalog, SimulationSettings settings,
            ISpawnTarget spawnTarget, ArenaBounds bounds)
        {
            _catalog = catalog;
            _settings = settings;
            _spawnTarget = spawnTarget;
            _bounds = bounds;
            _random = new System.Random(unchecked((int)_settings.EffectiveSeed));
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

        internal void Advance(float deltaTime)
        {
            _elapsed += deltaTime;
            var spawns = 0;
            while (_nextInterval > 0 && _elapsed >= _nextInterval)
            {
                _elapsed -= _nextInterval;
                _nextInterval = SampleInterval();
                TrySpawn();
                if (++spawns >= MaxSpawnsPerTick)
                {
                    _elapsed = 0;
                    break;
                }
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
            Vector3 position = Vector3.zero;
            for (var attempt = 0; ; attempt++)
            {
                var normalized = new Vector2(
                    inset + (float)_random.NextDouble() * range,
                    inset + (float)_random.NextDouble() * range);
                if (!_bounds.TryGetPoint(normalized, out position))
                {
                    if (!_boundsProjectionFailed)
                    {
                        _boundsProjectionFailed = true;
                        Debug.LogError("AnimalSpawner: the arena camera does not project onto the ground plane; spawning is skipped.");
                    }

                    return;
                }

                position.y += _settings.SpawnHeight;
                if (attempt == MaxSpawnPositionAttempts - 1 ||
                    _spawnTarget.IsPositionFree(position, _settings.SpawnClearance))
                {
                    break;
                }
            }

            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            var direction = new Vector3((float)Math.Cos(angle), 0, (float)Math.Sin(angle));
            _spawnTarget.TryRent(definition, position, direction, out _);
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
