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
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            Bounds = bounds;
        }

        public int ActiveCount => _active.Count;
        public ArenaBounds Bounds { get; }

        public Animal Rent(AnimalDefinition definition, Vector3 position, Vector3 direction)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AnimalFactory));

            if (definition == null) throw new ArgumentNullException(nameof(definition));

            definition.Validate();
            direction = MovementContext.NormalizeDirection(direction);
            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
                throw new ArgumentException("Spawn position must be finite.", nameof(position));

            var activationId = checked(++_nextActivationId);
            var pool = GetPool(definition.Prefab);
            var animal = pool.Get();
            try
            {
                var context = new MovementContext(animal.GetComponent<IAnimalMotor>(), direction, -Physics.gravity.y,
                    Bounds);
                var transform = animal.transform;
                transform.SetParent(null);
                transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction, Vector3.up));
                transform.localScale = definition.Prefab.transform.localScale;
                var body = animal.GetComponent<Rigidbody>();
                body.position = position;
                body.rotation = transform.rotation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                animal.Activate(activationId, definition, context);
                _active.Add(animal, (pool, activationId));
                animal.gameObject.SetActive(true);
                return animal;
            }
            catch
            {
                _active.Remove(animal);
                animal.Deactivate();
                pool.Release(animal);
                throw;
            }
        }

        public bool Release(Animal animal, long activationId)
        {
            if (_disposed || ReferenceEquals(animal, null)) return false;

            if (!_active.TryGetValue(animal, out var activation) || activation.Id != activationId ||
                (animal != null && animal.ActivationId != activationId))
                return false;

            _active.Remove(animal);
            if (animal == null) return true;

            animal.Deactivate();
            activation.Pool.Release(animal);
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            foreach (var animal in _active.Keys) DestroyAnimal(animal);

            _active.Clear();
            foreach (var pool in _pools.Values) pool.Dispose();

            _pools.Clear();
            if (_poolRoot != null) Object.Destroy(_poolRoot);
        }

        private ObjectPool<Animal> GetPool(Animal prefab)
        {
            if (_pools.TryGetValue(prefab, out var pool)) return pool;

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
            if (animal == null) return;

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