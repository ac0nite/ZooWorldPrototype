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
using ZooWorld.Motion;
using ZooWorld.Systems;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class ExtensibilityTests
    {
        private const float Step = 0.02f;

        // AC-08 fixture (declared before the demo assets): the third species "hare"
        // is a prey that reuses the shipped prey interaction profile and jumps with
        // distance=1.5 m, height=0.5 m, wait=0.8 s; the fourth species "turtle" is a
        // linear-mover prey at speed 1 m/s. Expected outcomes below come from these
        // fixture constants and the AC-05 PhysX tolerances, not from production code.
        private const float HareJumpDistance = 1.5f;
        private const float HareJumpHeight = 0.5f;
        private const float HareJumpWait = 0.8f;
        private const float HareRestHeight = 0.375f;
        private const float TurtleSpeed = 1f;
        private const float TurtleRestHeight = 0.55f;

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
        private AnimalDefinition _turtle;
        private AnimalCatalog _demoCatalog;

        [SetUp]
        public void SetUp()
        {
            _previousMode = Physics.simulationMode;
            _previousGravity = Physics.gravity;
            _previousStep = Time.fixedDeltaTime;
            Physics.simulationMode = SimulationMode.Script;
            Physics.gravity = new Vector3(0, -8, 0);
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
            _turtle = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/Tests/PlayMode/Fixtures/Turtle.asset");
            _demoCatalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>("Assets/Tests/PlayMode/Fixtures/ExtensibilityCatalog.asset");
            Assert.That(_snake, Is.Not.Null);
            Assert.That(_frog, Is.Not.Null);
            Assert.That(_hare, Is.Not.Null, "The demo third-species definition must be a saved asset.");
            Assert.That(_turtle, Is.Not.Null, "The demo fourth-species definition must be a saved asset.");
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
            Physics.simulationMode = _previousMode;
            Physics.gravity = _previousGravity;
            Time.fixedDeltaTime = _previousStep;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
        }

        [Test, Description("Демо-каталог добавляет виды только ассетами, shipped-каталог не меняется")]
        public void DemoCatalogAddsSpeciesThroughAssetsOnly()
        {
            Assert.DoesNotThrow(() => _demoCatalog.Validate());
            Assert.That(_demoCatalog.Definitions.Count, Is.EqualTo(4));
            var ids = new List<string>();
            foreach (AnimalDefinition definition in _demoCatalog.Definitions)
            {
                ids.Add(definition.Id);
            }

            Assert.That(ids, Is.EquivalentTo(new[] { "frog", "snake", "hare", "turtle" }));
            var shipped = AssetDatabase.LoadAssetAtPath<AnimalCatalog>("Assets/Settings/AnimalDefinitions/AnimalCatalog.asset");
            Assert.That(shipped.Definitions.Count, Is.EqualTo(3),
                "The shipped catalog ships frog/snake/hare; the turtle demo lives in fixture assets.");
            Assert.That(_hare.Role, Is.EqualTo(AnimalRole.Prey));
            Assert.That(_hare.InteractionProfile, Is.SameAs(_frog.InteractionProfile),
                "The hare reuses the shipped prey interaction profile asset.");
            var hareMovement = _hare.MovementProfile as JumpMovementProfile;
            Assert.That(hareMovement, Is.Not.Null);
            Assert.That(hareMovement.Distance, Is.EqualTo(HareJumpDistance));
            Assert.That(hareMovement.Height, Is.EqualTo(HareJumpHeight));
            Assert.That(hareMovement.WaitInterval, Is.EqualTo(HareJumpWait));
            Assert.That(_turtle.Role, Is.EqualTo(AnimalRole.Prey));
            Assert.That(_turtle.InteractionProfile, Is.SameAs(_frog.InteractionProfile),
                "The turtle reuses the shipped prey interaction profile asset.");
            var turtleMovement = _turtle.MovementProfile as LinearMovementProfile;
            Assert.That(turtleMovement, Is.Not.Null);
            Assert.That(turtleMovement.Speed, Is.EqualTo(TurtleSpeed));
        }

        [Test, Description("Новый вид спавнится фабрикой и прыгает по своему профилю")]
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

        [Test, Description("Новый вид гибнет от хищника по неизменным правилам")]
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

        [Test, Description("Новый вид отскакивает от добычи по неизменным правилам")]
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

            Assert.That(minDistance, Is.LessThan(0.9f),
                "The animals must actually reach contact range (sum of radii 0.75 m).");
            Assert.That(bounced, Is.True, "Prey contacts of the new species must physically rebound.");
            Tick(15);
            Assert.That(Vector3.Distance(first.transform.position, second.transform.position),
                Is.GreaterThanOrEqualTo(1.1f), "The rebound must visibly separate the bodies.");
            Assert.That(first.IsAlive && second.IsAlive, Is.True);
            Assert.That(_died, Is.Empty);
            Assert.That(_stats.PreyDeaths + _stats.PredatorDeaths, Is.EqualTo(0));
        }

        [Test, Description("Линейный demo-вид движется со своей настроенной скоростью")]
        public void NewLinearSpeciesMovesAtItsConfiguredSpeed()
        {
            Animal turtle = Rent(_turtle, new Vector3(0, TurtleRestHeight, 0), Vector3.right);
            Tick(50);
            Assert.That(turtle.transform.position.x, Is.EqualTo(TurtleSpeed).Within(0.06f));
            Assert.That(turtle.GetComponent<RigidbodyAnimalMotor>().IsGrounded, Is.True);
            Assert.That(turtle.IsAlive, Is.True);
        }

        [Test, Description("Пара черепах физически отскакивает при контакте")]
        public void NewLinearSpeciesPairBouncesOnContact()
        {
            Animal first = Rent(_turtle, new Vector3(-1, TurtleRestHeight, 0), Vector3.right);
            Animal second = Rent(_turtle, new Vector3(1, TurtleRestHeight, 0), Vector3.left);
            float minDistance = float.MaxValue;
            bool bounced = false;
            for (var i = 0; i < 60; i++)
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

            Assert.That(minDistance, Is.LessThan(1.15f),
                "The turtles must actually reach contact range (sum of radii 1.1 m).");
            Assert.That(bounced, Is.True, "The new prey species must physically rebound.");
            Tick(15);
            Assert.That(first.IsAlive && second.IsAlive, Is.True);
            Assert.That(_died, Is.Empty);
            Assert.That(_stats.PreyDeaths + _stats.PredatorDeaths, Is.EqualTo(0));
        }

        [Test, Description("Лягушка перепрыгивает стену, блокирующую наземные виды")]
        public void FrogClearsObstacleWallThatBlocksGroundSpecies()
        {
            Wall(new Vector3(-1, 0.275f, 0));
            Animal frog = Rent(_frog, new Vector3(-2, 0.5f, 0), Vector3.right);
            for (var i = 0; i < 140 && frog.transform.position.x <= -0.5f; i++)
            {
                Tick(1);
            }

            Assert.That(frog.transform.position.x, Is.GreaterThan(-0.5f),
                "The frog's jump must carry it past the wall's far face (-0.925 m); a blocked frog rests near the near face (-1.575 m).");
            Assert.That(frog.IsAlive, Is.True);
        }

        [Test, Description("Змея не может пересечь стену-препятствие")]
        public void SnakeCannotCrossTheObstacleWall()
        {
            Wall(new Vector3(-1.5f, 0.275f, 0));
            Animal snake = Rent(_snake, new Vector3(-2, 0.5f, 0), Vector3.right);
            Tick(150);
            Assert.That(snake.transform.position.x, Is.LessThan(-1f),
                "The linear-moving snake must stay on the near side of the wall.");
            Assert.That(snake.IsAlive, Is.True);
        }

        [Test, Description("Заяц не может перепрыгнуть стену-препятствие")]
        public void HareCannotCrossTheObstacleWall()
        {
            Wall(new Vector3(-1.5f, 0.275f, 0));
            Animal hare = Rent(_hare, new Vector3(-2, HareRestHeight, 0), Vector3.right);
            Tick(140);
            Assert.That(hare.transform.position.x, Is.LessThan(-1.4f),
                "The hare's lower jump must hit the wall face and fall back.");
            Assert.That(hare.IsAlive, Is.True);
        }

        [Test, Description("Черепаха не может пересечь стену-препятствие")]
        public void TurtleCannotCrossTheObstacleWall()
        {
            Wall(new Vector3(-1.5f, 0.275f, 0));
            Animal turtle = Rent(_turtle, new Vector3(-2, TurtleRestHeight, 0), Vector3.right);
            Tick(150);
            Assert.That(turtle.transform.position.x, Is.LessThan(-1f),
                "The linear-moving turtle must stay on the near side of the wall.");
            Assert.That(turtle.IsAlive, Is.True);
        }

        private Animal Rent(AnimalDefinition definition, Vector3 position, Vector3 direction)
        {
            Assert.That(_factory.TryRent(definition, position, direction, out Animal animal), Is.True);
            SceneManager.MoveGameObjectToScene(animal.gameObject, _scene);
            _animals.Add(animal);
            return animal;
        }

        private void Wall(Vector3 position)
        {
            GameObject wall = Track(new GameObject("Obstacle wall"));
            wall.transform.position = position;
            wall.AddComponent<BoxCollider>().size = new Vector3(0.15f, 0.55f, 4f);
        }

        private void Tick(int count)
        {
            for (int i = 0; i < count; i++)
            {
                foreach (Animal animal in _animals)
                {
                    if (animal != null && animal.gameObject.activeSelf)
                    {
                        animal.FixedUpdate();
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
