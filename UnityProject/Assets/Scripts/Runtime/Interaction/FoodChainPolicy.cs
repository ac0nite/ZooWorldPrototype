namespace ZooWorld.Interaction
{
    public sealed class FoodChainPolicy : IAnimalInteractionPolicy
    {
        public InteractionDecision Evaluate(InteractionData a, InteractionData b, uint tieBreak)
        {
            var aConsumesB = a.CanConsume(b);
            var bConsumesA = b.CanConsume(a);
            if (aConsumesB && bConsumesA)
                return (tieBreak & 1u) == 0 ? InteractionDecision.AConsumesB : InteractionDecision.BConsumesA;
            if (aConsumesB) return InteractionDecision.AConsumesB;
            return bConsumesA ? InteractionDecision.BConsumesA : InteractionDecision.None;
        }
    }
}
