using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;
using ZooWorld.Core;
using ZooWorld.Definitions;
using ZooWorld.Physics;
using Object = UnityEngine.Object;

namespace ZooWorld.Systems
{
    public sealed class AnimalFactory : IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly Dictionary<Animal, ObjectPool<Animal>> _pools = new Dictionary<Animal, ObjectPool<Animal>>();
        private readonly Dictionary<Animal, (ObjectPool<Animal> Pool, long Id)> _active =
            new Dictionary<Animal, (ObjectPool<Animal> Pool, long Id)>();
        private GameObject _poolRoot;
        private long _nextActivationId;
        private bool _disposed;

        public AnimalFactory(IObjectResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public int ActiveCount => _active.Count;

        public Animal Rent(AnimalDefinition definition, Vector3 position, Vector3 direction)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AnimalFactory));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            definition.Validate();
            direction = MovementContext.NormalizeDirection(direction);
            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
            {
                throw new ArgumentException("Spawn position must be finite.", nameof(position));
            }

            long activationId = checked(++_nextActivationId);
            ObjectPool<Animal> pool = GetPool(definition.Prefab);
            Animal animal = pool.Get();
            try
            {
                var context = new MovementContext(animal.GetComponent<IAnimalMotor>(), direction, -UnityEngine.Physics.gravity.y);
                Transform transform = animal.transform;
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

        public void Release(Animal animal, long activationId)
        {
            if (_disposed || ReferenceEquals(animal, null))
            {
                return;
            }

            if (!_active.TryGetValue(animal, out var activation) || activation.Id != activationId ||
                (animal != null && animal.ActivationId != activationId))
            {
                return;
            }

            _active.Remove(animal);
            if (animal == null)
            {
                return;
            }

            animal.Deactivate();
            activation.Pool.Release(animal);
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

        private ObjectPool<Animal> GetPool(Animal prefab)
        {
            if (_pools.TryGetValue(prefab, out ObjectPool<Animal> pool))
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
            Animal animal = _resolver.Instantiate(prefab, EnsurePoolRoot().transform);
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
