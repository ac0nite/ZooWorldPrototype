#if UNITY_EDITOR
using System;
using static ZooWorld.Tests.SerializedFields;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using ZooWorld.Core;
using ZooWorld.Definitions;
using ZooWorld.Motion;
using ZooWorld.Systems;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class PhysicalMovementTests
    {
        private const float Step = 0.02f;
        private readonly List<Object> _objects = new List<Object>();
        private readonly List<Animal> _animals = new List<Animal>();
        private SimulationMode _previousMode;
        private Vector3 _previousGravity;
        private float _previousStep;
        private Camera _camera;
        private ArenaBounds _bounds;
        private Scene _scene;
        private PhysicsScene _physicsScene;

        [SetUp]
        public void SetUp()
        {
            _previousMode = Physics.simulationMode;
            _previousGravity = Physics.gravity;
            _previousStep = Time.fixedDeltaTime;
            Physics.simulationMode = SimulationMode.Script;
            Physics.gravity = new Vector3(0, -8, 0);
            Time.fixedDeltaTime = Step;
            _scene = SceneManager.CreateScene("Physical fixture", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _physicsScene = _scene.GetPhysicsScene();
            GameObject ground = Track(new GameObject("Physical fixture ground"));
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.AddComponent<BoxCollider>().size = new Vector3(100, 1, 100);
            _camera = Track(new GameObject("Bounds fixture camera")).AddComponent<Camera>();
            _camera.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.Euler(90, 0, 0));
            _camera.orthographic = true;
            _camera.orthographicSize = 5;
            _camera.aspect = 1;
            _bounds = new ArenaBounds(_camera);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
            _animals.Clear();
            Physics.simulationMode = _previousMode;
            Physics.gravity = _previousGravity;
            Time.fixedDeltaTime = _previousStep;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
        }

        [Test, Description("Змея движется с настроенной скоростью по реальной земле")]
        public void SnakeMovesAtConfiguredSpeedOnRealGround()
        {
            Animal animal = Create(false, new Vector3(0, 0.5f, 0), Vector3.right);
            Tick(50);
            Assert.That(animal.transform.position.x, Is.EqualTo(3).Within(0.06f));
            Assert.That(animal.GetComponent<RigidbodyAnimalMotor>().IsGrounded, Is.True);
        }

        [Test, Description("Дальность, высота прыжка и пауза лягушки совпадают с конфигурацией")]
        public void FrogFlightDistanceHeightAndGroundedWaitMatchConfiguration()
        {
            Animal animal = Create(true, new Vector3(0, 0.5f, 0), Vector3.right);
            var motor = animal.GetComponent<RigidbodyAnimalMotor>();
            Tick(48);
            Assert.That(animal.transform.position.x, Is.EqualTo(0).Within(0.001f));
            int launch = 48;
            while (motor.Velocity.y < 1 && launch < 53)
            {
                Tick(1);
                launch++;
            }

            Assert.That(launch * Step, Is.EqualTo(1).Within(0.04f));
            float apex = animal.transform.position.y;
            int flight = 0;
            while ((!motor.IsGrounded || motor.Velocity.y > 0.1f) && flight++ < 65)
            {
                Tick(1);
                apex = Mathf.Max(apex, animal.transform.position.y);
            }

            Assert.That(flight, Is.LessThan(65));
            Assert.That(apex, Is.EqualTo(1.5f).Within(0.08f));
            float landingX = animal.transform.position.x;
            Assert.That(landingX, Is.EqualTo(2).Within(0.12f));
            int wait = 0;
            do
            {
                Tick(1);
                wait++;
                if (motor.Velocity.y < 1)
                {
                    Assert.That(animal.transform.position.x, Is.EqualTo(landingX).Within(0.04f));
                }
            }
            while (motor.Velocity.y < 1 && wait < 55);
            Assert.That(wait * Step, Is.EqualTo(1).Within(0.04f));
        }

        [Test, Description("Блокированный прыжок восстанавливается после снятия ограничения")]
        public void BlockedJumpRecoversAfterVerticalConstraintIsRemoved()
        {
            Animal animal = Create(true, new Vector3(0, 0.5f, 0), Vector3.right);
            var body = animal.GetComponent<Rigidbody>();
            body.constraints |= RigidbodyConstraints.FreezePositionY;
            Tick(60);
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.linearVelocity = Vector3.zero;
            bool airborne = false;
            for (int i = 0; i < 65; i++)
            {
                Tick(1);
                airborne |= animal.transform.position.y > 0.65f;
            }

            Assert.That(airborne, Is.True, "Failed launch must not permanently lock the strategy.");
        }

        [TestCase(0f), TestCase(0.4f)]
        [Description("Движущиеся добычи разлетаются и отскакивают без смертей")]
        public void MovingPreySeparateAndReboundWithoutLosingTheirLives(float offset)
        {
            Animal a = Create(false, new Vector3(-1, 0.5f, -offset / 2), Vector3.right, 1);
            Animal b = Create(false, new Vector3(1, 0.5f, offset / 2), Vector3.left, 1);
            Vector3 normal = Vector3.zero;
            float outward = 0;
            bool bounced = false;
            for (int i = 0; i < 35; i++)
            {
                Tick(1);
                normal = (b.transform.position - a.transform.position).normalized;
                outward = Vector3.Dot(b.GetComponent<Rigidbody>().linearVelocity - a.GetComponent<Rigidbody>().linearVelocity, normal);
                if (outward >= 1)
                {
                    bounced = true;
                    break;
                }
            }

            Assert.That(bounced, Is.True, "Real collision must reverse relative normal velocity.");
            if (offset == 0)
            {
                Assert.That(a.GetComponent<Rigidbody>().linearVelocity.x, Is.LessThanOrEqualTo(-1));
                Assert.That(b.GetComponent<Rigidbody>().linearVelocity.x, Is.GreaterThanOrEqualTo(1));
            }

            Tick(15);
            Assert.That(Vector3.Distance(a.transform.position, b.transform.position), Is.GreaterThanOrEqualTo(offset == 0 ? 1.2f : 1.1f));
            Assert.That(a.IsAlive && b.IsAlive, Is.True);
        }

        [Test, Description("Неупругий контакт останавливает тела, движение восстанавливается")]
        public void InelasticContactStopsBodiesThenMovementRecoversAfterBlockerRemoval()
        {
            Animal a = Create(false, new Vector3(-1, 0.5f, 0), Vector3.right, 0);
            Animal b = Create(false, new Vector3(1, 0.5f, 0), Vector3.left, 0);
            Tick(12);
            Assert.That(Mathf.Abs(b.GetComponent<Rigidbody>().linearVelocity.x - a.GetComponent<Rigidbody>().linearVelocity.x), Is.LessThanOrEqualTo(0.5f));
            float stoppedX = a.transform.position.x;
            b.Deactivate();
            b.gameObject.SetActive(false);
            Tick(30);
            Assert.That(a.transform.position.x, Is.GreaterThan(stoppedX + 0.3f));
        }

        [TestCase(-1, 0), TestCase(1, 0), TestCase(0, -1), TestCase(0, 1)]
        [TestCase(-1, -1), TestCase(-1, 1), TestCase(1, -1), TestCase(1, 1)]
        [Description("Оба типа движения разворачиваются у краёв и углов без телепорта")]
        public void BothMovementTypesReturnFromEachEdgeAndCornerWithoutTeleport(int x, int z)
        {
            foreach (bool jumping in new[] { false, true })
            {
                Vector3 start = new Vector3(x * 5.5f, 0.5f, z * 5.5f);
                Animal animal = Create(jumping, start, new Vector3(x, 0, z).normalized, bounds: _bounds);
                bool returned = false;
                for (int i = 0; i < (jumping ? 125 : 50); i++)
                {
                    Vector3 previous = animal.transform.position;
                    Tick(1);
                    Assert.That(Vector3.Distance(previous, animal.transform.position), Is.LessThanOrEqualTo(jumping ? 0.12f : 0.09f));
                    if (Mathf.Abs(animal.transform.position.x) <= 5 && Mathf.Abs(animal.transform.position.z) <= 5)
                    {
                        returned = true;
                        break;
                    }
                }

                Assert.That(returned, Is.True, jumping ? "Jump return" : "Linear return");
                animal.Deactivate();
                animal.gameObject.SetActive(false);
            }
        }

        [Test, Description("Касательное отклонение за границей разворачивается внутрь")]
        public void TangentialDeflectionWhileOutsideIsResteeredInward()
        {
            Animal animal = Create(false, new Vector3(5.5f, 0.5f, 0), Vector3.right, bounds: _bounds);
            Tick(2);
            Assert.That(animal.transform.position.x, Is.GreaterThan(5));
            animal.GetComponent<Rigidbody>().linearVelocity = new Vector3(0, 0, 3);
            bool returned = false;
            for (int i = 0; i < 50; i++)
            {
                Vector3 previous = animal.transform.position;
                Tick(1);
                Assert.That(Vector3.Distance(previous, animal.transform.position), Is.LessThanOrEqualTo(0.09f));
                if (Mathf.Abs(animal.transform.position.x) <= 5 && Mathf.Abs(animal.transform.position.z) <= 5)
                {
                    returned = true;
                    break;
                }
            }

            Assert.That(returned, Is.True, "Repeated deflection outside must be re-steered inward.");
            Assert.That(Mathf.Abs(animal.transform.position.z), Is.LessThan(1.5f));
        }

        [Test, Description("Уменьшенный отскок удерживается, затем скорость восстанавливается")]
        public void ReducedLinearReboundIsHeldThenRestoresConfiguredSpeed()
        {
            Animal animal = Create(false, new Vector3(0, 0.5f, 0), Vector3.right);
            Tick(5);
            animal.GetComponent<Rigidbody>().linearVelocity = new Vector3(1.5f, 0, 0);
            Tick(10);
            Assert.That(animal.GetComponent<Rigidbody>().linearVelocity.x, Is.EqualTo(1.5f).Within(0.05f),
                "A fresh rebound must survive the hold window untouched.");
            Tick(60);
            Assert.That(animal.GetComponent<Rigidbody>().linearVelocity.x, Is.EqualTo(3).Within(0.05f));
        }

        [Test, Description("Сохранённые лягушки отскакивают с боевым физматериалом")]
        public void SavedFrogsReboundWithTheProductionPhysicsMaterial()
        {
            var definition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/Settings/AnimalDefinitions/Frog.asset");
            Assert.That(definition, Is.Not.Null);
            PhysicsMaterial material = definition.Prefab.GetComponent<Collider>().sharedMaterial;
            Assert.That(material.bounciness, Is.EqualTo(1));
            Assert.That(material.bounceCombine, Is.EqualTo(PhysicsMaterialCombine.Minimum));
            Assert.That(material.staticFriction, Is.EqualTo(0));
            Assert.That(material.dynamicFriction, Is.EqualTo(0));
            Assert.That(material.frictionCombine, Is.EqualTo(PhysicsMaterialCombine.Minimum));
            using IObjectResolver container = BuildInteractionContainer(null);
            var factory = container.Resolve<AnimalFactory>();
            Assert.That(factory.TryRent(definition, new Vector3(-1, 0.5f, 0), Vector3.right, out Animal a),
                Is.True);
            Assert.That(factory.TryRent(definition, new Vector3(1, 0.5f, 0), Vector3.left, out Animal b),
                Is.True);
            SceneManager.MoveGameObjectToScene(a.gameObject, _scene);
            SceneManager.MoveGameObjectToScene(b.gameObject, _scene);
            _animals.Add(a);
            _animals.Add(b);
            bool bounced = false;
            for (int i = 0; i < 140; i++)
            {
                Tick(1);
                Vector3 normal = (b.transform.position - a.transform.position).normalized;
                float outward = Vector3.Dot(
                    b.GetComponent<Rigidbody>().linearVelocity - a.GetComponent<Rigidbody>().linearVelocity, normal);
                if (outward >= 1)
                {
                    bounced = true;
                    break;
                }
            }

            Assert.That(bounced, Is.True, "Saved production prey must physically rebound without dying.");
            Tick(15);
            Assert.That(Vector3.Distance(a.transform.position, b.transform.position), Is.GreaterThanOrEqualTo(1.1f));
            Assert.That(a.IsAlive && b.IsAlive, Is.True);
        }

        [Test, Description("Промах лучей камеры отключает разворот без поломки тиков")]
        public void CameraRaysMissingTheGroundPlaneDisableSteeringWithoutBreakingTicks()
        {
            _camera.transform.rotation = Quaternion.identity;
            Animal animal = Create(false, new Vector3(0, 0.5f, 0), Vector3.right, bounds: _bounds);
            Assert.That(_bounds.TryGetInwardDirection(animal.transform.position, out _), Is.False);
            Tick(50);
            Assert.That(animal.transform.position.x, Is.EqualTo(3).Within(0.06f));
            Assert.That(animal.IsAlive, Is.True);
        }

        [Test, Description("Животное на динамическом теле не считается на земле")]
        public void AnimalRestingOnADynamicBodyIsNotGrounded()
        {
            GameObject platform = Track(new GameObject("Dynamic platform"));
            platform.transform.position = new Vector3(0, 0.25f, 0);
            platform.AddComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            platform.AddComponent<BoxCollider>().size = new Vector3(2, 0.5f, 2);
            Animal animal = Create(false, new Vector3(0, 1f, 0), Vector3.right);
            var motor = animal.GetComponent<RigidbodyAnimalMotor>();
            Tick(3);
            Assert.That(motor.IsGrounded, Is.False);
            Assert.That(animal.transform.position.y, Is.EqualTo(1f).Within(0.05f));
            Assert.That(animal.IsAlive, Is.True);
        }

        [Test, Description("Границы следуют за камерой, нормализованные точки остаются внутри")]
        public void BoundsFollowCameraAndNormalizedPointsStayInside()
        {
            Assert.That(_bounds.TryGetInwardDirection(new Vector3(5.5f, 0, 0), out Vector3 direction), Is.True);
            Assert.That(direction.x, Is.LessThan(0));
            Assert.That(_bounds.TryGetPoint(Vector2.one, out Vector3 topRight), Is.True);
            Assert.That(topRight, Is.EqualTo(new Vector3(5, 0, 5)).Using(Vector3ComparerWithTolerance.Instance));
            _camera.orthographicSize = 10;
            _camera.aspect = 2;
            _camera.transform.position = new Vector3(3, 10, 4);
            Assert.That(_bounds.TryGetInwardDirection(new Vector3(5.5f, 0, 0), out _), Is.False);
            Assert.That(_bounds.TryGetPoint(Vector2.one, out Vector3 wideTopRight), Is.True);
            Assert.That(wideTopRight, Is.EqualTo(new Vector3(23, 0, 14)).Using(Vector3ComparerWithTolerance.Instance));
            _camera.transform.rotation = Quaternion.Euler(90, 30, 0);
            for (int x = 1; x < 10; x++)
            {
                for (int z = 1; z < 10; z++)
                {
                    Assert.That(_bounds.TryGetPoint(new Vector2(x / 10f, z / 10f), out Vector3 point), Is.True);
                    Assert.That(_bounds.TryGetInwardDirection(point, out _), Is.False);
                }
            }
        }

        [Test, Description("Виды из каталога спавнятся фабрикой с реальным мотором и общими границами")]
        public void SavedSpeciesRentThroughFactoryWithRealMotorAndSharedBounds()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>("Assets/Settings/AnimalDefinitions/AnimalCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            catalog.Validate();
            Assert.That(catalog.Definitions.Count, Is.EqualTo(3));
            using IObjectResolver container = BuildInteractionContainer(_bounds);
            var factory = container.Resolve<AnimalFactory>();
            foreach (string species in new[] { "Snake", "Frog", "Hare" })
            {
                var definition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/Settings/AnimalDefinitions/" + species + ".asset");
                Assert.That(definition.Prefab.GetComponent<RigidbodyAnimalMotor>(), Is.Not.Null);
                Assert.That(factory.TryRent(definition, new Vector3(5.5f, 0.5f, 0), Vector3.right,
                    out Animal animal), Is.True);
                SceneManager.MoveGameObjectToScene(animal.gameObject, _scene);
                _animals.Add(animal);
                Tick(species == "Snake" ? 50 : 125);
                Assert.That(animal.transform.position.x, Is.LessThan(5));
                Assert.That(animal.IsAlive, Is.True);
                factory.Release(animal, animal.ActivationId);
            }
        }

        [Test, Description("Сохранённая лягушка приземляется и ждёт, не отскакивая от земли")]
        public void SavedFrogLandsAndWaitsInsteadOfBouncingOffTheGround()
        {
            var definition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/Settings/AnimalDefinitions/Frog.asset");
            using IObjectResolver container = BuildInteractionContainer(null);
            var factory = container.Resolve<AnimalFactory>();
            Assert.That(factory.TryRent(definition, new Vector3(0, 0.5f, 0), Vector3.right,
                out Animal animal), Is.True);
            SceneManager.MoveGameObjectToScene(animal.gameObject, _scene);
            _animals.Add(animal);
            Tick(105);
            float landingX = animal.transform.position.x;
            Assert.That(landingX, Is.EqualTo(2).Within(0.12f));
            Tick(30);
            Assert.That(animal.transform.position.x, Is.EqualTo(landingX).Within(0.04f));
            Assert.That(animal.GetComponent<RigidbodyAnimalMotor>().IsGrounded, Is.True);
        }

        private static IObjectResolver BuildInteractionContainer(ArenaBounds bounds)
        {
            var builder = new ContainerBuilder();
            builder.Register<ZooWorld.Interaction.FoodChainPolicy>(Lifetime.Singleton).As<ZooWorld.Interaction.IAnimalInteractionPolicy>();
            builder.Register<AnimalEventBus>(Lifetime.Singleton);
            builder.Register(resolver => new AnimalFactory(resolver, bounds), Lifetime.Singleton);
            builder.Register<AnimalInteractionResolver>(Lifetime.Singleton);
            return builder.Build();
        }

        private Animal Create(bool jumping, Vector3 position, Vector3 direction, float restitution = 0, ArenaBounds bounds = null)
        {
            GameObject root = Track(new GameObject("Physical animal"));
            root.SetActive(false);
            root.transform.position = position;
            var body = root.AddComponent<Rigidbody>();
            body.mass = 1;
            body.linearDamping = 0;
            body.angularDamping = 0;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            var collider = root.AddComponent<SphereCollider>();
            collider.radius = 0.5f;
            collider.contactOffset = 0.01f;
            var material = Track(new PhysicsMaterial("Fixture response"));
            material.staticFriction = 0;
            material.dynamicFriction = 0;
            material.bounciness = restitution;
            material.bounceCombine = PhysicsMaterialCombine.Maximum;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            collider.sharedMaterial = material;
            var motor = root.AddComponent<RigidbodyAnimalMotor>();
            var animal = root.AddComponent<Animal>();
            MovementProfile profile = jumping ? Track(ScriptableObject.CreateInstance<JumpMovementProfile>()) : Track(ScriptableObject.CreateInstance<LinearMovementProfile>());
            var category = Track(ScriptableObject.CreateInstance<InteractionCategory>());
            Set(category, "_id", property => property.stringValue = "physical-prey");
            var interaction = Track(ScriptableObject.CreateInstance<InteractionProfile>());
            Set(interaction, "_category", property => property.objectReferenceValue = category);
            var definition = Track(ScriptableObject.CreateInstance<AnimalDefinition>());
            Set(definition, "_movementProfile", property => property.objectReferenceValue = profile);
            Set(definition, "_interactionProfile", property => property.objectReferenceValue = interaction);
            Assert.That(MovementContext.TryCreate(motor, direction, 8, bounds,
                out MovementContext context), Is.True);
            Assert.That(animal.TryActivate(_animals.Count + 1, definition, context), Is.True);
            _animals.Add(animal);
            root.SetActive(true);
            Physics.SyncTransforms();
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


        private sealed class Vector3ComparerWithTolerance : IEqualityComparer<Vector3>
        {
            public static readonly Vector3ComparerWithTolerance Instance = new Vector3ComparerWithTolerance();
            public bool Equals(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < 0.0001f;
            public int GetHashCode(Vector3 value) => 0;
        }
    }
}
#endif
