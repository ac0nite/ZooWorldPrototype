namespace ZooWorld.Interaction
{
    public interface IAnimalInteractionPolicy
    {
        InteractionDecision Evaluate(InteractionData a, InteractionData b, uint tieBreak);
    }
}
