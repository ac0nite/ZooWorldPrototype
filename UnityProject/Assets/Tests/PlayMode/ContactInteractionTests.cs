#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using ZooWorld.Composition;
using ZooWorld.Core;
using ZooWorld.Definitions;
using ZooWorld.Interaction;
using ZooWorld.Physics;
using ZooWorld.Systems;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class ContactInteractionTests
    {
        private const float Step = 0.02f;
        private readonly List<Object> _objects = new List<Object>();
        private readonly List<Animal> _animals = new List<Animal>();
        private List<string> _order;
        private List<AnimalDied> _died;
        private List<AnimalConsumed> _consumed;
        private SimulationMode _previousMode;
        private Vector3 _previousGravity;
        private float _previousStep;
        private Scene _scene;
        private PhysicsScene _physicsScene;
        private GameObject _scopeRoot;
        private AnimalFactory _factory;
        private AnimalEventBus _events;
        private GameStats _stats;
        private AnimalInteractionResolver _resolver;
        private AnimalDefinition _snake;
        private AnimalDefinition _frog;

        [SetUp]
        public void SetUp()
        {
            _order = new List<string>();
            _died = new List<AnimalDied>();
            _consumed = new List<AnimalConsumed>();
            _previousMode = UnityEngine.Physics.simulationMode;
            _previousGravity = UnityEngine.Physics.gravity;
            _previousStep = Time.fixedDeltaTime;
            UnityEngine.Physics.simulationMode = SimulationMode.Script;
            UnityEngine.Physics.gravity = new Vector3(0, -8, 0);
            Time.fixedDeltaTime = Step;
            _scene = SceneManager.CreateScene("Contact fixture", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _physicsScene = _scene.GetPhysicsScene();
            GameObject ground = Track(new GameObject("Contact fixture ground"));
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.AddComponent<BoxCollider>().size = new Vector3(100, 1, 100);
            _scopeRoot = new GameObject("Contact GameLifetimeScope");
            _scopeRoot.SetActive(false);
            var scope = _scopeRoot.AddComponent<GameLifetimeScope>();
            _scopeRoot.SetActive(true);
            _factory = scope.Container.Resolve<AnimalFactory>();
            _events = scope.Container.Resolve<AnimalEventBus>();
            _stats = scope.Container.Resolve<GameStats>();
            _resolver = scope.Container.Resolve<AnimalInteractionResolver>();
            _events.Died += payload => { _order.Add("died"); _died.Add(payload); };
            _events.Consumed += payload => { _order.Add("consumed"); _consumed.Add(payload); };
            _snake = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/Settings/AnimalDefinitions/Snake.asset");
            _frog = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/Settings/AnimalDefinitions/Frog.asset");
            Assert.That(_snake, Is.Not.Null);
            Assert.That(_frog, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scopeRoot != null)
            {
                Object.Destroy(_scopeRoot);
            }

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
            _animals.Clear();
            UnityEngine.Physics.simulationMode = _previousMode;
            UnityEngine.Physics.gravity = _previousGravity;
            Time.fixedDeltaTime = _previousStep;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
        }

        [Test, Category("AC03")]
        public void PredatorConsumesPreyOnRealContactWithExactlyOneOrderedEventPair()
        {
            Animal snake = Rent(_snake, new Vector3(-1, 0.5f, 0), Vector3.right);
            Animal frog = Rent(_frog, new Vector3(1, 0.5f, 0), Vector3.left);
            long frogActivation = frog.ActivationId;
            InteractionData snakeData = snake.Interaction;
            InteractionData frogData = frog.Interaction;
            bool activeAtDied = false;
            int countAtDied = -1;
            _events.Died += _ => { activeAtDied = frog.gameObject.activeSelf; countAtDied = _factory.ActiveCount; };
            for (var i = 0; i < 60 && frog.IsAlive; i++)
            {
                Tick(1);
            }

            Assert.That(frog.IsAlive, Is.False, "The predator must consume the prey on contact.");
            Assert.That(snake.IsAlive, Is.True);
            Assert.That(snakeData.CanConsume(frogData), Is.True);
            Assert.That(frogData.CanConsume(snakeData), Is.False,
                "Only the predator may consume the prey; the outcome must not depend on the tie-break.");
            Assert.That(_order, Is.EqualTo(new[] { "died", "consumed" }));
            Assert.That(_died[0].ActivationId, Is.EqualTo(frogActivation));
            Assert.That(_died[0].SpeciesId, Is.EqualTo(_frog.Id));
            Assert.That(_died[0].Role, Is.EqualTo(AnimalRole.Prey));
            Assert.That(_consumed[0].EaterActivationId, Is.EqualTo(snake.ActivationId));
            Assert.That(Vector3.Distance(_consumed[0].Position, snake.transform.position), Is.LessThan(0.5f));
            Assert.That(activeAtDied, Is.True, "The victim is released only after its events are delivered.");
            Assert.That(countAtDied, Is.EqualTo(2));
            Assert.That(_stats.PreyDeaths, Is.EqualTo(1));
            Assert.That(_stats.PredatorDeaths, Is.EqualTo(0));
            Assert.That(_factory.ActiveCount, Is.EqualTo(1));
            Assert.That(frog.gameObject.activeSelf, Is.False);
            Tick(50);
            Assert.That(_died.Count, Is.EqualTo(1));
            Assert.That(_consumed.Count, Is.EqualTo(1));
            Assert.That(_stats.PreyDeaths, Is.EqualTo(1));
        }

        [Test, Category("AC04")]
        public void PredatorPairRealContactKillsExactlyOneAcrossDuplicateReports()
        {
            Animal first = Rent(_snake, new Vector3(-1, 0.5f, 0), Vector3.right);
            Animal second = Rent(_snake, new Vector3(1, 0.5f, 0), Vector3.left);
            for (var i = 0; i < 60 && first.IsAlive && second.IsAlive; i++)
            {
                Tick(1);
            }

            Assert.That(first.IsAlive != second.IsAlive, Is.True, "Exactly one predator must survive.");
            Assert.That(_died.Count, Is.EqualTo(1));
            Assert.That(_consumed.Count, Is.EqualTo(1));
            Assert.That(_died[0].Role, Is.EqualTo(AnimalRole.Predator));
            Animal survivor = first.IsAlive ? first : second;
            Assert.That(_consumed[0].EaterActivationId, Is.EqualTo(survivor.ActivationId));
            Assert.That(_stats.PredatorDeaths, Is.EqualTo(1));
            Assert.That(_stats.PreyDeaths, Is.EqualTo(0));
            Assert.That(_factory.ActiveCount, Is.EqualTo(1));
            Tick(50);
            Assert.That(_died.Count, Is.EqualTo(1));
            Assert.That(_stats.PredatorDeaths, Is.EqualTo(1));
        }

        [Test, Category("AC02"), Category("AC03")]
        public void PreyPairRealContactProducesNoDeathsOrEvents()
        {
            AnimalDefinition prey = FixturePreyDefinition();
            Animal first = Rent(prey, new Vector3(-1, 0.5f, 0), Vector3.right);
            Animal second = Rent(prey, new Vector3(1, 0.5f, 0), Vector3.left);
            Tick(60);
            Assert.That(first.IsAlive && second.IsAlive, Is.True);
            Assert.That(_died, Is.Empty);
            Assert.That(_consumed, Is.Empty);
            Assert.That(_stats.PreyDeaths + _stats.PredatorDeaths, Is.EqualTo(0));
            Assert.That(_factory.ActiveCount, Is.EqualTo(2));
        }

        [Test, Category("AC03")]
        public void GroundContactsProduceNoEventsDuringContinuousMovement()
        {
            Animal frog = Rent(_frog, new Vector3(0, 0.5f, 0), Vector3.right);
            Tick(150);
            Assert.That(frog.IsAlive, Is.True);
            Assert.That(_died, Is.Empty);
            Assert.That(_consumed, Is.Empty);
        }

        [Test, Category("AC03"), Category("AC09")]
        public void ReplayedAndStaleContactsAreRejectedAfterDeathReleaseAndRerent()
        {
            Animal first = Rent(_snake, new Vector3(-1, 0.5f, 0), Vector3.right);
            Animal second = Rent(_snake, new Vector3(1, 0.5f, 0), Vector3.left);
            var forward = new AnimalContact(first, first.ActivationId, second, second.ActivationId,
                Vector3.zero, Vector3.zero);
            var reverse = new AnimalContact(second, second.ActivationId, first, first.ActivationId,
                Vector3.zero, Vector3.zero);
            _resolver.Resolve(forward);
            Animal victim = first.IsAlive ? second : first;
            Animal survivor = first.IsAlive ? first : second;
            long staleVictimId = _died[0].ActivationId;
            _resolver.Resolve(forward);
            _resolver.Resolve(reverse);
            Assert.That(_died.Count, Is.EqualTo(1), "Duplicate reports must not kill twice.");
            Assert.That(_stats.PredatorDeaths, Is.EqualTo(1));
            Assert.That(_factory.ActiveCount, Is.EqualTo(1));
            Animal rerented = _factory.Rent(_snake, new Vector3(0, 0.5f, -20), Vector3.forward);
            _animals.Add(rerented);
            SceneManager.MoveGameObjectToScene(rerented.gameObject, _scene);
            Assert.That(rerented, Is.SameAs(victim));
            Assert.That(rerented.ActivationId, Is.Not.EqualTo(staleVictimId));
            _resolver.Resolve(forward);
            _resolver.Resolve(reverse);
            Assert.That(rerented.IsAlive, Is.True, "A stale contact must not kill a reactivated instance.");
            Assert.That(survivor.IsAlive, Is.True);
            Assert.That(_died.Count, Is.EqualTo(1));
            Assert.That(_stats.PredatorDeaths, Is.EqualTo(1));
        }

        [Test, Category("AC11")]
        public void ScopeWiresResolverEventsStatsAndRelayInjection()
        {
            Assert.That(_resolver, Is.Not.Null);
            Assert.That(_events, Is.Not.Null);
            Assert.That(_stats, Is.Not.Null);
            Animal snake = Rent(_snake, new Vector3(-1, 0.5f, -3), Vector3.right);
            var relay = snake.GetComponent<AnimalContactRelay>();
            Assert.That(relay, Is.Not.Null);
            Animal frog = Rent(_frog, new Vector3(1, 0.5f, -3), Vector3.left);
            for (var i = 0; i < 30 && frog.IsAlive; i++)
            {
                Tick(1);
            }

            Assert.That(_died, Is.Not.Empty, "The injected relay must forward real collisions to the scope resolver.");
            Assert.That(_stats.PreyDeaths, Is.EqualTo(1));
        }

        private AnimalDefinition FixturePreyDefinition()
        {
            var definition = Track(ScriptableObject.CreateInstance<AnimalDefinition>());
            var movement = Track(ScriptableObject.CreateInstance<LinearMovementProfile>());
            var interaction = Track(ScriptableObject.CreateInstance<InteractionProfile>());
            var category = Track(ScriptableObject.CreateInstance<InteractionCategory>());
            Set(category, "_id", property => property.stringValue = "fixture-prey");
            Set(interaction, "_category", property => property.objectReferenceValue = category);
            Set(definition, "_id", property => property.stringValue = "fixture-prey-species");
            Set(definition, "_movementProfile", property => property.objectReferenceValue = movement);
            Set(definition, "_interactionProfile", property => property.objectReferenceValue = interaction);
            Set(definition, "_prefab", property => property.objectReferenceValue = _frog.Prefab);
            return definition;
        }

        private Animal Rent(AnimalDefinition definition, Vector3 position, Vector3 direction)
        {
            Animal animal = _factory.Rent(definition, position, direction);
            SceneManager.MoveGameObjectToScene(animal.gameObject, _scene);
            _animals.Add(animal);
            return animal;
        }

        private void Tick(int count)
        {
            for (int i = 0; i < count; i++)
            {
                foreach (Animal animal in _animals)
                {
                    if (animal != null && animal.gameObject.activeSelf)
                    {
                        animal.SendMessage("FixedUpdate");
                    }
                }

                _physicsScene.Simulate(Step);
            }
        }

        private T Track<T>(T value) where T : Object
        {
            _objects.Add(value);
            if (value is GameObject root)
            {
                SceneManager.MoveGameObjectToScene(root, _scene);
            }

            return value;
        }

        private static void Set(Object target, string field, Action<SerializedProperty> change)
        {
            var serialized = new SerializedObject(target);
            change(serialized.FindProperty(field));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
