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
        [SerializeField] private uint _seed;
        [SerializeField] private float _spawnInset = 0.05f;
        [SerializeField] private float _spawnHeight = 1;

        public float MinSpawnInterval => _minSpawnInterval;
        public float MaxSpawnInterval => _maxSpawnInterval;
        public int LiveCap => _liveCap;
        public uint Seed => _seed;
        public float SpawnInset => _spawnInset;
        public float SpawnHeight => _spawnHeight;

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
        }
    }
}
