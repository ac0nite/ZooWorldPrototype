#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;
using ZooWorld.Composition;
using ZooWorld.Core;
using ZooWorld.Definitions;
using ZooWorld.Interaction;
using ZooWorld.Systems;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class AnimalFactoryTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private IObjectResolver _container;
        private AnimalFactory _factory;
        private AnimalDefinition _definition;
        private float _previousFixedDeltaTime;

        [SetUp]
        public void SetUp()
        {
            _previousFixedDeltaTime = Time.fixedDeltaTime;
            Time.fixedDeltaTime = 0.25f;
            var builder = new ContainerBuilder();
            builder.Register<FoodChainPolicy>(Lifetime.Singleton).As<IAnimalInteractionPolicy>();
            builder.Register<AnimalEventBus>(Lifetime.Singleton);
            builder.Register<AnimalFactory>(Lifetime.Singleton);
            builder.Register<AnimalInteractionResolver>(Lifetime.Singleton);
            builder.Register<LifecycleProbe>(Lifetime.Singleton);
            _container = builder.Build();
            _factory = _container.Resolve<AnimalFactory>();
            var animal = AssetDatabase.LoadAssetAtPath<Animal>("Assets/Tests/PlayMode/Fixtures/PoolAnimal.prefab");
            Assert.That(animal, Is.Not.Null, "Fixture must be a saved prefab asset.");
            Assert.That(animal.gameObject.activeSelf, Is.True);
            _definition = Asset<AnimalDefinition>();
            var movement = Asset<LinearMovementProfile>();
            var category = Asset<InteractionCategory>();
            var interaction = Asset<InteractionProfile>();
            Set(category, "_id", property => property.stringValue = "fixture-category");
            Set(interaction, "_category", property => property.objectReferenceValue = category);
            Set(_definition, "_id", property => property.stringValue = "fixture-species");
            Set(_definition, "_prefab", property => property.objectReferenceValue = animal);
            Set(_definition, "_movementProfile", property => property.objectReferenceValue = movement);
            Set(_definition, "_interactionProfile", property => property.objectReferenceValue = interaction);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.fixedDeltaTime = _previousFixedDeltaTime;
            _factory.Dispose();
            _container.Dispose();
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.Destroy(_objects[i]);
            }

            _objects.Clear();
            yield return null;
        }

        [UnityTest, Description("Переиспользование сбрасывает ID, позу и скорости без повторной инъекции")]
        public IEnumerator ReuseResetsIdentityPoseAndVelocitiesWithoutReinjection()
        {
            Animal first = Rent(Vector3.zero, Vector3.right);
            var probe = first.GetComponent<PoolTestMotor>();
            Assert.That(probe.InitializedBeforeEnable, Is.True);
            Assert.That(probe.EnableCount, Is.EqualTo(1));
            Assert.That(probe.Dependency, Is.SameAs(_container.Resolve<LifecycleProbe>()));
            Assert.That(_factory.ActiveCount, Is.EqualTo(1));
            long oldId = first.ActivationId;
            Assert.That(oldId, Is.GreaterThan(0));
            var body = first.GetComponent<Rigidbody>();
            body.linearVelocity = Vector3.one * 7;
            body.angularVelocity = Vector3.one * 5;
            first.transform.rotation = Quaternion.Euler(20, 30, 40);
            first.transform.localScale = Vector3.one * 2;
            Assert.That(first.TryDie(), Is.True);
            _factory.Release(first, oldId);
            Assert.That(first.gameObject.activeSelf, Is.False);
            Assert.That(first.IsAlive, Is.False);
            Assert.That(first.ActivationId, Is.Zero);
            Assert.That(first.Definition, Is.Null);
            Assert.That(first.Interaction, Is.Null);
            Assert.That(_factory.ActiveCount, Is.Zero);
            Animal reused = Rent(new Vector3(5, 0, 2), Vector3.forward);
            Assert.That(reused, Is.SameAs(first));
            Assert.That(reused.ActivationId, Is.GreaterThan(oldId));
            Assert.That(reused.IsAlive, Is.True);
            Assert.That(reused.transform.position, Is.EqualTo(new Vector3(5, 0, 2)));
            Assert.That(Quaternion.Angle(reused.transform.rotation, Quaternion.identity), Is.LessThan(0.00001f));
            Assert.That(reused.Definition, Is.SameAs(_definition));
            Assert.That(reused.transform.localScale, Is.EqualTo(_definition.Prefab.transform.localScale));
            Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(body.angularVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(probe.InjectionCount, Is.EqualTo(1));
            Assert.That(probe.EnableCount, Is.EqualTo(2));
            Assert.That(probe.InitializedBeforeEnable, Is.True);
            yield return null;
        }

        [UnityTest, Description("Дубль и устаревший возврат не снимают текущую активацию")]
        public IEnumerator DuplicateAndStaleReleaseCannotRemoveCurrentActivation()
        {
            Animal animal = Rent(Vector3.zero, Vector3.right);
            long oldId = animal.ActivationId;
            _factory.Release(animal, oldId);
            _factory.Release(animal, oldId);
            Assert.That(_factory.ActiveCount, Is.Zero);
            Animal reused = Rent(Vector3.zero, Vector3.forward);
            _factory.Release(reused, oldId);
            Assert.That(_factory.ActiveCount, Is.EqualTo(1));
            Assert.That(reused.gameObject.activeSelf, Is.True);
            Assert.That(reused.IsAlive, Is.True);
            yield return null;
        }

        [UnityTest, Description("Переиспользованный prefab берёт новый профиль и стопит тики после смерти")]
        public IEnumerator ReusedPrefabUsesNewMovementProfileAndStopsTickingAfterDeath()
        {
            Animal animal = Rent(Vector3.zero, Vector3.right);
            yield return new WaitForFixedUpdate();
            Assert.That(animal.GetComponent<PoolTestMotor>().Velocity, Is.EqualTo(Vector3.right * 3));
            _factory.Release(animal, animal.ActivationId);
            var replacement = Asset<AnimalDefinition>();
            var jump = Asset<JumpMovementProfile>();
            Set(replacement, "_id", property => property.stringValue = "replacement");
            Set(replacement, "_prefab", property => property.objectReferenceValue = _definition.Prefab);
            Set(replacement, "_movementProfile", property => property.objectReferenceValue = jump);
            Set(replacement, "_interactionProfile", property => property.objectReferenceValue = _definition.InteractionProfile);
            Animal reused = _factory.Rent(replacement, Vector3.zero, Vector3.forward);
            Assert.That(reused, Is.SameAs(animal));
            yield return new WaitForFixedUpdate();
            Assert.That(reused.GetComponent<PoolTestMotor>().Velocity, Is.EqualTo(Vector3.zero));
            Assert.That(reused.TryDie(), Is.True);
            var linear = Asset<LinearMovementProfile>();
            _factory.Release(reused, reused.ActivationId);
            Set(replacement, "_movementProfile", property => property.objectReferenceValue = linear);
            Animal final = _factory.Rent(replacement, Vector3.zero, Vector3.forward);
            Assert.That(final, Is.SameAs(animal));
            Assert.That(final.TryDie(), Is.True);
            yield return new WaitForFixedUpdate();
            Assert.That(final.GetComponent<PoolTestMotor>().Velocity, Is.EqualTo(Vector3.zero));
        }

        [UnityTest, Description("Dispose уничтожает активные и пассивные экземпляры и отклоняет выдачу")]
        public IEnumerator DisposeDestroysActiveAndInactiveInstancesAndRejectsNewRent()
        {
            Animal active = Rent(Vector3.zero, Vector3.right);
            Animal inactive = Rent(Vector3.forward * 10, Vector3.forward);
            Assert.That(inactive, Is.Not.SameAs(active));
            _factory.Release(inactive, inactive.ActivationId);
            _factory.Dispose();
            _factory.Dispose();
            yield return null;
            Assert.That(active == null, Is.True);
            Assert.That(inactive == null, Is.True);
            Assert.That(_factory.ActiveCount, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => _factory.Rent(_definition, Vector3.zero, Vector3.right));
        }

        [UnityTest, Description("Пауза прыжка при повторной выдаче стартует с полного интервала")]
        public IEnumerator RecycledJumpWaitStartsFromFullIntervalInsteadOfPreviousRemainder()
        {
            var jump = Asset<JumpMovementProfile>();
            Set(jump, "_waitInterval", property => property.floatValue = 1);
            Set(_definition, "_movementProfile", property => property.objectReferenceValue = jump);
            Animal animal = Rent(Vector3.zero, Vector3.right);
            for (var i = 0; i < 3; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(animal.GetComponent<PoolTestMotor>().Velocity, Is.EqualTo(Vector3.zero));
            _factory.Release(animal, animal.ActivationId);
            Animal reused = Rent(Vector3.zero, Vector3.forward);
            Assert.That(reused, Is.SameAs(animal));
            for (var i = 0; i < 3; i++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(reused.GetComponent<PoolTestMotor>().Velocity, Is.EqualTo(Vector3.zero));
            }

            yield return new WaitForFixedUpdate();
            Vector3 velocity = reused.GetComponent<PoolTestMotor>().Velocity;
            Assert.That(velocity.x, Is.Zero);
            Assert.That(velocity.y, Is.GreaterThan(0));
            Assert.That(velocity.z, Is.GreaterThan(0));
        }

        [UnityTest, Description("Животные с общим профилем имеют независимые таймеры и направления")]
        public IEnumerator SharedProfileAnimalsHaveIndependentJumpTimersAndDirections()
        {
            var jump = Asset<JumpMovementProfile>();
            Set(jump, "_waitInterval", property => property.floatValue = 1);
            Set(_definition, "_movementProfile", property => property.objectReferenceValue = jump);
            Animal first = Rent(Vector3.zero, Vector3.right);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Animal second = Rent(Vector3.forward * 10, Vector3.forward);
            yield return new WaitForFixedUpdate();
            Assert.That(first.GetComponent<PoolTestMotor>().Velocity, Is.EqualTo(Vector3.zero));
            Assert.That(second.GetComponent<PoolTestMotor>().Velocity, Is.EqualTo(Vector3.zero));
            yield return new WaitForFixedUpdate();
            Assert.That(first.GetComponent<PoolTestMotor>().Velocity.x, Is.GreaterThan(0));
            Assert.That(first.GetComponent<PoolTestMotor>().Velocity.z, Is.Zero);
            Assert.That(second.GetComponent<PoolTestMotor>().Velocity, Is.EqualTo(Vector3.zero));
            yield return new WaitForFixedUpdate();
            Assert.That(second.GetComponent<PoolTestMotor>().Velocity, Is.EqualTo(Vector3.zero));
            yield return new WaitForFixedUpdate();
            Assert.That(second.GetComponent<PoolTestMotor>().Velocity.x, Is.Zero);
            Assert.That(second.GetComponent<PoolTestMotor>().Velocity.z, Is.GreaterThan(0));
        }

        [UnityTest, Description("Снимок взаимодействия неизменяем и пересобирается при повторной выдаче")]
        public IEnumerator InteractionSnapshotIsImmutableAndRebuiltOnReuse()
        {
            Animal animal = Rent(Vector3.zero, Vector3.right);
            var previous = animal.Interaction;
            Assert.That(previous.CategoryId, Is.EqualTo("fixture-category"));
            Set(_definition.InteractionProfile.Category, "_id", property => property.stringValue = "new-category");
            Assert.That(previous.CategoryId, Is.EqualTo("fixture-category"));
            _factory.Release(animal, animal.ActivationId);
            Animal reused = Rent(Vector3.zero, Vector3.forward);
            Assert.That(reused, Is.SameAs(animal));
            Assert.That(reused.Interaction, Is.Not.SameAs(previous));
            Assert.That(reused.Interaction.CategoryId, Is.EqualTo("new-category"));
            yield return null;
        }

        [UnityTest, Description("Разрушение контейнера уничтожает фабрику и её экземпляры")]
        public IEnumerator ContainerDisposalOwnsFactoryAndDestroysItsInstances()
        {
            Animal active = Rent(Vector3.zero, Vector3.right);
            Animal inactive = Rent(Vector3.forward * 10, Vector3.forward);
            _factory.Release(inactive, inactive.ActivationId);
            _container.Dispose();
            yield return null;
            Assert.That(active == null, Is.True);
            Assert.That(inactive == null, Is.True);
            Assert.That(_factory.ActiveCount, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => _factory.Rent(_definition, Vector3.zero, Vector3.right));
        }

        [UnityTest, Description("Выданное животное смотрит по начальному направлению")]
        public IEnumerator RentFacesInitialDirection()
        {
            Animal animal = Rent(Vector3.zero, Vector3.right);
            Assert.That(Vector3.Angle(animal.transform.forward, Vector3.right), Is.LessThan(0.00001f));
            _factory.Release(animal, animal.ActivationId);
            var diagonal = new Vector3(1, 0, 1).normalized;
            Animal reused = Rent(Vector3.zero, diagonal);
            Assert.That(reused, Is.SameAs(animal));
            Assert.That(Vector3.Angle(reused.transform.forward, diagonal), Is.LessThan(0.00001f));
            yield return null;
        }

        [UnityTest, Description("Пулы prefab'ов изолированы по каждому ассету")]
        public IEnumerator PrefabPoolsAreIsolatedPerPrefabAsset()
        {
            var secondPrefab = AssetDatabase.LoadAssetAtPath<Animal>(
                "Assets/Tests/PlayMode/Fixtures/PoolAnimalSecond.prefab");
            Assert.That(secondPrefab, Is.Not.Null, "Second fixture must be a saved prefab asset.");
            Assert.That(secondPrefab, Is.Not.SameAs(_definition.Prefab));
            var secondDefinition = Asset<AnimalDefinition>();
            Set(secondDefinition, "_id", property => property.stringValue = "second-fixture");
            Set(secondDefinition, "_prefab", property => property.objectReferenceValue = secondPrefab);
            Set(secondDefinition, "_movementProfile",
                property => property.objectReferenceValue = _definition.MovementProfile);
            Set(secondDefinition, "_interactionProfile",
                property => property.objectReferenceValue = _definition.InteractionProfile);
            Animal first = Rent(Vector3.zero, Vector3.right);
            _factory.Release(first, first.ActivationId);
            Animal second = _factory.Rent(secondDefinition, Vector3.zero, Vector3.forward);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(_factory.ActiveCount, Is.EqualTo(1));
            _factory.Release(second, second.ActivationId);
            Animal firstAgain = Rent(Vector3.zero, Vector3.forward);
            Assert.That(firstAgain, Is.SameAs(first));
            yield return null;
        }

        [UnityTest, Description("Возврат внешне уничтоженного животного корректно чистит счётчик")]
        public IEnumerator ReleaseOfExternallyDestroyedActiveAnimalStillCleansActiveCount()
        {
            Animal animal = Rent(Vector3.zero, Vector3.right);
            long activationId = animal.ActivationId;
            Object.DestroyImmediate(animal.gameObject);
            _factory.Release(animal, activationId);
            Assert.That(_factory.ActiveCount, Is.Zero);
            yield return null;
        }

        [UnityTest, Description("Возврат утилизирует стратегию активации, новая выдача создаёт новую")]
        public IEnumerator ReleaseDisposesPerActivationStrategyAndFreshRentCreatesNewOne()
        {
            var recording = Asset<RecordingMovementProfile>();
            Set(_definition, "_movementProfile", property => property.objectReferenceValue = recording);
            Animal animal = Rent(Vector3.zero, Vector3.right);
            RecordingMovementProfile.RecordingStrategy first = recording.LastStrategy;
            Assert.That(first, Is.Not.Null);
            _factory.Release(animal, animal.ActivationId);
            Assert.That(first.Disposed, Is.True);
            yield return new WaitForFixedUpdate();
            Assert.That(first.Ticks, Is.Zero);
            Animal reused = Rent(Vector3.zero, Vector3.forward);
            Assert.That(reused, Is.SameAs(animal));
            Assert.That(recording.LastStrategy, Is.Not.SameAs(first));
            Assert.That(recording.LastStrategy.Disposed, Is.False);
        }

        [UnityTest, Description("Невалидная выдача не течёт счётчиком, чужие фабрики не возвращают животных")]
        public IEnumerator InvalidRentDoesNotLeakActiveCountAndOtherFactoriesCannotReleaseAnimals()
        {
            Assert.Throws<ArgumentException>(() => _factory.Rent(_definition, Vector3.zero, Vector3.zero));
            Assert.Throws<ArgumentException>(() => _factory.Rent(_definition, Vector3.zero, Vector3.up));
            Assert.Throws<ArgumentException>(
                () => _factory.Rent(_definition, new Vector3(float.NaN, 0, 0), Vector3.right));
            Assert.That(_factory.ActiveCount, Is.Zero);
            Animal animal = Rent(Vector3.zero, Vector3.right);
            using (var other = new AnimalFactory(_container))
            {
                other.Release(animal, animal.ActivationId);
                Assert.That(other.ActiveCount, Is.Zero);
                Assert.That(_factory.ActiveCount, Is.EqualTo(1));
                Assert.That(animal.IsAlive, Is.True);
            }

            yield return null;
        }

        [UnityTest, Description("Разрушение скоупа уничтожает активных и pooled животных")]
        public IEnumerator GameScopeDisposalDestroysActiveAndPooledAnimals()
        {
            var root = new GameObject("Factory Scope Fixture");
            root.SetActive(false);
            _objects.Add(root);
            GameLifetimeScope scope;
            using (LifetimeScope.Enqueue(builder => builder.Register<LifecycleProbe>(Lifetime.Singleton)))
            {
                scope = root.AddComponent<GameLifetimeScope>();
                root.SetActive(true);
            }

            var factory = scope.Container.Resolve<AnimalFactory>();
            Animal active = factory.Rent(_definition, Vector3.zero, Vector3.right);
            Animal inactive = factory.Rent(_definition, Vector3.forward * 10, Vector3.forward);
            factory.Release(inactive, inactive.ActivationId);
            Object.Destroy(root);
            yield return null;
            yield return null;
            Assert.That(active == null, Is.True);
            Assert.That(inactive == null, Is.True);
            Assert.That(factory.ActiveCount, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => factory.Rent(_definition, Vector3.zero, Vector3.right));
        }

        [UnityTest, Description("Неудачная активация возвращает экземпляр без утечки счётчика")]
        public IEnumerator FailedActivationReturnsInstanceWithoutReinjectionOrActiveCountLeak()
        {
            Vector3 previousGravity = Physics.gravity;
            try
            {
                Physics.gravity = Vector3.zero;
                Assert.Throws<ArgumentOutOfRangeException>(() => _factory.Rent(_definition, Vector3.zero, Vector3.right));
                Assert.That(_factory.ActiveCount, Is.Zero);
            }
            finally
            {
                Physics.gravity = previousGravity;
            }

            Animal animal = Rent(Vector3.zero, Vector3.right);
            Assert.That(animal.GetComponent<PoolTestMotor>().InjectionCount, Is.EqualTo(1));
            Assert.That(animal.GetComponent<PoolTestMotor>().EnableCount, Is.EqualTo(1));
            Assert.That(_factory.ActiveCount, Is.EqualTo(1));
            yield return null;
        }

        private Animal Rent(Vector3 position, Vector3 direction)
        {
            Animal animal = _factory.Rent(_definition, position, direction);
            Assert.That(animal, Is.Not.Null, "Factory must return an initialized instance.");
            return animal;
        }

        private T Asset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _objects.Add(asset);
            return asset;
        }

        private static void Set(Object target, string field, Action<SerializedProperty> configure)
        {
            var serialized = new SerializedObject(target);
            configure(serialized.FindProperty(field));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
