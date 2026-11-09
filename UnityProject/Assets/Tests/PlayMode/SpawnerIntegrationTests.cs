#if UNITY_EDITOR
using System.Collections;
using static ZooWorld.Tests.SerializedFields;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using ZooWorld.Composition;
using ZooWorld.Core;
using ZooWorld.Definitions;
using ZooWorld.Interaction;
using ZooWorld.Systems;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class SpawnerIntegrationTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private IObjectResolver _container;
        private AnimalFactory _factory;
        private ArenaBounds _bounds;
        private AnimalCatalog _catalog;
        private AnimalDefinition _fixture;
        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            var cameraObject = new GameObject("Spawner test camera");
            _objects.Add(cameraObject);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.Euler(90, 0, 0));
            _camera.orthographic = true;
            _camera.orthographicSize = 5;
            _camera.aspect = 1;
            _bounds = new ArenaBounds(_camera);
            var builder = new ContainerBuilder();
            builder.Register<FoodChainPolicy>(Lifetime.Singleton).As<IAnimalInteractionPolicy>();
            builder.Register<AnimalEventBus>(Lifetime.Singleton);
            builder.Register<AnimalInteractionResolver>(Lifetime.Singleton);
            builder.Register<LifecycleProbe>(Lifetime.Singleton);
            ArenaBounds bounds = _bounds;
            builder.Register(resolver => new AnimalFactory(resolver, bounds), Lifetime.Singleton);
            _container = builder.Build();
            _factory = _container.Resolve<AnimalFactory>();
            _catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(
                "Assets/Settings/AnimalDefinitions/AnimalCatalog.asset");
            Assert.That(_catalog, Is.Not.Null, "The saved production catalog must exist.");
            var animal = AssetDatabase.LoadAssetAtPath<Animal>(
                "Assets/Tests/PlayMode/Fixtures/PoolAnimal.prefab");
            Assert.That(animal, Is.Not.Null, "Fixture must be a saved prefab asset.");
            _fixture = ScriptableObject.CreateInstance<AnimalDefinition>();
            _objects.Add(_fixture);
            var movement = ScriptableObject.CreateInstance<LinearMovementProfile>();
            _objects.Add(movement);
            var category = ScriptableObject.CreateInstance<InteractionCategory>();
            _objects.Add(category);
            var interaction = ScriptableObject.CreateInstance<InteractionProfile>();
            _objects.Add(interaction);
            Set(category, "_id", property => property.stringValue = "spawner-fixture-category");
            Set(interaction, "_category", property => property.objectReferenceValue = category);
            Set(_fixture, "_id", property => property.stringValue = "spawner-fixture-species");
            Set(_fixture, "_prefab", property => property.objectReferenceValue = animal);
            Set(_fixture, "_movementProfile", property => property.objectReferenceValue = movement);
            Set(_fixture, "_interactionProfile", property => property.objectReferenceValue = interaction);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _factory.Dispose();
            _container.Dispose();
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.Destroy(_objects[i]);
            }

            _objects.Clear();
            yield return null;
        }

        [UnityTest, Description("Спавнер с задержкой 1-2 с выдаёт живых животных внутри арены")]
        public IEnumerator ScheduledSpawnsRentLiveAnimalsInsideTheArena()
        {
            SimulationSettings settings = Settings(min: 0.5f, max: 0.5f, cap: 10);
            var spawner = new AnimalSpawner(_catalog, settings, _factory, _bounds);
            spawner.Start();
            Assert.That(_factory.ActiveCount, Is.Zero);
            spawner.Advance(0.49f);
            Assert.That(_factory.ActiveCount, Is.Zero,
                "No spawn may happen before the first scheduled interval elapses.");
            spawner.Advance(0.05f);
            Assert.That(_factory.ActiveCount, Is.EqualTo(1));
            Animal spawned = SingleActiveAnimal();
            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.IsAlive, Is.True);
            Assert.That(spawned.gameObject.activeSelf, Is.True);
            Assert.That(spawned.transform.position.y, Is.EqualTo(settings.SpawnHeight).Within(0.001f));
            Assert.That(_bounds.TryGetInwardDirection(spawned.transform.position, out _), Is.False,
                "The spawned animal must start inside the camera footprint.");
            spawner.Advance(0.5f);
            Assert.That(_factory.ActiveCount, Is.EqualTo(2));
            yield return null;
            foreach (Animal animal in ActiveAnimals())
            {
                Assert.That(animal.IsAlive, Is.True);
                Assert.That(animal.gameObject.activeSelf, Is.True);
            }
        }

        [UnityTest, Description("При 100 активных спавн пропускается и возобновляется после освобождения места")]
        public IEnumerator LiveCapSkipsSpawnAtOneHundredActiveAndResumesAfterRelease()
        {
            SimulationSettings settings = Settings(min: 0.5f, max: 0.5f, cap: 100);
            var spawner = new AnimalSpawner(_catalog, settings, _factory, _bounds);
            spawner.Start();
            var rented = new List<Animal>();
            for (var i = 0; i < 100; i++)
            {
                var position = new Vector3(i % 10 * 2f, 0.5f, 100 + i / 10 * 2f);
                Assert.That(_factory.TryRent(_fixture, position, Vector3.forward, out Animal rentedAnimal),
                    Is.True);
                rented.Add(rentedAnimal);
            }

            Assert.That(_factory.ActiveCount, Is.EqualTo(100));
            spawner.Advance(10f);
            Assert.That(_factory.ActiveCount, Is.EqualTo(100),
                "At the live cap every scheduled spawn must be skipped.");
            Animal released = rented[0];
            Assert.That(_factory.Release(released, released.ActivationId), Is.True);
            Assert.That(_factory.ActiveCount, Is.EqualTo(99));
            spawner.Advance(0.6f);
            Assert.That(_factory.ActiveCount, Is.EqualTo(100),
                "Once capacity frees, the next scheduled interval spawns again.");
            yield return null;
        }

        [UnityTest, Description("Спавнер тикает через entry point скоупа и останавливается при его разрушении")]
        public IEnumerator SpawnerTicksThroughScopeEntryPointAndStopsOnDisposal()
        {
            SimulationSettings settings = Settings(min: 0.15f, max: 0.15f, cap: 100);
            var root = new GameObject("Spawner Scope Fixture");
            _objects.Add(root);
            root.SetActive(false);
            var scope = root.AddComponent<GameLifetimeScope>();
            var serialized = new SerializedObject(scope);
            serialized.FindProperty("_arenaCamera").objectReferenceValue = _camera;
            serialized.FindProperty("_catalog").objectReferenceValue = _catalog;
            serialized.FindProperty("_settings").objectReferenceValue = settings;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            var factory = scope.Container.Resolve<AnimalFactory>();
            var stats = scope.Container.Resolve<GameStats>();
            Assert.That(scope.Container.Resolve<AnimalSpawner>(), Is.Not.Null);
            float waited = 0;
            while (factory.ActiveCount == 0 && waited < 5f)
            {
                yield return null;
                waited += Time.deltaTime;
            }

            Assert.That(factory.ActiveCount, Is.GreaterThan(0),
                "The scope entry point must tick the spawner and produce animals.");
            Animal[] spawned = ActiveAnimals();
            Assert.That(spawned, Is.Not.Empty);
            foreach (Animal animal in spawned)
            {
                Assert.That(animal.IsAlive, Is.True);
                Assert.That(_bounds.TryGetInwardDirection(animal.transform.position, out _), Is.False);
            }
            Object.Destroy(root);
            _objects.Remove(root);
            yield return null;
            yield return null;
            Assert.That(factory.ActiveCount, Is.Zero);
            Assert.That(Object.FindObjectsByType<Animal>(FindObjectsSortMode.None), Is.Empty,
                "Scope disposal must stop spawning and destroy active animals.");
            Assert.That(stats.PreyDeaths + stats.PredatorDeaths, Is.EqualTo(0),
                "Scope cleanup must not count spawned animals as deaths.");
        }

        [UnityTest, Description("Скоуп с одной из пары каталог/настройки не конфигурирует симуляцию")]
        public IEnumerator ScopeWithOnlyOneOfCatalogOrSettingsFailsToConfigure()
        {
            for (var caseIndex = 0; caseIndex < 2; caseIndex++)
            {
                var root = new GameObject("Partial Scope Fixture");
                _objects.Add(root);
                root.SetActive(false);
                var scope = root.AddComponent<GameLifetimeScope>();
                var serialized = new SerializedObject(scope);
                serialized.FindProperty("_arenaCamera").objectReferenceValue = _camera;
                serialized.FindProperty("_catalog").objectReferenceValue =
                    caseIndex == 0 ? _catalog : null;
                serialized.FindProperty("_settings").objectReferenceValue =
                    caseIndex == 1 ? Settings(1, 2, 10) : null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                LogAssert.Expect(LogType.Exception, new Regex(
                    "InvalidOperationException: GameLifetimeScope requires both an animal catalog"));
                root.SetActive(true);
                yield return null;
            }
        }

        private static Animal SingleActiveAnimal()
        {
            Animal[] animals = Object.FindObjectsByType<Animal>(FindObjectsSortMode.None);
            return animals.Length == 1 ? animals[0] : null;
        }

        private static Animal[] ActiveAnimals()
        {
            return Object.FindObjectsByType<Animal>(FindObjectsSortMode.None);
        }

        private SimulationSettings Settings(float min, float max, int cap)
        {
            var settings = ScriptableObject.CreateInstance<SimulationSettings>();
            _objects.Add(settings);
            Set(settings, "_minSpawnInterval", property => property.floatValue = min);
            Set(settings, "_maxSpawnInterval", property => property.floatValue = max);
            Set(settings, "_liveCap", property => property.intValue = cap);
            return settings;
        }

    }
}
#endif
