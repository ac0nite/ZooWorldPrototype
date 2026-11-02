#if UNITY_EDITOR
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
    public sealed class ExtensibilityTests
    {
        private const float Step = 0.02f;

        // AC-08 fixture (declared before the demo assets): the third species "hare"
        // is a prey that reuses the shipped prey interaction profile and jumps with
        // distance=1.5 m, height=0.7 m, wait=0.8 s. Expected outcomes below come from
        // these fixture constants and the AC-05 PhysX tolerances, not from production code.
        private const float HareJumpDistance = 1.5f;
        private const float HareJumpHeight = 0.7f;
        private const float HareJumpWait = 0.8f;
        private const float HareRestHeight = 0.375f;

        private readonly List<Object> _objects = new List<Object>();
        private readonly List<Animal> _animals = new List<Animal>();
        private readonly List<AnimalDied> _died = new List<AnimalDied>();
        private readonly List<AnimalConsumed> _consumed = new List<AnimalConsumed>();
        private SimulationMode _previousMode;
        private Vector3 _previousGravity;
        private float _previousStep;
        private Scene _scene;
        private PhysicsScene _physicsScene;
        private GameObject _scopeRoot;
        private AnimalFactory _factory;
        private GameStats _stats;
        private AnimalDefinition _snake;
        private AnimalDefinition _frog;
        private AnimalDefinition _hare;
        private AnimalCatalog _demoCatalog;

        [SetUp]
        public void SetUp()
        {
            _previousMode = UnityEngine.Physics.simulationMode;
            _previousGravity = UnityEngine.Physics.gravity;
            _previousStep = Time.fixedDeltaTime;
            UnityEngine.Physics.simulationMode = SimulationMode.Script;
            UnityEngine.Physics.gravity = new Vector3(0, -8, 0);
            Time.fixedDeltaTime = Step;
            _scene = SceneManager.CreateScene("Extensibility fixture", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _physicsScene = _scene.GetPhysicsScene();
            GameObject ground = Track(new GameObject("Extensibility fixture ground"));
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.AddComponent<BoxCollider>().size = new Vector3(100, 1, 100);
            _scopeRoot = new GameObject("Extensibility GameLifetimeScope");
            _scopeRoot.SetActive(false);
            var scope = _scopeRoot.AddComponent<GameLifetimeScope>();
            _scopeRoot.SetActive(true);
            _factory = scope.Container.Resolve<AnimalFactory>();
            _stats = scope.Container.Resolve<GameStats>();
            var events = scope.Container.Resolve<AnimalEventBus>();
            events.Died += payload => _died.Add(payload);
            events.Consumed += payload => _consumed.Add(payload);
            _snake = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/Settings/AnimalDefinitions/Snake.asset");
            _frog = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/Settings/AnimalDefinitions/Frog.asset");
            _hare = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/Tests/PlayMode/Fixtures/Hare.asset");
            _demoCatalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>("Assets/Tests/PlayMode/Fixtures/ExtensibilityCatalog.asset");
            Assert.That(_snake, Is.Not.Null);
            Assert.That(_frog, Is.Not.Null);
            Assert.That(_hare, Is.Not.Null, "The demo third-species definition must be a saved asset.");
            Assert.That(_demoCatalog, Is.Not.Null, "The demo catalog must be a saved asset.");
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
            _died.Clear();
            _consumed.Clear();
            UnityEngine.Physics.simulationMode = _previousMode;
            UnityEngine.Physics.gravity = _previousGravity;
            Time.fixedDeltaTime = _previousStep;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
        }

        [Test, Category("AC08")]
        public void DemoCatalogValidatesWithThirdSpeciesWhileShippedCatalogStaysUnchanged()
        {
            Assert.DoesNotThrow(() => _demoCatalog.Validate());
            Assert.That(_demoCatalog.Definitions.Count, Is.EqualTo(3));
            var ids = new List<string>();
            foreach (AnimalDefinition definition in _demoCatalog.Definitions)
            {
                ids.Add(definition.Id);
            }

            Assert.That(ids, Is.EquivalentTo(new[] { "frog", "snake", "hare" }));
            var shipped = AssetDatabase.LoadAssetAtPath<AnimalCatalog>("Assets/Settings/AnimalDefinitions/AnimalCatalog.asset");
            Assert.That(shipped.Definitions.Count, Is.EqualTo(2),
                "The shipped catalog must keep exactly the two task species; the demo lives in fixture assets.");
            Assert.That(_hare.Role, Is.EqualTo(AnimalRole.Prey));
            Assert.That(_hare.InteractionProfile, Is.SameAs(_frog.InteractionProfile),
                "The hare reuses the shipped prey interaction profile asset.");
            var movement = _hare.MovementProfile as JumpMovementProfile;
            Assert.That(movement, Is.Not.Null);
            Assert.That(movement.Distance, Is.EqualTo(HareJumpDistance));
            Assert.That(movement.Height, Is.EqualTo(HareJumpHeight));
            Assert.That(movement.WaitInterval, Is.EqualTo(HareJumpWait));
        }

        [Test, Category("AC05"), Category("AC08")]
        public void NewSpeciesRentsThroughFactoryAndJumpsWithItsConfiguredProfile()
        {
            Animal hare = Rent(_hare, new Vector3(0, HareRestHeight, 0), Vector3.right);
            var motor = hare.GetComponent<RigidbodyAnimalMotor>();
            Assert.That(motor, Is.Not.Null, "The prefab variant must carry the production motor.");
            int launch = 0;
            while (motor.Velocity.y < 1 && launch < 45)
            {
                Tick(1);
                launch++;
            }

            Assert.That(launch * Step, Is.EqualTo(HareJumpWait).Within(0.04f),
                "The first grounded wait must follow the hare's own profile interval.");
            float apex = hare.transform.position.y;
            int flight = 0;
            while ((!motor.IsGrounded || motor.Velocity.y > 0.1f) && flight++ < 65)
            {
                Tick(1);
                apex = Mathf.Max(apex, hare.transform.position.y);
            }

            Assert.That(flight, Is.LessThan(65));
            Assert.That(apex, Is.EqualTo(HareRestHeight + HareJumpHeight).Within(0.08f));
            Assert.That(hare.transform.position.x, Is.EqualTo(HareJumpDistance).Within(0.12f));
            Assert.That(hare.IsAlive, Is.True);
        }

        [Test, Category("AC03"), Category("AC08")]
        public void NewSpeciesDiesToPredatorThroughUnchangedInteractionRules()
        {
            Animal snake = Rent(_snake, new Vector3(-1, 0.5f, 0), Vector3.right);
            Animal hare = Rent(_hare, new Vector3(1, 0.5f, 0), Vector3.left);
            long hareActivation = hare.ActivationId;
            for (var i = 0; i < 60 && hare.IsAlive; i++)
            {
                Tick(1);
            }

            Assert.That(hare.IsAlive, Is.False, "The predator must consume the new prey species on contact.");
            Assert.That(snake.IsAlive, Is.True);
            Assert.That(_died.Count, Is.EqualTo(1));
            Assert.That(_died[0].ActivationId, Is.EqualTo(hareActivation));
            Assert.That(_died[0].SpeciesId, Is.EqualTo("hare"));
            Assert.That(_died[0].Role, Is.EqualTo(AnimalRole.Prey));
            Assert.That(_consumed.Count, Is.EqualTo(1));
            Assert.That(_consumed[0].EaterActivationId, Is.EqualTo(snake.ActivationId));
            Assert.That(_stats.PreyDeaths, Is.EqualTo(1));
            Assert.That(_stats.PredatorDeaths, Is.EqualTo(0));
            Assert.That(_factory.ActiveCount, Is.EqualTo(1));
            Assert.That(hare.gameObject.activeSelf, Is.False);
        }

        [Test, Category("AC02"), Category("AC08")]
        public void NewSpeciesBouncesOffPreyThroughUnchangedInteractionRules()
        {
            Animal first = Rent(_hare, new Vector3(-1, HareRestHeight, 0), Vector3.right);
            Animal second = Rent(_hare, new Vector3(1, HareRestHeight, 0), Vector3.left);
            float minDistance = float.MaxValue;
            bool bounced = false;
            for (var i = 0; i < 90; i++)
            {
                Tick(1);
                minDistance = Mathf.Min(minDistance,
                    Vector3.Distance(first.transform.position, second.transform.position));
                Vector3 normal = (second.transform.position - first.transform.position).normalized;
                float outward = Vector3.Dot(
                    second.GetComponent<Rigidbody>().linearVelocity - first.GetComponent<Rigidbody>().linearVelocity, normal);
                if (outward >= 1 && first.transform.position.x < second.transform.position.x)
                {
                    bounced = true;
                    break;
                }
            }

            Assert.That(minDistance, Is.LessThan(0.8f),
                "The animals must actually reach contact range (sum of radii 0.75 m).");
            Assert.That(bounced, Is.True, "Prey contacts of the new species must physically rebound.");
            Tick(15);
            Assert.That(Vector3.Distance(first.transform.position, second.transform.position),
                Is.GreaterThanOrEqualTo(1.1f), "The rebound must visibly separate the bodies.");
            Assert.That(first.IsAlive && second.IsAlive, Is.True);
            Assert.That(_died, Is.Empty);
            Assert.That(_stats.PreyDeaths + _stats.PredatorDeaths, Is.EqualTo(0));
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
    }
}
#endif
