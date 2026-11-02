using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Definitions;
using ZooWorld.Systems;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class AnimalSpawnerTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private ArenaBounds _bounds;
        private FakeSpawnTarget _target;

        [SetUp]
        public void SetUp()
        {
            var cameraObject = new GameObject("Spawner test camera");
            _objects.Add(cameraObject);
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.Euler(90, 0, 0));
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.aspect = 1;
            _bounds = new ArenaBounds(camera);
            _target = new FakeSpawnTarget();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
        }

        [Test, Category("AC01")]
        public void ScheduledSpawnsAddOneAnimalEachAfterAnIntervalWithinRange()
        {
            AnimalSpawner spawner = Spawner(Catalog(Definition("frog", 1), Definition("snake", 1)));
            var gaps = new List<float>();
            float elapsed = 0;
            while (_target.Rents.Count < 5 && elapsed < 30)
            {
                spawner.Advance(0.05f);
                elapsed += 0.05f;
                if (_target.Rents.Count > gaps.Count)
                {
                    gaps.Add(elapsed);
                    elapsed = 0;
                }
            }

            Assert.That(_target.Rents, Has.Count.EqualTo(5));
            foreach (float gap in gaps)
            {
                Assert.That(gap, Is.InRange(0.99f, 2.1f),
                    "Each scheduled spawn must follow an interval inside the configured [1, 2] seconds.");
            }
        }

        [Test, Category("AC01")]
        public void FirstSpawnDoesNotHappenBeforeTheMinimumInterval()
        {
            AnimalSpawner spawner = Spawner(Catalog(Definition("frog", 1)));
            spawner.Advance(0.99f);
            Assert.That(_target.Rents, Is.Empty);
            spawner.Advance(0f);
            Assert.That(_target.Rents, Is.Empty);
        }

        [Test, Category("AC01")]
        public void SameSeedReproducesTheSameSpawnSequence()
        {
            AnimalCatalog catalog = Catalog(Definition("frog", 1), Definition("snake", 3));
            var first = new FakeSpawnTarget();
            var second = new FakeSpawnTarget();
            AnimalSpawner firstSpawner = Spawner(catalog, first, seed: 7);
            AnimalSpawner secondSpawner = Spawner(catalog, second, seed: 7);
            for (var i = 0; i < 40; i++)
            {
                firstSpawner.Advance(0.25f);
                secondSpawner.Advance(0.25f);
            }

            Assert.That(second.Rents, Has.Count.EqualTo(first.Rents.Count));
            Assert.That(first.Rents.Count, Is.GreaterThan(0));
            for (var i = 0; i < first.Rents.Count; i++)
            {
                Assert.That(second.Rents[i].Definition, Is.SameAs(first.Rents[i].Definition));
                Assert.That(second.Rents[i].Position, Is.EqualTo(first.Rents[i].Position));
                Assert.That(second.Rents[i].Direction, Is.EqualTo(first.Rents[i].Direction));
            }
        }

        [Test, Category("AC01")]
        public void DifferentSeedChangesTheSpawnSequence()
        {
            AnimalCatalog catalog = Catalog(Definition("frog", 1), Definition("snake", 1));
            var first = new FakeSpawnTarget();
            var second = new FakeSpawnTarget();
            AnimalSpawner firstSpawner = Spawner(catalog, first, seed: 7);
            AnimalSpawner secondSpawner = Spawner(catalog, second, seed: 8);
            for (var i = 0; i < 40; i++)
            {
                firstSpawner.Advance(0.25f);
                secondSpawner.Advance(0.25f);
            }

            Assert.That(first.Rents.Count, Is.GreaterThan(0));
            Assert.That(second.Rents.Count, Is.GreaterThan(0));
            bool differs = first.Rents.Count != second.Rents.Count;
            for (int i = 0, count = Math.Min(first.Rents.Count, second.Rents.Count); i < count; i++)
            {
                differs |= first.Rents[i].Definition != second.Rents[i].Definition ||
                    first.Rents[i].Position != second.Rents[i].Position ||
                    first.Rents[i].Direction != second.Rents[i].Direction;
            }

            Assert.That(differs, Is.True, "A different seed must produce a different spawn sequence.");
        }

        [Test, Category("AC01")]
        public void WeightedSelectionNeverPicksZeroWeightAndPrefersHeavierSpecies()
        {
            AnimalDefinition ignored = Definition("ignored", 0);
            AnimalDefinition light = Definition("light", 1);
            AnimalDefinition heavy = Definition("heavy", 3);
            AnimalSpawner spawner = Spawner(Catalog(ignored, light, heavy), cap: 500);
            for (var i = 0; i < 300 && _target.Rents.Count < 200; i++)
            {
                spawner.Advance(2f);
            }

            int lightCount = 0;
            int heavyCount = 0;
            foreach (RecordedRent rent in _target.Rents)
            {
                Assert.That(rent.Definition, Is.Not.SameAs(ignored));
                if (ReferenceEquals(rent.Definition, light))
                {
                    lightCount++;
                }
                else if (ReferenceEquals(rent.Definition, heavy))
                {
                    heavyCount++;
                }
            }

            Assert.That(lightCount + heavyCount, Is.EqualTo(200));
            Assert.That(heavyCount, Is.GreaterThan(lightCount),
                "A 3:1 spawn weight must favor the heavier species.");
        }

        [Test, Category("AC01")]
        public void SpawnPositionsStayInsideTheFootprintAndAboveGround()
        {
            AnimalSpawner spawner = Spawner(Catalog(Definition("frog", 1)), cap: 100);
            for (var i = 0; i < 80 && _target.Rents.Count < 50; i++)
            {
                spawner.Advance(2f);
            }

            foreach (RecordedRent rent in _target.Rents)
            {
                Vector3 position = rent.Position;
                Assert.That(position.x, Is.InRange(-5f, 5f));
                Assert.That(position.z, Is.InRange(-5f, 5f));
                Assert.That(position.y, Is.EqualTo(1f).Within(0.0001f),
                    "Spawns drop in from the configured spawn height above the ground plane.");
                Assert.That(_bounds.TryGetInwardDirection(position, out _), Is.False,
                    "Spawned animals must start inside the camera footprint.");
            }
        }

        [Test, Category("AC01")]
        public void SpawnDirectionsArePlanarUnitAndRandomized()
        {
            AnimalSpawner spawner = Spawner(Catalog(Definition("frog", 1)), cap: 100);
            for (var i = 0; i < 50 && _target.Rents.Count < 30; i++)
            {
                spawner.Advance(2f);
            }

            var directions = new HashSet<Vector3>();
            foreach (RecordedRent rent in _target.Rents)
            {
                Assert.That(rent.Direction.y, Is.EqualTo(0f));
                Assert.That(rent.Direction.magnitude, Is.EqualTo(1f).Within(0.00001f));
                directions.Add(rent.Direction);
            }

            Assert.That(directions.Count, Is.GreaterThan(1), "Spawn directions must be randomized.");
        }

        [Test, Category("AC10")]
        public void ScheduledSpawnIsSkippedWhileAtTheLiveCap()
        {
            AnimalSpawner spawner = Spawner(Catalog(Definition("frog", 1)), _target, cap: 3);
            _target.ActiveCount = 3;
            spawner.Advance(20f);
            Assert.That(_target.Rents, Is.Empty);
            _target.ActiveCount = 2;
            spawner.Advance(2f);
            Assert.That(_target.Rents, Has.Count.GreaterThanOrEqualTo(1));
        }

        [Test, Category("AC10")]
        public void ScheduledSpawnIsSkippedAtOneHundredActive()
        {
            AnimalSpawner spawner = Spawner(Catalog(Definition("frog", 1)), _target, cap: 100);
            _target.ActiveCount = 100;
            spawner.Advance(30f);
            Assert.That(_target.Rents, Is.Empty);
        }

        [Test, Category("AC10")]
        public void SpawningResumesAtTheNextScheduledIntervalAfterCapacityFrees()
        {
            AnimalSpawner spawner = Spawner(Catalog(Definition("frog", 1)), _target, cap: 2);
            _target.ActiveCount = 2;
            spawner.Advance(10f);
            Assert.That(_target.Rents, Is.Empty);
            _target.ActiveCount = 1;
            float waited = 0;
            while (_target.Rents.Count == 0 && waited < 5)
            {
                spawner.Advance(0.05f);
                waited += 0.05f;
            }

            Assert.That(_target.Rents, Has.Count.EqualTo(1));
            Assert.That(waited, Is.LessThanOrEqualTo(2.05f),
                "After capacity frees, spawning must resume at the next scheduled interval.");
        }

        [Test, Category("AC10")]
        public void StartRejectsInvalidSettingsAndCatalog()
        {
            AnimalCatalog empty = Asset<AnimalCatalog>();
            AnimalSpawner validCatalog = Spawner(empty);
            Assert.Throws<InvalidOperationException>(() => validCatalog.Start());
            SimulationSettings broken = Settings(min: 5, max: 1);
            var spawner = new AnimalSpawner(empty, broken, _target, _bounds);
            Assert.Throws<InvalidOperationException>(() => spawner.Start());
        }

        [Test, Category("AC01")]
        public void InvalidAdvanceAndNullDependenciesAreRejected()
        {
            AnimalSpawner spawner = Spawner(Catalog(Definition("frog", 1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => spawner.Advance(-0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => spawner.Advance(float.NaN));
            Assert.Throws<ArgumentNullException>(() => new AnimalSpawner(
                null, Settings(), _target, _bounds));
            Assert.Throws<ArgumentNullException>(() => new AnimalSpawner(
                Catalog(Definition("frog", 1)), null, _target, _bounds));
            Assert.Throws<ArgumentNullException>(() => new AnimalSpawner(
                Catalog(Definition("frog", 1)), Settings(), null, _bounds));
            Assert.Throws<ArgumentNullException>(() => new AnimalSpawner(
                Catalog(Definition("frog", 1)), Settings(), _target, null));
        }

        private AnimalSpawner Spawner(AnimalCatalog catalog, FakeSpawnTarget target = null,
            float min = 1, float max = 2, int cap = 10, uint seed = 7)
        {
            return new AnimalSpawner(catalog, Settings(min, max, cap, seed),
                target ?? _target, _bounds);
        }

        private AnimalCatalog Catalog(params AnimalDefinition[] definitions)
        {
            var catalog = Asset<AnimalCatalog>();
            var serialized = new SerializedObject(catalog);
            SerializedProperty array = serialized.FindProperty("_definitions");
            array.arraySize = definitions.Length;
            for (var i = 0; i < definitions.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private AnimalDefinition Definition(string id, float weight)
        {
            var definition = Asset<AnimalDefinition>();
            SetString(definition, "_id", id);
            SetFloat(definition, "_spawnWeight", weight);
            return definition;
        }

        private SimulationSettings Settings(float min = 1, float max = 2, int cap = 10,
            uint seed = 7, float inset = 0.05f, float height = 1)
        {
            var settings = Asset<SimulationSettings>();
            SetFloat(settings, "_minSpawnInterval", min);
            SetFloat(settings, "_maxSpawnInterval", max);
            SetInt(settings, "_liveCap", cap);
            SetInt(settings, "_seed", (int)seed);
            SetFloat(settings, "_spawnInset", inset);
            SetFloat(settings, "_spawnHeight", height);
            return settings;
        }

        private T Asset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _objects.Add(asset);
            return asset;
        }

        private static void SetString(Object target, string field, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class RecordedRent
        {
            public readonly AnimalDefinition Definition;
            public readonly Vector3 Position;
            public readonly Vector3 Direction;

            public RecordedRent(AnimalDefinition definition, Vector3 position, Vector3 direction)
            {
                Definition = definition;
                Position = position;
                Direction = direction;
            }
        }

        private sealed class FakeSpawnTarget : ISpawnTarget
        {
            public int ActiveCount { get; set; }
            public List<RecordedRent> Rents { get; } = new List<RecordedRent>();

            public Animal Rent(AnimalDefinition definition, Vector3 position, Vector3 direction)
            {
                Rents.Add(new RecordedRent(definition, position, direction));
                ActiveCount++;
                return null;
            }
        }
    }
}
