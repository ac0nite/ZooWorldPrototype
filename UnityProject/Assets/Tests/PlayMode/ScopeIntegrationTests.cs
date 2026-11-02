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
        GameObject root;
        GameObject instance;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (instance != null) Object.Destroy(instance);
            if (root != null) Object.Destroy(root);
            yield return null;
        }

        [UnityTest, Category("AC11")]
        public IEnumerator EntryPointStartsTicksAndStopsAfterScopeDisposal()
        {
            var scope = CreateScope();
            var probe = scope.Container.Resolve<LifecycleProbe>();
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(probe.Starts, Is.EqualTo(1));
            Assert.That(probe.Ticks, Is.GreaterThan(0));
            Object.Destroy(root);
            yield return null;
            Assert.That(probe.Disposals, Is.EqualTo(1));
            var ticks = probe.Ticks;
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(probe.Ticks, Is.EqualTo(ticks));
            var replacement = CreateScope().Container.Resolve<LifecycleProbe>();
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(replacement, Is.Not.SameAs(probe));
            Assert.That(replacement.Starts, Is.EqualTo(1));
            Assert.That(replacement.Ticks, Is.GreaterThan(0));
            Assert.That(probe.Ticks, Is.EqualTo(ticks));
        }

        [UnityTest, Category("AC11")]
        public IEnumerator PrefabIsInjectedOnceBeforeEnableAndStart()
        {
            var scope = CreateScope();
            var prefab = Resources.Load<InjectionProbe>("InjectionProbe");
            Assert.That(prefab, Is.Not.Null, "The fixture must be a saved prefab asset.");
            var clone = scope.Container.Instantiate(prefab, scope.transform);
            instance = clone.gameObject;
            Assert.That(clone.Dependency, Is.SameAs(scope.Container.Resolve<LifecycleProbe>()));
            Assert.That(clone.InjectedBeforeEnable, Is.True);
            yield return null;
            Assert.That(clone.InjectedBeforeStart, Is.True);
            instance.SetActive(false);
            instance.SetActive(true);
            Assert.That(clone.InjectionCount, Is.EqualTo(1));
            Assert.That(prefab.gameObject.activeSelf, Is.True);
        }

        GameLifetimeScope CreateScope()
        {
            root = new GameObject("Test GameLifetimeScope");
            root.SetActive(false);
            GameLifetimeScope scope;
            using (LifetimeScope.Enqueue(builder => builder.RegisterEntryPoint<LifecycleProbe>().AsSelf()))
            {
                scope = root.AddComponent<GameLifetimeScope>();
                root.SetActive(true);
            }
            return scope;
        }
    }
}
