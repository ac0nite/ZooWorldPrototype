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

        [Test, Category("AC03"), Category("AC04")]
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

        [Test, Category("AC09")]
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

        [Test, Category("AC10")]
        public void CleanupIsNotADeathTransition()
        {
            _animal.Activate(41);
            _animal.Deactivate();
            Assert.That(_animal.IsAlive, Is.False);
            Assert.That(_animal.TryDie(), Is.False);
        }

        [TestCase(0), TestCase(-1), Category("AC09")]
        public void NonPositiveActivationIdsAreRejected(long id)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _animal.Activate(id));
            Assert.That(_animal.IsAlive, Is.False);
            Assert.That(_animal.ActivationId, Is.Zero);
        }

        [Test, Category("AC09")]
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
