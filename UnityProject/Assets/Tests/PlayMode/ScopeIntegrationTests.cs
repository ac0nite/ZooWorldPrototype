#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using ZooWorld.Composition;
using ZooWorld.Core;
using ZooWorld.Definitions;
using ZooWorld.Interaction;
using ZooWorld.UI;

namespace ZooWorld.Tests
{
    public sealed class ScopeIntegrationTests
    {
        private readonly List<Object> _extraObjects = new List<Object>();
        private GameObject _root;
        private GameObject _instance;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_instance != null)
            {
                Object.Destroy(_instance);
            }

            if (_root != null)
            {
                Object.Destroy(_root);
            }

            for (int i = _extraObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_extraObjects[i]);
            }

            _extraObjects.Clear();
            yield return null;
        }

        [UnityTest, Category("AC11")]
        public IEnumerator EntryPointStartsTicksAndStopsAfterScopeDisposal()
        {
            GameLifetimeScope scope = CreateScope();
            var probe = scope.Container.Resolve<LifecycleProbe>();
            for (var i = 0; i < 3; i++)
            {
                yield return null;
            }

            Assert.That(probe.Starts, Is.EqualTo(1));
            Assert.That(probe.Ticks, Is.GreaterThan(0));
            Object.Destroy(_root);
            yield return null;
            Assert.That(probe.Disposals, Is.EqualTo(1));
            int ticks = probe.Ticks;
            for (var i = 0; i < 3; i++)
            {
                yield return null;
            }

            Assert.That(probe.Ticks, Is.EqualTo(ticks));
            var replacement = CreateScope().Container.Resolve<LifecycleProbe>();
            for (var i = 0; i < 3; i++)
            {
                yield return null;
            }

            Assert.That(replacement, Is.Not.SameAs(probe));
            Assert.That(replacement.Starts, Is.EqualTo(1));
            Assert.That(replacement.Ticks, Is.GreaterThan(0));
            Assert.That(probe.Ticks, Is.EqualTo(ticks));
        }

        [UnityTest, Category("AC11")]
        public IEnumerator GameScopeResolvesTheRealPolicyAsAScopedSingleton()
        {
            GameLifetimeScope scope = CreateScope();
            var policy = scope.Container.Resolve<ZooWorld.Interaction.IAnimalInteractionPolicy>();
            Assert.That(policy, Is.TypeOf<ZooWorld.Interaction.FoodChainPolicy>());
            Assert.That(scope.Container.Resolve<ZooWorld.Interaction.IAnimalInteractionPolicy>(), Is.SameAs(policy));
            Object.Destroy(_root);
            yield return null;
            var replacement = CreateScope().Container.Resolve<ZooWorld.Interaction.IAnimalInteractionPolicy>();
            Assert.That(replacement, Is.Not.SameAs(policy));
        }

        [Test, Category("AC06"), Category("AC11")]
        public void GameScopeRegistersArenaBoundsFromSerializedCamera()
        {
            _instance = new GameObject("Bounds camera");
            var camera = _instance.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.Euler(90, 0, 0));
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.aspect = 1;
            GameLifetimeScope scope = CreateScope(camera);
            var bounds = scope.Container.Resolve<ZooWorld.Systems.ArenaBounds>();
            Assert.That(bounds, Is.Not.Null);
            Assert.That(bounds.TryGetInwardDirection(new Vector3(5.5f, 0, 0), out Vector3 inward), Is.True);
            Assert.That(inward.x, Is.LessThan(0));
            var factory = scope.Container.Resolve<ZooWorld.Systems.AnimalFactory>();
            Assert.That(factory, Is.Not.Null);
            Assert.That(factory.Bounds, Is.SameAs(bounds));
        }

        [UnityTest, Category("AC11")]
        public IEnumerator PrefabIsInjectedOnceBeforeEnableAndStart()
        {
            GameLifetimeScope scope = CreateScope();
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<InjectionProbe>(
                "Assets/Tests/PlayMode/Fixtures/InjectionProbe.prefab");
            Assert.That(prefab, Is.Not.Null, "The fixture must be a saved prefab asset.");
            InjectionProbe clone = scope.Container.Instantiate(prefab, scope.transform);
            _instance = clone.gameObject;
            Assert.That(clone.Dependency, Is.SameAs(scope.Container.Resolve<LifecycleProbe>()));
            Assert.That(clone.InjectedBeforeEnable, Is.True);
            yield return null;
            Assert.That(clone.InjectedBeforeStart, Is.True);
            _instance.SetActive(false);
            _instance.SetActive(true);
            Assert.That(clone.InjectionCount, Is.EqualTo(1));
            Assert.That(prefab.gameObject.activeSelf, Is.True);
        }

        [UnityTest, Category("AC07"), Category("AC11")]
        public IEnumerator GameScopeWiresHudAndTastyViewsFromSerializedReferences()
        {
            _instance = new GameObject("UI fixture");
            _instance.SetActive(false);
            var counterGo = new GameObject("Counters");
            counterGo.transform.SetParent(_instance.transform, false);
            Text counterText = counterGo.AddComponent<Text>();
            var hudGo = new GameObject("Hud view");
            hudGo.transform.SetParent(_instance.transform, false);
            var hudView = hudGo.AddComponent<HudView>();
            var hudSerialized = new UnityEditor.SerializedObject(hudView);
            hudSerialized.FindProperty("_counterText").objectReferenceValue = counterText;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();
            var canvasGo = new GameObject("Tasty canvas");
            canvasGo.transform.SetParent(_instance.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var templateGo = new GameObject("Tasty template");
            templateGo.transform.SetParent(canvasGo.transform, false);
            Text template = templateGo.AddComponent<Text>();
            var tastyLabels = canvasGo.AddComponent<TastyLabelPool>();
            var tastySerialized = new UnityEditor.SerializedObject(tastyLabels);
            tastySerialized.FindProperty("_labelTemplate").objectReferenceValue = template;
            tastySerialized.ApplyModifiedPropertiesWithoutUndo();

            _root = new GameObject("Test GameLifetimeScope");
            _root.SetActive(false);
            var scope = _root.AddComponent<GameLifetimeScope>();
            var scopeSerialized = new UnityEditor.SerializedObject(scope);
            scopeSerialized.FindProperty("_hudView").objectReferenceValue = hudView;
            scopeSerialized.FindProperty("_tastyLabels").objectReferenceValue = tastyLabels;
            scopeSerialized.ApplyModifiedPropertiesWithoutUndo();
            _instance.SetActive(true);
            _root.SetActive(true);
            yield return null;

            Assert.That(counterText.text, Is.EqualTo("Dead prey: 0\nDead predators: 0"),
                "The HUD presenter must push initial counters at startup.");

            var resolver = scope.Container.Resolve<ZooWorld.Systems.AnimalInteractionResolver>();
            Animal snake = ActivateUiAnimal(3, UiDefinition("snake", AnimalRole.Predator, "predator",
                new[] { "prey", "predator" }));
            Animal frog = ActivateUiAnimal(5, UiDefinition("frog", AnimalRole.Prey, "prey",
                System.Array.Empty<string>()));
            resolver.Resolve(new AnimalContact(snake, snake.ActivationId, frog, frog.ActivationId,
                Vector3.zero, Vector3.zero));

            Assert.That(counterText.text, Is.EqualTo("Dead prey: 1\nDead predators: 0"));
            Text active = null;
            foreach (Text label in tastyLabels.GetComponentsInChildren<Text>())
            {
                if (label.gameObject.activeSelf)
                {
                    Assert.That(active, Is.Null, "Exactly one Tasty label must be active.");
                    active = label;
                }
            }

            Assert.That(active, Is.Not.Null, "The Tasty presenter must show a label on consumption.");
            Assert.That(active.transform.position, Is.EqualTo(new Vector3(0f, 0f, -1f)));
            Assert.That(active.text, Is.EqualTo("Tasty!"));
        }

        private Animal ActivateUiAnimal(long id, AnimalDefinition definition)
        {
            var root = new GameObject($"UI animal {id}");
            root.transform.SetParent(_instance.transform, false);
            root.SetActive(false);
            var motor = root.AddComponent<CatalogTestMotor>();
            var animal = root.AddComponent<Animal>();
            animal.Activate(id, definition, new MovementContext(motor, Vector3.right, 8));
            root.SetActive(true);
            return animal;
        }

        private AnimalDefinition UiDefinition(string speciesId, AnimalRole role, string categoryId, string[] edibleIds)
        {
            var definition = Track(ScriptableObject.CreateInstance<AnimalDefinition>());
            var movement = Track(ScriptableObject.CreateInstance<LinearMovementProfile>());
            var interaction = Track(ScriptableObject.CreateInstance<InteractionProfile>());
            SetField(interaction, "_category", p => p.objectReferenceValue = UiCategory(categoryId));
            var categoryIds = edibleIds;
            var serializedInteraction = new UnityEditor.SerializedObject(interaction);
            UnityEditor.SerializedProperty edibles = serializedInteraction.FindProperty("_edibleCategories");
            edibles.arraySize = categoryIds.Length;
            for (var i = 0; i < categoryIds.Length; i++)
            {
                edibles.GetArrayElementAtIndex(i).objectReferenceValue = UiCategory(categoryIds[i]);
            }

            serializedInteraction.ApplyModifiedPropertiesWithoutUndo();
            SetField(definition, "_id", p => p.stringValue = speciesId);
            SetField(definition, "_role", p => p.intValue = (int)role);
            SetField(definition, "_movementProfile", p => p.objectReferenceValue = movement);
            SetField(definition, "_interactionProfile", p => p.objectReferenceValue = interaction);
            return definition;
        }

        private InteractionCategory UiCategory(string id)
        {
            var category = Track(ScriptableObject.CreateInstance<InteractionCategory>());
            SetField(category, "_id", p => p.stringValue = id);
            return category;
        }

        private T Track<T>(T value) where T : Object
        {
            _extraObjects.Add(value);
            return value;
        }

        private static void SetField(Object target, string field, System.Action<UnityEditor.SerializedProperty> change)
        {
            var serialized = new UnityEditor.SerializedObject(target);
            change(serialized.FindProperty(field));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private GameLifetimeScope CreateScope(Camera camera = null)
        {
            _root = new GameObject("Test GameLifetimeScope");
            _root.SetActive(false);
            GameLifetimeScope scope;
            using (LifetimeScope.Enqueue(builder => builder.RegisterEntryPoint<LifecycleProbe>().AsSelf()))
            {
                scope = _root.AddComponent<GameLifetimeScope>();
                if (camera != null)
                {
                    var serialized = new UnityEditor.SerializedObject(scope);
                    serialized.FindProperty("_arenaCamera").objectReferenceValue = camera;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                _root.SetActive(true);
            }

            return scope;
        }
    }
}
#endif
