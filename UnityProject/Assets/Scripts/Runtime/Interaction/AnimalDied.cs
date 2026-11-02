using ZooWorld.Core;

namespace ZooWorld.Interaction
{
    public readonly struct AnimalDied
    {
        public long ActivationId { get; }
        public string SpeciesId { get; }
        public AnimalRole Role { get; }

        public AnimalDied(long activationId, string speciesId, AnimalRole role)
        {
            ActivationId = activationId;
            SpeciesId = speciesId;
            Role = role;
        }
    }
}
