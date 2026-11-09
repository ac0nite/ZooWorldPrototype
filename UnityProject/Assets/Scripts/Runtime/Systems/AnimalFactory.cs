using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;
using ZooWorld.Core;
using ZooWorld.Definitions;
using ZooWorld.Motion;
using Object = UnityEngine.Object;

namespace ZooWorld.Systems
{
    public sealed class AnimalFactory : IDisposable, ISpawnTarget
    {
        private readonly IObjectResolver _resolver;
        private readonly Dictionary<Animal, ObjectPool<Animal>> _pools = new();
        private readonly Dictionary<Animal, (ObjectPool<Animal> Pool, long Id)> _active = new();
        private GameObject _poolRoot;
        private long _nextActivationId;
        private bool _disposed;

        [Inject]
        public AnimalFactory(IObjectResolver resolver) : this(resolver, null)
        {
        }

        public AnimalFactory(IObjectResolver resolver, ArenaBounds bounds)
        {
            _resolver = resolver;
            Bounds = bounds;
        }

        public int ActiveCount => _active.Count;
        public ArenaBounds Bounds { get; }

        public bool TryRent(AnimalDefinition definition, Vector3 position, Vector3 direction, out Animal animal)
        {
            animal = null;
            if (_disposed || definition == null ||
                !IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
            {
                return false;
            }

            var activationId = checked(++_nextActivationId);
            var pool = GetPool(definition.Prefab);
            var candidate = pool.Get();
            if (!MovementContext.TryCreate(candidate.GetComponent<IAnimalMotor>(), direction,
                    -Physics.gravity.y, Bounds, out MovementContext context))
            {
                pool.Release(candidate);
                return false;
            }

            var transform = candidate.transform;
            transform.SetParent(null);
            transform.SetPositionAndRotation(position,
                Quaternion.LookRotation(context.InitialDirection, Vector3.up));
            transform.localScale = definition.Prefab.transform.localScale;
            var body = candidate.GetComponent<Rigidbody>();
            body.position = position;
            body.rotation = transform.rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            if (!candidate.TryActivate(activationId, definition, context))
            {
                candidate.Deactivate();
                pool.Release(candidate);
                return false;
            }

            _active.Add(candidate, (pool, activationId));
            candidate.gameObject.SetActive(true);
            animal = candidate;
            return true;
        }

        public bool Release(Animal animal, long activationId)
        {
            if (_disposed || ReferenceEquals(animal, null))
            {
                return false;
            }

            if (!_active.TryGetValue(animal, out var activation) || activation.Id != activationId ||
                (animal != null && animal.ActivationId != activationId))
            {
                return false;
            }

            _active.Remove(animal);
            if (animal == null)
            {
                return true;
            }

            animal.Deactivate();
            activation.Pool.Release(animal);
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (Animal animal in _active.Keys)
            {
                DestroyAnimal(animal);
            }

            _active.Clear();
            foreach (ObjectPool<Animal> pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
            if (_poolRoot != null)
            {
                Object.Destroy(_poolRoot);
            }
        }

        public bool IsPositionFree(Vector3 position, float clearance)
        {
            float sqrClearance = clearance * clearance;
            foreach (Animal animal in _active.Keys)
            {
                if (animal == null)
                {
                    continue;
                }

                Vector3 other = animal.transform.position;
                float dx = other.x - position.x;
                float dz = other.z - position.z;
                if (dx * dx + dz * dz < sqrClearance)
                {
                    return false;
                }
            }

            return true;
        }

        private ObjectPool<Animal> GetPool(Animal prefab)
        {
            if (_pools.TryGetValue(prefab, out var pool))
            {
                return pool;
            }

            pool = new ObjectPool<Animal>(
                () => CreateAnimal(prefab),
                actionOnRelease: animal =>
                {
                    var body = animal.GetComponent<Rigidbody>();
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    animal.gameObject.SetActive(false);
                    animal.transform.SetParent(EnsurePoolRoot().transform);
                },
                actionOnDestroy: DestroyAnimal,
                maxSize: int.MaxValue);
            _pools.Add(prefab, pool);
            return pool;
        }

        private Animal CreateAnimal(Animal prefab)
        {
            var animal = _resolver.Instantiate(prefab, EnsurePoolRoot().transform);
            animal.gameObject.SetActive(false);
            return animal;
        }

        private GameObject EnsurePoolRoot()
        {
            if (_poolRoot == null)
            {
                _poolRoot = new GameObject("Animal Pool");
                _poolRoot.SetActive(false);
            }

            return _poolRoot;
        }

        private static void DestroyAnimal(Animal animal)
        {
            if (animal == null)
            {
                return;
            }

            animal.Deactivate();
            animal.gameObject.SetActive(false);
            Object.Destroy(animal.gameObject);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}