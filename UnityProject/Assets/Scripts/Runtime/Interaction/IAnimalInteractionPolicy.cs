namespace ZooWorld.Interaction
{
    public interface IAnimalInteractionPolicy
    {
        public InteractionDecision Evaluate(InteractionData a, InteractionData b, uint tieBreak);
    }
}
