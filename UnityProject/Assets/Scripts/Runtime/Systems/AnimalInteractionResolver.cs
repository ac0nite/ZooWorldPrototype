using System;
using VContainer;
using ZooWorld.Core;
using ZooWorld.Definitions;
using ZooWorld.Interaction;

namespace ZooWorld.Systems
{
    public sealed class AnimalInteractionResolver
    {
        private readonly IAnimalInteractionPolicy _policy;
        private readonly AnimalFactory _factory;
        private readonly AnimalEventBus _events;
        private readonly uint _seed;

        [Inject]
        public AnimalInteractionResolver(IAnimalInteractionPolicy policy, AnimalFactory factory,
            AnimalEventBus events) : this(policy, factory, events, 0)
        {
        }

        public AnimalInteractionResolver(IAnimalInteractionPolicy policy, AnimalFactory factory,
            AnimalEventBus events, uint seed)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _seed = seed;
        }

        public void Resolve(AnimalContact contact)
        {
            if (contact == null)
            {
                return;
            }

            Animal actor = contact.Actor;
            Animal other = contact.Other;
            if (actor == null || other == null || ReferenceEquals(actor, other) ||
                actor.ActivationId <= 0 || actor.ActivationId != contact.ActorActivationId ||
                other.ActivationId <= 0 || other.ActivationId != contact.OtherActivationId ||
                !actor.IsAlive || !other.IsAlive ||
                actor.Interaction == null || other.Interaction == null ||
                actor.Definition == null || other.Definition == null)
            {
                return;
            }

            Animal first = actor.ActivationId < other.ActivationId ? actor : other;
            Animal second = ReferenceEquals(first, actor) ? other : actor;
            uint tieBreak = DeriveTieBreak(first.ActivationId, second.ActivationId, _seed);
            InteractionDecision decision = _policy.Evaluate(first.Interaction, second.Interaction, tieBreak);
            Animal eater;
            Animal victim;
            if (decision == InteractionDecision.AConsumesB)
            {
                eater = first;
                victim = second;
            }
            else if (decision == InteractionDecision.BConsumesA)
            {
                eater = second;
                victim = first;
            }
            else
            {
                return;
            }

            if (!victim.TryDie())
            {
                return;
            }

            AnimalDefinition definition = victim.Definition;
            _events.PublishDied(new AnimalDied(victim.ActivationId, definition.Id, definition.Role));
            _events.PublishConsumed(new AnimalConsumed(eater.ActivationId, eater.transform.position));
            _factory.Release(victim, victim.ActivationId);
        }

        internal static uint DeriveTieBreak(long firstActivationId, long secondActivationId, uint seed)
        {
            unchecked
            {
                uint hash = 2166136261u ^ seed;
                hash = (hash ^ (uint)firstActivationId) * 16777619u;
                hash = (hash ^ (uint)(firstActivationId >> 32)) * 16777619u;
                hash = (hash ^ (uint)secondActivationId) * 16777619u;
                hash = (hash ^ (uint)(secondActivationId >> 32)) * 16777619u;
                return hash;
            }
        }
    }
}
