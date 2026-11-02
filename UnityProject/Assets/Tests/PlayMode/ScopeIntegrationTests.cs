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

        private GameLifetimeScope CreateScope()
        {
            _root = new GameObject("Test GameLifetimeScope");
            _root.SetActive(false);
            GameLifetimeScope scope;
            using (LifetimeScope.Enqueue(builder => builder.RegisterEntryPoint<LifecycleProbe>().AsSelf()))
            {
                scope = _root.AddComponent<GameLifetimeScope>();
                _root.SetActive(true);
            }

            return scope;
        }
    }
}
#endif
