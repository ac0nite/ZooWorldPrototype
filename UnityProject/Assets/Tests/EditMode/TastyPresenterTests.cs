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
    public sealed class TastyPresenterTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private IObjectResolver _container;
        private AnimalFactory _factory;
        private AnimalEventBus _events;
        private AnimalInteractionResolver _resolver;
        private FakeTastyView _view;
        private TastyPresenter _presenter;
        private long _nextActivationId;

        [SetUp]
        public void SetUp()
        {
            _nextActivationId = 3;
            var builder = new ContainerBuilder();
            builder.Register<FoodChainPolicy>(Lifetime.Singleton).As<IAnimalInteractionPolicy>();
            builder.Register<AnimalEventBus>(Lifetime.Singleton);
            builder.Register<AnimalFactory>(Lifetime.Singleton);
            _container = builder.Build();
            _factory = _container.Resolve<AnimalFactory>();
            _events = _container.Resolve<AnimalEventBus>();
            _resolver = new AnimalInteractionResolver(
                _container.Resolve<IAnimalInteractionPolicy>(), _factory, _events);
            _view = new FakeTastyView();
            _presenter = new TastyPresenter(_events, _view);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
            _factory.Dispose();
            _container.Dispose();
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
        }

        [Test, Description("Поедание показывает Tasty один раз в позиции едока")]
        public void ConsumptionShowsTastyOnceAtTheCapturedEaterPosition()
        {
            Animal snake = Activate(PredatorDefinition("snake"));
            Animal frog = Activate(PreyDefinition("frog"));
            snake.transform.position = new Vector3(2f, 0.5f, 3f);
            _presenter.Start();
            _resolver.Resolve(Contact(snake, frog));
            Assert.That(_view.Shows, Is.EqualTo(new[] { new Vector3(2f, 0.5f, 3f) }));
        }

        [Test, Description("Контакт двух хищников показывает ровно один Tasty")]
        public void MutualPredatorContactShowsExactlyOneTasty()
        {
            Animal first = Activate(PredatorDefinition("snake-a"));
            Animal second = Activate(PredatorDefinition("snake-b"));
            _presenter.Start();
            _resolver.Resolve(Contact(first, second));
            _resolver.Resolve(Contact(second, first));
            Assert.That(_view.Shows.Count, Is.EqualTo(1));
        }

        [Test, Description("Несмертельный контакт не показывает Tasty")]
        public void NonLethalContactShowsNoTasty()
        {
            Animal first = Activate(PreyDefinition("frog-a"));
            Animal second = Activate(PreyDefinition("frog-b"));
            _presenter.Start();
            _resolver.Resolve(Contact(first, second));
            Assert.That(_view.Shows, Is.Empty);
        }

        [Test, Description("Поедания после Dispose не доходят до view")]
        public void ConsumptionsAfterDisposeDoNotReachTheView()
        {
            _presenter.Start();
            _presenter.Dispose();
            Animal snake = Activate(PredatorDefinition("snake"));
            Animal frog = Activate(PreyDefinition("frog"));
            _resolver.Resolve(Contact(snake, frog));
            Assert.That(_view.Shows, Is.Empty);
        }

        [Test, Description("Новый presenter не удваивает показы")]
        public void ReplacementPresenterDoesNotMultiplyShows()
        {
            _presenter.Start();
            _presenter.Dispose();
            var replacement = new TastyPresenter(_events, _view);
            replacement.Start();
            Animal snake = Activate(PredatorDefinition("snake"));
            Animal frog = Activate(PreyDefinition("frog"));
            _resolver.Resolve(Contact(snake, frog));
            Assert.That(_view.Shows.Count, Is.EqualTo(1));
            replacement.Dispose();
        }

        [Test, Description("Повторный Start не подписывается дважды")]
        public void RepeatedStartDoesNotDoubleSubscribe()
        {
            _presenter.Start();
            _presenter.Start();
            Animal snake = Activate(PredatorDefinition("snake"));
            Animal frog = Activate(PreyDefinition("frog"));
            _resolver.Resolve(Contact(snake, frog));
            Assert.That(_view.Shows.Count, Is.EqualTo(1));
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

        private sealed class FakeTastyView : ITastyView
        {
            public readonly List<Vector3> Shows = new List<Vector3>();

            public void Show(Vector3 worldPosition)
            {
                Shows.Add(worldPosition);
            }
        }
    }
}
