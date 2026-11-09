using System;
using UnityEngine;

namespace ZooWorld.Definitions
{
    [CreateAssetMenu(menuName = "Zoo World/Simulation Settings")]
    public sealed class SimulationSettings : ScriptableObject
    {
        [SerializeField] private float _minSpawnInterval = 1;
        [SerializeField] private float _maxSpawnInterval = 2;
        [SerializeField] private int _liveCap = 100;
        [SerializeField, Tooltip("0 = random per session")] private uint _seed;
        [SerializeField] private float _spawnInset = 0.05f;
        [SerializeField] private float _spawnHeight = 1;
        [SerializeField] private float _spawnClearance = 1.1f;
        [SerializeField, Min(1)] private int _maxSpawnsPerTick = 3;
        [SerializeField, Min(1)] private int _maxSpawnPositionAttempts = 4;
        [SerializeField, Min(0.1f)] private float _diagnosticsReportInterval = 1;

        private uint? _resolvedSeed;

        public float MinSpawnInterval => _minSpawnInterval;
        public float MaxSpawnInterval => _maxSpawnInterval;
        public int LiveCap => _liveCap;
        public uint Seed => _seed;
        public float SpawnInset => _spawnInset;
        public float SpawnHeight => _spawnHeight;
        public float SpawnClearance => _spawnClearance;
        public int MaxSpawnsPerTick => _maxSpawnsPerTick;
        public int MaxSpawnPositionAttempts => _maxSpawnPositionAttempts;
        public float DiagnosticsReportInterval => _diagnosticsReportInterval;

        public uint EffectiveSeed =>
            _resolvedSeed ??= _seed != 0 ? _seed : (uint)UnityEngine.Random.Range(1, int.MaxValue);

        [ContextMenu("Validate")]
        public void Validate()
        {
            if (float.IsNaN(_minSpawnInterval) || float.IsInfinity(_minSpawnInterval) || _minSpawnInterval <= 0)
            {
                throw new InvalidOperationException($"Settings '{name}' require a finite positive minimum spawn interval.");
            }

            if (float.IsNaN(_maxSpawnInterval) || float.IsInfinity(_maxSpawnInterval) ||
                _maxSpawnInterval < _minSpawnInterval)
            {
                throw new InvalidOperationException(
                    $"Settings '{name}' require a maximum spawn interval not smaller than the minimum.");
            }

            if (_liveCap <= 0)
            {
                throw new InvalidOperationException($"Settings '{name}' require a positive live cap.");
            }

            if (float.IsNaN(_spawnInset) || float.IsInfinity(_spawnInset) ||
                _spawnInset < 0 || _spawnInset >= 0.5f)
            {
                throw new InvalidOperationException($"Settings '{name}' require a spawn inset within [0, 0.5).");
            }

            if (float.IsNaN(_spawnHeight) || float.IsInfinity(_spawnHeight) || _spawnHeight < 0)
            {
                throw new InvalidOperationException($"Settings '{name}' require a finite non-negative spawn height.");
            }

            if (float.IsNaN(_spawnClearance) || float.IsInfinity(_spawnClearance) || _spawnClearance < 0)
            {
                throw new InvalidOperationException($"Settings '{name}' require a finite non-negative spawn clearance.");
            }

            if (_maxSpawnsPerTick <= 0)
            {
                throw new InvalidOperationException($"Settings '{name}' require a positive max spawns per tick.");
            }

            if (_maxSpawnPositionAttempts <= 0)
            {
                throw new InvalidOperationException($"Settings '{name}' require positive max spawn position attempts.");
            }

            if (float.IsNaN(_diagnosticsReportInterval) || float.IsInfinity(_diagnosticsReportInterval) ||
                _diagnosticsReportInterval <= 0)
            {
                throw new InvalidOperationException($"Settings '{name}' require a positive diagnostics report interval.");
            }
        }
    }
}
