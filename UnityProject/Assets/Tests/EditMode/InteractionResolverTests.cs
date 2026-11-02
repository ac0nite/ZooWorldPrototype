using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VContainer;
using ZooWorld.Core;
using ZooWorld.Definitions;
using ZooWorld.Interaction;
using ZooWorld.Systems;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class InteractionResolverTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private List<string> _order;
        private List<AnimalDied> _died;
        private List<AnimalConsumed> _consumed;
        private IObjectResolver _container;
        private AnimalFactory _factory;
        private AnimalEventBus _events;
        private GameStats _stats;
        private AnimalInteractionResolver _resolver;
        private int _changes;

        [SetUp]
        public void SetUp()
        {
            _order = new List<string>();
            _died = new List<AnimalDied>();
            _consumed = new List<AnimalConsumed>();
            _changes = 0;
            var builder = new ContainerBuilder();
            builder.Register<FoodChainPolicy>(Lifetime.Singleton).As<IAnimalInteractionPolicy>();
            builder.Register<AnimalEventBus>(Lifetime.Singleton);
            builder.Register<AnimalFactory>(Lifetime.Singleton);
            builder.Register<AnimalInteractionResolver>(Lifetime.Singleton);
            builder.Register<GameStats>(Lifetime.Singleton);
            _container = builder.Build();
            _factory = _container.Resolve<AnimalFactory>();
            _events = _container.Resolve<AnimalEventBus>();
            _stats = _container.Resolve<GameStats>();
            _resolver = Resolver(7);
            _events.Died += payload => { _order.Add("died"); _died.Add(payload); };
            _events.Consumed += payload => { _order.Add("consumed"); _consumed.Add(payload); };
            _stats.Changed += () => _changes++;
        }

        [TearDown]
        public void TearDown()
        {
            _stats.Dispose();
            _factory.Dispose();
            _container.Dispose();
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
        }

        [Test, Description("Хищник съедает добычу: Died, затем Consumed, затем возврат в пул")]
        public void PredatorConsumesPreyPublishesDiedThenConsumedThenReleases()
        {
            Animal snake = Activate(3, PredatorDefinition("snake"));
            Animal frog = Activate(5, PreyDefinition("frog"));
            bool activeAtDied = false;
            bool activeAtConsumed = false;
            _events.Died += _ => activeAtDied = frog.gameObject.activeSelf;
            _events.Consumed += _ => activeAtConsumed = frog.gameObject.activeSelf;
            _resolver.Resolve(Contact(snake, frog));
            Assert.That(frog.IsAlive, Is.False);
            Assert.That(snake.IsAlive, Is.True);
            Assert.That(_order, Is.EqualTo(new[] { "died", "consumed" }));
            Assert.That(_died[0].ActivationId, Is.EqualTo(5));
            Assert.That(_died[0].SpeciesId, Is.EqualTo("frog"));
            Assert.That(_died[0].Role, Is.EqualTo(AnimalRole.Prey));
            Assert.That(_consumed[0].EaterActivationId, Is.EqualTo(3));
            Assert.That(activeAtDied || activeAtConsumed, Is.True,
                "The victim must still be active while its death/consumption events are delivered.");
            Assert.That(frog.ActivationId, Is.EqualTo(0),
                "After events the victim lifecycle must end: activation cleared.");
            Assert.That(frog.gameObject.activeSelf, Is.False,
                "After events the victim object must be disabled.");
            Assert.That(_stats.PreyDeaths, Is.EqualTo(1));
            Assert.That(_stats.PredatorDeaths, Is.EqualTo(0));
            Assert.That(_changes, Is.EqualTo(1));
        }

        [Test, Description("Бросающий подписчик событий не мешает возврату жертвы")]
        public void ThrowingEventSubscriberDoesNotPreventVictimRelease()
        {
            Animal snake = Activate(3, PredatorDefinition("snake"));
            Animal frog = Activate(5, PreyDefinition("frog"));
            _events.Died += _ => throw new InvalidOperationException("subscriber failure");
            Assert.Throws<InvalidOperationException>(() => _resolver.Resolve(Contact(snake, frog)));
            Assert.That(frog.IsAlive, Is.False);
            Assert.That(frog.ActivationId, Is.EqualTo(0),
                "A throwing subscriber must not leave a dead animal active.");
            Assert.That(frog.gameObject.activeSelf, Is.False);
            Assert.That(_died.Count, Is.EqualTo(1), "Handlers registered before the throwing one still run.");
            Assert.That(_consumed, Is.Empty, "A throwing Died handler aborts the remaining chain.");
            Assert.That(_stats.PreyDeaths, Is.EqualTo(1));
        }

        [Test, Description("Consumed фиксирует мировую позицию едока в момент поедания")]
        public void ConsumedCapturesTheEatersWorldPositionAtConsumptionTime()
        {
            Animal snake = Activate(3, PredatorDefinition("snake"));
            Animal frog = Activate(5, PreyDefinition("frog"));
            snake.transform.position = new Vector3(3, 0.5f, -4);
            _resolver.Resolve(Contact(snake, frog));
            Assert.That(Vector3.Distance(_consumed[0].Position, new Vector3(3, 0.5f, -4)), Is.LessThan(0.001f));
        }

        [Test, Description("Дубли и обратные репорты дают одну смерть и пару событий")]
        public void DuplicateAndSwappedReportsProduceASingleDeathAndEventPair()
        {
            Animal snake = Activate(3, PredatorDefinition("snake"));
            Animal frog = Activate(5, PreyDefinition("frog"));
            AnimalContact forward = Contact(snake, frog);
            _resolver.Resolve(forward);
            _resolver.Resolve(forward);
            _resolver.Resolve(Contact(frog, snake));
            _resolver.Resolve(forward);
            Assert.That(_died.Count, Is.EqualTo(1));
            Assert.That(_consumed.Count, Is.EqualTo(1));
            Assert.That(_stats.PreyDeaths, Is.EqualTo(1));
            Assert.That(frog.IsAlive, Is.False);
        }

        [Test, Description("Пара хищников выбирает ту же жертву для обоих порядков контакта")]
        public void PredatorPairSelectsTheSameVictimForBothContactOrders()
        {
            Animal firstForward = Activate(11, PredatorDefinition("alpha"));
            Animal secondForward = Activate(13, PredatorDefinition("beta"));
            _resolver.Resolve(Contact(firstForward, secondForward));
            long forwardVictim = _died[0].ActivationId;
            Animal firstReverse = Activate(11, PredatorDefinition("alpha"));
            Animal secondReverse = Activate(13, PredatorDefinition("beta"));
            _resolver.Resolve(Contact(secondReverse, firstReverse));
            long reverseVictim = _died[1].ActivationId;
            Assert.That(reverseVictim, Is.EqualTo(forwardVictim));
            Assert.That(_died.Count, Is.EqualTo(2));
            Assert.That(_consumed.Count, Is.EqualTo(2));
            Assert.That(_stats.PredatorDeaths, Is.EqualTo(2));
            Assert.That(_stats.PreyDeaths, Is.EqualTo(0));
        }

        [Test, Description("Пара хищников оставляет выжившего и стопит события после смерти")]
        public void PredatorPairKeepsOneSurvivorAndStopsPublishingAfterDeath()
        {
            Animal first = Activate(11, PredatorDefinition("alpha"));
            Animal second = Activate(13, PredatorDefinition("beta"));
            _resolver.Resolve(Contact(first, second));
            Animal victim = first.IsAlive ? second : first;
            Animal survivor = first.IsAlive ? first : second;
            _resolver.Resolve(Contact(victim, survivor));
            _resolver.Resolve(Contact(survivor, victim));
            Assert.That(_died.Count, Is.EqualTo(1));
            Assert.That(_consumed.Count, Is.EqualTo(1));
            Assert.That(survivor.IsAlive, Is.True);
            Assert.That(_stats.PredatorDeaths, Is.EqualTo(1));
        }

        [Test, Description("Один и тот же seed выбирает ту же жертву для пары")]
        public void SameSeedAlwaysSelectsTheSameVictimForAPair()
        {
            var another = Resolver(7);
            Animal a = Activate(21, PredatorDefinition("alpha"));
            Animal b = Activate(23, PredatorDefinition("beta"));
            Animal c = Activate(21, PredatorDefinition("alpha"));
            Animal d = Activate(23, PredatorDefinition("beta"));
            _resolver.Resolve(Contact(a, b));
            another.Resolve(Contact(c, d));
            long first = _died[0].ActivationId;
            long second = _died[1].ActivationId;
            Assert.That(second, Is.EqualTo(first));
        }

        [Test, Description("Несъедобный контакт ничего не публикует")]
        public void NeitherConsumableContactPublishesNothing()
        {
            Animal first = Activate(3, PreyDefinition("prey-a"));
            Animal second = Activate(5, PreyDefinition("prey-b"));
            _resolver.Resolve(Contact(first, second));
            Assert.That(first.IsAlive && second.IsAlive, Is.True);
            Assert.That(_died, Is.Empty);
            Assert.That(_consumed, Is.Empty);
            Assert.That(_changes, Is.EqualTo(0));
        }

        [Test, Description("Добыча съедается независимо от порядка аргументов контакта")]
        public void PreyIsConsumedRegardlessOfContactArgumentOrder()
        {
            Animal frog = Activate(3, PreyDefinition("frog"));
            Animal snake = Activate(5, PredatorDefinition("snake"));
            _resolver.Resolve(Contact(frog, snake));
            Assert.That(frog.IsAlive, Is.False);
            Assert.That(_died[0].ActivationId, Is.EqualTo(3));
            Assert.That(_consumed[0].EaterActivationId, Is.EqualTo(5));
        }

        [Test, Description("Контакт с самим собой отклоняется")]
        public void SelfContactIsRejected()
        {
            Animal animal = Activate(3, PredatorDefinition("snake"));
            _resolver.Resolve(new AnimalContact(animal, animal.ActivationId, animal, animal.ActivationId,
                Vector3.zero, Vector3.zero));
            Assert.That(animal.IsAlive, Is.True);
            Assert.That(_died, Is.Empty);
        }

        [Test, Description("Устаревшие ID активаций отклоняются")]
        public void StaleActivationIdsAreRejected()
        {
            Animal snake = Activate(3, PredatorDefinition("snake"));
            Animal frog = Activate(5, PreyDefinition("frog"));
            _resolver.Resolve(new AnimalContact(snake, 999, frog, frog.ActivationId, Vector3.zero, Vector3.zero));
            _resolver.Resolve(new AnimalContact(snake, snake.ActivationId, frog, 999, Vector3.zero, Vector3.zero));
            Assert.That(snake.IsAlive && frog.IsAlive, Is.True);
            Assert.That(_died, Is.Empty);
        }

        [Test, Description("Мёртвые, возвращённые или неактивированные участники отклоняются")]
        public void DeadReleasedOrUnactivatedParticipantsAreRejected()
        {
            Animal snake = Activate(3, PredatorDefinition("snake"));
            Animal frog = Activate(5, PreyDefinition("frog"));
            Assert.That(frog.TryDie(), Is.True);
            _resolver.Resolve(Contact(snake, frog));
            Assert.That(_died, Is.Empty, "An already dead victim must not publish events.");
            var fresh = new GameObject("Unactivated");
            _objects.Add(fresh);
            var unactivated = fresh.AddComponent<Animal>();
            _resolver.Resolve(new AnimalContact(snake, snake.ActivationId, unactivated, 0, Vector3.zero, Vector3.zero));
            Assert.That(snake.IsAlive, Is.True);
            Assert.That(_died, Is.Empty);
            Animal released = Activate(7, PreyDefinition("released"));
            released.Deactivate();
            _resolver.Resolve(Contact(snake, released));
            Assert.That(snake.IsAlive, Is.True);
            Assert.That(_died, Is.Empty);
        }

        [Test, Description("Животное без данных взаимодействия отклоняется")]
        public void AnimalWithoutInteractionDataIsRejected()
        {
            var root = new GameObject("No interaction");
            _objects.Add(root);
            var animal = root.AddComponent<Animal>();
            animal.Activate(3);
            Animal snake = Activate(5, PredatorDefinition("snake"));
            _resolver.Resolve(new AnimalContact(snake, snake.ActivationId, animal, animal.ActivationId,
                Vector3.zero, Vector3.zero));
            Assert.That(animal.IsAlive && snake.IsAlive, Is.True);
            Assert.That(_died, Is.Empty);
        }

        [Test, Description("Резолвер отклоняет null-контакты и невалидные аргументы конструктора")]
        public void ResolverRejectsNullContactsAndInvalidConstructorArguments()
        {
            Assert.DoesNotThrow(() => _resolver.Resolve(null));
            Assert.That(_died, Is.Empty);
            Assert.Throws<ArgumentNullException>(() => new AnimalInteractionResolver(null, _factory, _events));
            Assert.Throws<ArgumentNullException>(() => new AnimalInteractionResolver(
                _container.Resolve<IAnimalInteractionPolicy>(), null, _events));
            Assert.Throws<ArgumentNullException>(() => new AnimalInteractionResolver(
                _container.Resolve<IAnimalInteractionPolicy>(), _factory, null));
        }

        [Test, Description("Resolve считает каждый не-null репорт контакта")]
        public void ResolveCountsEveryNonNullContactReport()
        {
            Animal snake = Activate(3, PredatorDefinition("snake"));
            Animal frog = Activate(5, PreyDefinition("frog"));
            Assert.That(_resolver.ContactsReceived, Is.EqualTo(0));
            _resolver.Resolve(Contact(snake, frog));
            Assert.That(_resolver.ContactsReceived, Is.EqualTo(1));
            _resolver.Resolve(Contact(snake, frog));
            Assert.That(_resolver.ContactsReceived, Is.EqualTo(2),
                "Rejected duplicate reports still count as received load.");
            _resolver.Resolve(null);
            Assert.That(_resolver.ContactsReceived, Is.EqualTo(2));
        }

        [Test, Description("Контакт требует обоих участников")]
        public void ContactRequiresBothActors()
        {
            Assert.Throws<ArgumentNullException>(() => new AnimalContact(null, 1, Activate(3, PreyDefinition("frog")), 3,
                Vector3.zero, Vector3.zero));
            Assert.Throws<ArgumentNullException>(() => new AnimalContact(Activate(3, PreyDefinition("frog")), 3, null, 1,
                Vector3.zero, Vector3.zero));
        }

        [Test, Description("GameStats отписывается при Dispose")]
        public void GameStatsUnsubscribesOnDispose()
        {
            var stats = new GameStats(_events);
            stats.Dispose();
            Animal snake = Activate(3, PredatorDefinition("snake"));
            Animal frog = Activate(5, PreyDefinition("frog"));
            _resolver.Resolve(Contact(snake, frog));
            Assert.That(stats.PreyDeaths, Is.EqualTo(0));
            Assert.That(_stats.PreyDeaths, Is.EqualTo(1));
        }

        private AnimalInteractionResolver Resolver(uint seed)
        {
            return new AnimalInteractionResolver(_container.Resolve<IAnimalInteractionPolicy>(), _factory, _events, seed);
        }

        private static AnimalContact Contact(Animal actor, Animal other)
        {
            return new AnimalContact(actor, actor.ActivationId, other, other.ActivationId,
                Vector3.zero, Vector3.zero);
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

        private Animal Activate(long id, AnimalDefinition definition)
        {
            var root = new GameObject($"Animal {definition.Id} {id}");
            _objects.Add(root);
            root.SetActive(false);
            var motor = root.AddComponent<CatalogTestMotor>();
            var animal = root.AddComponent<Animal>();
            animal.Activate(id, definition, new MovementContext(motor, Vector3.right, 8));
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

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string field, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty array = serialized.FindProperty(field);
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
