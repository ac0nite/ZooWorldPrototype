using UnityEngine;
using VContainer;
using VContainer.Unity;
using ZooWorld.Interaction;
using ZooWorld.Systems;

namespace ZooWorld.Composition
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private Camera _arenaCamera;
        [SerializeField] private float _groundHeight;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<FoodChainPolicy>(Lifetime.Singleton).As<IAnimalInteractionPolicy>();
            builder.Register<AnimalEventBus>(Lifetime.Singleton);
            builder.Register<GameStats>(Lifetime.Singleton);
            builder.Register<AnimalInteractionResolver>(Lifetime.Singleton);
            builder.RegisterBuildCallback(resolver => resolver.Resolve<GameStats>());
            ArenaBounds bounds = _arenaCamera != null ? new ArenaBounds(_arenaCamera, _groundHeight) : null;
            if (bounds != null)
            {
                builder.RegisterInstance(bounds);
            }

            builder.Register(resolver => new AnimalFactory(resolver, bounds), Lifetime.Singleton);
        }
    }
}
