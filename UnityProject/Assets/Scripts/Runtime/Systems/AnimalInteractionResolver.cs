using VContainer;
using ZooWorld.Core;
using ZooWorld.Interaction;

namespace ZooWorld.Systems
{
    public sealed class AnimalInteractionResolver
    {
        private readonly IAnimalInteractionPolicy _policy;
        private readonly AnimalFactory _factory;
        private readonly AnimalEventBus _events;
        private readonly uint _seed;

        public long ContactsReceived { get; private set; }

        private const uint FnvOffsetBasis = 2166136261u;
        private const uint FnvPrime = 16777619u;

        [Inject]
        public AnimalInteractionResolver(IAnimalInteractionPolicy policy, AnimalFactory factory,
            AnimalEventBus events) : this(policy, factory, events, 0)
        {
        }

        public AnimalInteractionResolver(IAnimalInteractionPolicy policy, AnimalFactory factory,
            AnimalEventBus events, uint seed)
        {
            _policy = policy;
            _factory = factory;
            _events = events;
            _seed = seed;
        }

        internal uint Seed => _seed;

        public void Resolve(AnimalContact contact)
        {
            ContactsReceived++;
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

            var definition = victim.Definition;
            long victimActivationId = victim.ActivationId;
            long eaterActivationId = eater.ActivationId;
            var eaterPosition = eater.transform.position;
            try
            {
                _events.PublishDied(new AnimalDied(victimActivationId, definition.Id, definition.Role));
                _events.PublishConsumed(new AnimalConsumed(eaterActivationId, eaterPosition));
            }
            finally
            {
                if (!_factory.Release(victim, victimActivationId))
                {
                    victim.Deactivate();
                    victim.gameObject.SetActive(false);
                }
            }
        }

        internal static uint DeriveTieBreak(long firstActivationId, long secondActivationId, uint seed)
        {
            unchecked
            {
                var hash = FnvOffsetBasis ^ seed;
                hash = (hash ^ (uint)firstActivationId) * FnvPrime;
                hash = (hash ^ (uint)(firstActivationId >> 32)) * FnvPrime;
                hash = (hash ^ (uint)secondActivationId) * FnvPrime;
                hash = (hash ^ (uint)(secondActivationId >> 32)) * FnvPrime;
                return hash;
            }
        }
    }
}