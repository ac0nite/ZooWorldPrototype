using System;
using NUnit.Framework;
using UnityEngine;
using ZooWorld.Core;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class AnimalLifecycleTests
    {
        GameObject root;
        Animal animal;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Lifecycle fixture");
            animal = root.AddComponent<Animal>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test, Category("AC03"), Category("AC04")]
        public void DeathSucceedsOnlyOnceForAnActivation()
        {
            Assert.That(animal.TryDie(), Is.False);
            animal.Activate(41);
            Assert.That(animal.IsAlive, Is.True);
            Assert.That(animal.TryDie(), Is.True);
            Assert.That(animal.TryDie(), Is.False);
            Assert.That(animal.IsAlive, Is.False);
            Assert.That(animal.ActivationId, Is.EqualTo(41));
        }

        [Test, Category("AC09")]
        public void ReleaseClearsIdentityAndNextActivationIsAlive()
        {
            animal.Activate(41);
            Assert.That(animal.TryDie(), Is.True);
            animal.Deactivate();
            animal.Deactivate();
            Assert.That(animal.ActivationId, Is.Zero);
            Assert.That(animal.TryDie(), Is.False);
            animal.Activate(42);
            Assert.That(animal.ActivationId, Is.EqualTo(42));
            Assert.That(animal.IsAlive, Is.True);
            Assert.That(animal.TryDie(), Is.True);
        }

        [Test, Category("AC10")]
        public void CleanupIsNotADeathTransition()
        {
            animal.Activate(41);
            animal.Deactivate();
            Assert.That(animal.IsAlive, Is.False);
            Assert.That(animal.TryDie(), Is.False);
        }

        [TestCase(0), TestCase(-1), Category("AC09")]
        public void NonPositiveActivationIdsAreRejected(long id)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => animal.Activate(id));
            Assert.That(animal.IsAlive, Is.False);
            Assert.That(animal.ActivationId, Is.Zero);
        }

        [Test, Category("AC09")]
        public void LiveOrDeadActivationCannotBeOverwrittenBeforeRelease()
        {
            animal.Activate(41);
            Assert.Throws<InvalidOperationException>(() => animal.Activate(42));
            Assert.That(animal.TryDie(), Is.True);
            Assert.Throws<InvalidOperationException>(() => animal.Activate(42));
            Assert.That(animal.ActivationId, Is.EqualTo(41));
        }
    }
}
