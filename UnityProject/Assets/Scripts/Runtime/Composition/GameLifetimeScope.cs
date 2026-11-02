using VContainer;
using VContainer.Unity;
using ZooWorld.Interaction;
using ZooWorld.Systems;

namespace ZooWorld.Composition
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<FoodChainPolicy>(Lifetime.Singleton).As<IAnimalInteractionPolicy>();
            builder.Register<AnimalFactory>(Lifetime.Singleton);
        }
    }
}
