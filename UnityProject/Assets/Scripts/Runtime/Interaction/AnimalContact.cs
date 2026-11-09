using UnityEngine;
using ZooWorld.Core;

namespace ZooWorld.Interaction
{
    public readonly struct AnimalContact
    {
        public Animal Actor { get; }
        public long ActorActivationId { get; }
        public Animal Other { get; }
        public long OtherActivationId { get; }
        public Vector3 Position { get; }

        public AnimalContact(Animal actor, long actorActivationId, Animal other, long otherActivationId,
            Vector3 position)
        {
            Actor = actor;
            Other = other;
            ActorActivationId = actorActivationId;
            OtherActivationId = otherActivationId;
            Position = position;
        }
    }
}
