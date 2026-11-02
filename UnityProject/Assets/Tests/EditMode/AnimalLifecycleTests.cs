using System;
using NUnit.Framework;
using UnityEngine;
using ZooWorld.Core;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class AnimalLifecycleTests
    {
        private GameObject _root;
        private Animal _animal;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Lifecycle fixture");
            _animal = _root.AddComponent<Animal>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test, Description("Смерть засчитывается один раз за активацию")]
        public void DeathSucceedsOnlyOnceForAnActivation()
        {
            Assert.That(_animal.TryDie(), Is.False);
            _animal.Activate(41);
            Assert.That(_animal.IsAlive, Is.True);
            Assert.That(_animal.TryDie(), Is.True);
            Assert.That(_animal.TryDie(), Is.False);
            Assert.That(_animal.IsAlive, Is.False);
            Assert.That(_animal.ActivationId, Is.EqualTo(41));
        }

        [Test, Description("Возврат очищает идентичность, следующая активация жива")]
        public void ReleaseClearsIdentityAndNextActivationIsAlive()
        {
            _animal.Activate(41);
            Assert.That(_animal.TryDie(), Is.True);
            _animal.Deactivate();
            _animal.Deactivate();
            Assert.That(_animal.ActivationId, Is.Zero);
            Assert.That(_animal.TryDie(), Is.False);
            _animal.Activate(42);
            Assert.That(_animal.ActivationId, Is.EqualTo(42));
            Assert.That(_animal.IsAlive, Is.True);
            Assert.That(_animal.TryDie(), Is.True);
        }

        [Test, Description("Очистка — не переход в смерть")]
        public void CleanupIsNotADeathTransition()
        {
            _animal.Activate(41);
            _animal.Deactivate();
            Assert.That(_animal.IsAlive, Is.False);
            Assert.That(_animal.TryDie(), Is.False);
        }

        [TestCase(0), TestCase(-1)]
        [Description("Неположительные ID активаций отклоняются")]
        public void NonPositiveActivationIdsAreRejected(long id)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _animal.Activate(id));
            Assert.That(_animal.IsAlive, Is.False);
            Assert.That(_animal.ActivationId, Is.Zero);
        }

        [Test, Description("Живая или мёртвая активация не перезаписывается до возврата")]
        public void LiveOrDeadActivationCannotBeOverwrittenBeforeRelease()
        {
            _animal.Activate(41);
            Assert.Throws<InvalidOperationException>(() => _animal.Activate(42));
            Assert.That(_animal.TryDie(), Is.True);
            Assert.Throws<InvalidOperationException>(() => _animal.Activate(42));
            Assert.That(_animal.ActivationId, Is.EqualTo(41));
        }
    }
}
