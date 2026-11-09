using System;
using UnityEngine;
using VContainer.Unity;
using ZooWorld.Definitions;
using Random = System.Random;

namespace ZooWorld.Systems
{
    public sealed class AnimalSpawner : IStartable, ITickable
    {
        private readonly AnimalCatalog _catalog;
        private readonly SimulationSettings _settings;
        private readonly ISpawnTarget _spawnTarget;
        private readonly ArenaBounds _bounds;
        private readonly Random _random;
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
            _random = new Random(unchecked((int)_settings.EffectiveSeed));
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
                if (++spawns >= _settings.MaxSpawnsPerTick)
                {
                    _elapsed = 0;
                    break;
                }
            }
        }

        private void TrySpawn()
        {
            if (_spawnTarget.ActiveCount >= _settings.LiveCap) return;

            var definition = PickDefinition();
            var inset = _settings.SpawnInset;
            var range = 1 - inset * 2;
            Vector3 position;
            for (var attempt = 0;; attempt++)
            {
                var normalized = new Vector2(
                    inset + (float)_random.NextDouble() * range,
                    inset + (float)_random.NextDouble() * range);
                if (!_bounds.TryGetPoint(normalized, out position))
                {
                    if (!_boundsProjectionFailed)
                    {
                        _boundsProjectionFailed = true;
                        Debug.LogError(
                            "AnimalSpawner: the arena camera does not project onto the ground plane; spawning is skipped.");
                    }

                    return;
                }

                position.y += _settings.SpawnHeight;
                if (attempt == _settings.MaxSpawnPositionAttempts - 1 ||
                    _spawnTarget.IsPositionFree(position, _settings.SpawnClearance))
                    break;
            }

            var angle = (float)(_random.NextDouble() * Math.PI * 2);
            var direction = new Vector3((float)Math.Cos(angle), 0, (float)Math.Sin(angle));
            _spawnTarget.TryRent(definition, position, direction, out _);
        }

        private AnimalDefinition PickDefinition()
        {
            var definitions = _catalog.Definitions;
            double total = 0;
            for (var i = 0; i < definitions.Count; i++)
            {
                total += definitions[i].SpawnWeight;
            }

            var roll = _random.NextDouble() * total;
            for (var i = 0; i < definitions.Count; i++)
            {
                roll -= definitions[i].SpawnWeight;
                if (roll < 0) return definitions[i];
            }

            return definitions[^1];
        }

        private float SampleInterval()
        {
            return _settings.MinSpawnInterval +
                   (float)_random.NextDouble() * (_settings.MaxSpawnInterval - _settings.MinSpawnInterval);
        }
    }
}