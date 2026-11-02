#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;
using ZooWorld.Composition;

namespace ZooWorld.Tests
{
    public sealed class ScopeIntegrationTests
    {
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
            Assert.That(scope.Container.Resolve<ZooWorld.Systems.AnimalFactory>(), Is.Not.Null);
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
