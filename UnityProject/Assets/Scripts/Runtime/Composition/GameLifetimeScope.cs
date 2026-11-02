using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using ZooWorld.Definitions;
using ZooWorld.Interaction;
using ZooWorld.Systems;

namespace ZooWorld.Composition
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private Camera _arenaCamera;
        [SerializeField] private float _groundHeight;
        [SerializeField] private AnimalCatalog _catalog;
        [SerializeField] private SimulationSettings _settings;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<FoodChainPolicy>(Lifetime.Singleton).As<IAnimalInteractionPolicy>();
            builder.Register<AnimalEventBus>(Lifetime.Singleton);
            builder.Register<GameStats>(Lifetime.Singleton);
            uint seed = _settings != null ? _settings.Seed : 0;
            builder.Register(resolver => new AnimalInteractionResolver(
                resolver.Resolve<IAnimalInteractionPolicy>(),
                resolver.Resolve<AnimalFactory>(),
                resolver.Resolve<AnimalEventBus>(), seed), Lifetime.Singleton);
            builder.RegisterBuildCallback(resolver => resolver.Resolve<GameStats>());
            ArenaBounds bounds = _arenaCamera != null ? new ArenaBounds(_arenaCamera, _groundHeight) : null;
            if (bounds != null)
            {
                builder.RegisterInstance(bounds);
            }

            builder.Register(resolver => new AnimalFactory(resolver, bounds), Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            if (_catalog != null || _settings != null)
            {
                if (_catalog == null || _settings == null)
                {
                    throw new InvalidOperationException(
                        "GameLifetimeScope requires both an animal catalog and simulation settings to enable spawning.");
                }

                builder.RegisterInstance(_catalog);
                builder.RegisterInstance(_settings);
                builder.RegisterEntryPoint<AnimalSpawner>().AsSelf();
            }
        }
    }
}
