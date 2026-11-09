using System;
using static ZooWorld.Tests.SerializedFields;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VContainer;
using ZooWorld.Core;
using ZooWorld.Definitions;
using ZooWorld.Interaction;
using ZooWorld.Systems;
using ZooWorld.UI;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class HudPresenterTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private IObjectResolver _container;
        private AnimalFactory _factory;
        private AnimalEventBus _events;
        private GameStats _stats;
        private AnimalInteractionResolver _resolver;
        private FakeHudView _view;
        private HudPresenter _presenter;
        private long _nextActivationId;

        [SetUp]
        public void SetUp()
        {
            _nextActivationId = 3;
            var builder = new ContainerBuilder();
            builder.Register<FoodChainPolicy>(Lifetime.Singleton).As<IAnimalInteractionPolicy>();
            builder.Register<AnimalEventBus>(Lifetime.Singleton);
            builder.Register<AnimalFactory>(Lifetime.Singleton);
            builder.Register<GameStats>(Lifetime.Singleton);
            _container = builder.Build();
            _factory = _container.Resolve<AnimalFactory>();
            _events = _container.Resolve<AnimalEventBus>();
            _stats = _container.Resolve<GameStats>();
            _resolver = new AnimalInteractionResolver(
                _container.Resolve<IAnimalInteractionPolicy>(), _factory, _events);
            _view = new FakeHudView();
            _presenter = new HudPresenter(_stats, _view);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
            _stats.Dispose();
            _factory.Dispose();
            _container.Dispose();
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
        }

        [Test, Description("Start отправляет текущие счётчики во view")]
        public void StartPushesCurrentCountersToTheView()
        {
            _presenter.Start();
            Assert.That(_view.Calls, Is.EqualTo(new[] { (0, 0) }));
        }

        [Test, Description("Start отправляет счётчики, накопленные до подписки")]
        public void StartPushesCountersAccumulatedBeforeSubscription()
        {
            DrivePreyConsumption();
            _presenter.Start();
            Assert.That(_view.Calls, Is.EqualTo(new[] { (1, 0) }));
        }

        [Test, Description("Смерть добычи даёт ровно одно обновление view")]
        public void PreyDeathProducesExactlyOneViewUpdate()
        {
            _presenter.Start();
            DrivePreyConsumption();
            Assert.That(_view.Calls, Is.EqualTo(new[] { (0, 0), (1, 0) }));
        }

        [Test, Description("Смерть хищника обновляет только счётчик хищников")]
        public void PredatorDeathUpdatesOnlyThePredatorCounter()
        {
            _presenter.Start();
            Animal first = Activate(PredatorDefinition("snake-a"));
            Animal second = Activate(PredatorDefinition("snake-b"));
            _resolver.Resolve(Contact(first, second));
            Assert.That(_view.Calls, Is.EqualTo(new[] { (0, 0), (0, 1) }));
        }

        [Test, Description("Изменения после Dispose не доходят до view")]
        public void ChangesAfterDisposeDoNotReachTheView()
        {
            _presenter.Start();
            _presenter.Dispose();
            DrivePreyConsumption();
            Assert.That(_view.Calls, Is.EqualTo(new[] { (0, 0) }));
            Assert.That(_stats.PreyDeaths, Is.EqualTo(1), "The model still counts; only the view is cut off.");
        }

        [Test, Description("Новый presenter не удваивает обновления")]
        public void ReplacementPresenterDoesNotMultiplyUpdates()
        {
            _presenter.Start();
            _presenter.Dispose();
            var replacement = new HudPresenter(_stats, _view);
            replacement.Start();
            DrivePreyConsumption();
            Assert.That(_view.Calls, Is.EqualTo(new[] { (0, 0), (0, 0), (1, 0) }));
            replacement.Dispose();
        }

        [Test, Description("Повторный Start не подписывается дважды")]
        public void RepeatedStartDoesNotDoubleSubscribe()
        {
            _presenter.Start();
            _presenter.Start();
            DrivePreyConsumption();
            Assert.That(_view.Calls, Is.EqualTo(new[] { (0, 0), (1, 0) }));
        }

        private void DrivePreyConsumption()
        {
            Animal snake = Activate(PredatorDefinition("snake"));
            Animal frog = Activate(PreyDefinition("frog"));
            _resolver.Resolve(Contact(snake, frog));
        }

        private static AnimalContact Contact(Animal actor, Animal other)
        {
            return new AnimalContact(actor, actor.ActivationId, other, other.ActivationId,
                Vector3.zero);
        }

        private AnimalDefinition PreyDefinition(string speciesId)
        {
            return Definition(speciesId, AnimalRole.Prey, "prey", Array.Empty<string>());
        }

        private AnimalDefinition PredatorDefinition(string speciesId)
        {
            return Definition(speciesId, AnimalRole.Predator, "predator", new[] { "prey", "predator" });
        }

        private AnimalDefinition Definition(string speciesId, AnimalRole role, string categoryId, string[] edibleIds)
        {
            var definition = Asset<AnimalDefinition>($"{speciesId} Definition");
            var movement = Asset<LinearMovementProfile>($"{speciesId} Movement");
            var interaction = Asset<InteractionProfile>($"{speciesId} Interaction");
            SetReference(interaction, "_category", Category(categoryId));
            var edibles = new Object[edibleIds.Length];
            for (var i = 0; i < edibleIds.Length; i++)
            {
                edibles[i] = Category(edibleIds[i]);
            }

            SetArray(interaction, "_edibleCategories", edibles);
            SetString(definition, "_id", speciesId);
            SetEnum(definition, "_role", (int)role);
            SetReference(definition, "_movementProfile", movement);
            SetReference(definition, "_interactionProfile", interaction);
            return definition;
        }

        private Animal Activate(AnimalDefinition definition)
        {
            var root = new GameObject($"Animal {definition.Id} {_nextActivationId}");
            _objects.Add(root);
            root.SetActive(false);
            var motor = root.AddComponent<CatalogTestMotor>();
            var animal = root.AddComponent<Animal>();
            Assert.That(MovementContext.TryCreate(motor, Vector3.right, 8, null,
                out MovementContext context), Is.True);
            Assert.That(animal.TryActivate(_nextActivationId++, definition, context), Is.True);
            root.SetActive(true);
            return animal;
        }

        private InteractionCategory Category(string id)
        {
            var category = Asset<InteractionCategory>($"Category {id} {_objects.Count}");
            SetString(category, "_id", id);
            return category;
        }

        private T Asset<T>(string name) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            _objects.Add(asset);
            return asset;
        }

        private sealed class FakeHudView : IHudView
        {
            public readonly List<(int Prey, int Predator)> Calls = new List<(int, int)>();

            public void SetCounters(int preyDeaths, int predatorDeaths)
            {
                Calls.Add((preyDeaths, predatorDeaths));
            }
        }
    }
}
