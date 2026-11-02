using NUnit.Framework;
using VContainer;

namespace ZooWorld.Tests
{
    public sealed class CompositionTests
    {
        [Test, Category("AC11")]
        public void ScopeSingletonIsSharedWithinContainerButNotAcrossContainers()
        {
            var firstBuilder = new ContainerBuilder();
            firstBuilder.Register<Dependency>(Lifetime.Singleton);
            var secondBuilder = new ContainerBuilder();
            secondBuilder.Register<Dependency>(Lifetime.Singleton);
            using var first = firstBuilder.Build();
            using var second = secondBuilder.Build();
            Assert.That(first.Resolve<Dependency>(), Is.SameAs(first.Resolve<Dependency>()));
            Assert.That(second.Resolve<Dependency>(), Is.Not.SameAs(first.Resolve<Dependency>()));
        }

        public sealed class Dependency { }
    }
}
