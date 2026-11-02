using UnityEngine;

namespace ZooWorld.Interaction
{
    public readonly struct AnimalConsumed
    {
        public long EaterActivationId { get; }
        public Vector3 Position { get; }

        public AnimalConsumed(long eaterActivationId, Vector3 position)
        {
            EaterActivationId = eaterActivationId;
            Position = position;
        }
    }
}
