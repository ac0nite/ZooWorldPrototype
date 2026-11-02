using System;
using NUnit.Framework;
using ZooWorld.Interaction;

namespace ZooWorld.Tests
{
    public sealed class FoodChainPolicyTests
    {
        readonly IAnimalInteractionPolicy policy = new FoodChainPolicy();

        [TestCase(false, false, 0u, InteractionDecision.None)]
        [TestCase(false, false, 1u, InteractionDecision.None)]
        [TestCase(true, false, 0u, InteractionDecision.AConsumesB)]
        [TestCase(true, false, 1u, InteractionDecision.AConsumesB)]
        [TestCase(false, true, 0u, InteractionDecision.BConsumesA)]
        [TestCase(false, true, 1u, InteractionDecision.BConsumesA)]
        [TestCase(true, true, 0u, InteractionDecision.AConsumesB)]
        [TestCase(true, true, 1u, InteractionDecision.BConsumesA)]
        [TestCase(true, true, 2u, InteractionDecision.AConsumesB)]
        [TestCase(true, true, 3u, InteractionDecision.BConsumesA)]
        [Category("AC03"), Category("AC04")]
        public void PermissionsAndExplicitTieBreakDetermineDecision(bool aEatsB, bool bEatsA,
            uint tieBreak, InteractionDecision expected)
        {
            var a = new InteractionData("a", aEatsB ? new[] { "b" } : Array.Empty<string>());
            var b = new InteractionData("b", bEatsA ? new[] { "a" } : Array.Empty<string>());
            Assert.That(policy.Evaluate(a, b, tieBreak), Is.EqualTo(expected));
        }

        [TestCase(0u), TestCase(1u), Category("AC03")]
        public void InitialPreyAndPredatorPermissionsWorkInBothArgumentOrders(uint tieBreak)
        {
            var prey = new InteractionData("prey", Array.Empty<string>());
            var predator = new InteractionData("predator", new[] { "prey", "predator" });
            Assert.That(policy.Evaluate(prey, prey, tieBreak), Is.EqualTo(InteractionDecision.None));
            Assert.That(policy.Evaluate(predator, prey, tieBreak), Is.EqualTo(InteractionDecision.AConsumesB));
            Assert.That(policy.Evaluate(prey, predator, tieBreak), Is.EqualTo(InteractionDecision.BConsumesA));
        }

        [TestCase(0u, InteractionDecision.AConsumesB)]
        [TestCase(1u, InteractionDecision.BConsumesA)]
        [TestCase(2u, InteractionDecision.AConsumesB)]
        [TestCase(3u, InteractionDecision.BConsumesA)]
        [Category("AC04")]
        public void TwoPredatorsHaveExactlyOneConsumerAndRepeatedEvaluationIsStable(uint tieBreak,
            InteractionDecision expected)
        {
            var first = new InteractionData("predator", new[] { "prey", "predator" });
            var second = new InteractionData("predator", new[] { "prey", "predator" });
            Assert.That(policy.Evaluate(first, second, tieBreak), Is.EqualTo(expected));
            Assert.That(policy.Evaluate(first, second, tieBreak), Is.EqualTo(expected));
            Assert.That(policy.Evaluate(second, first, tieBreak), Is.EqualTo(expected));
        }

        [Test, Category("AC08")]
        public void ThirdFoodCategoryRequiresOnlyNewData()
        {
            var insect = new InteractionData("insect", Array.Empty<string>());
            var frog = new InteractionData("prey", new[] { "insect" });
            Assert.That(policy.Evaluate(frog, insect, 0), Is.EqualTo(InteractionDecision.AConsumesB));
            Assert.That(policy.Evaluate(insect, frog, 0), Is.EqualTo(InteractionDecision.BConsumesA));
        }

        [Test, Category("AC09")]
        public void SnapshotDefensivelyCopiesEdibleCategories()
        {
            var source = new[] { "prey" };
            var predator = new InteractionData("predator", source);
            source[0] = "predator";
            Assert.That(predator.CanConsume(new InteractionData("prey", Array.Empty<string>())), Is.True);
            Assert.That(predator.CanConsume(new InteractionData("predator", Array.Empty<string>())), Is.False);
        }

        [TestCase(null), TestCase(""), TestCase(" "), Category("AC08")]
        public void EmptyCategoryIdsAreRejected(string id)
        {
            Assert.Throws<ArgumentException>(() => new InteractionData(id, Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() => new InteractionData("valid", new[] { id }));
        }
    }
}
