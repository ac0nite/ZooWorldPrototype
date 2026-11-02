using System;
using System.Collections.Generic;
using NUnit.Framework;
using ZooWorld.Definitions;

namespace ZooWorld.Tests
{
    [Category("AC08")]
    public sealed class SpeciesIdGeneratorTests
    {
        [Test]
        public void GeneratedIdIsANonEmptyGuidInCompactFormat()
        {
            string id = SpeciesIdGenerator.Generate();

            Assert.That(id.Length, Is.EqualTo(32));
            Assert.That(Guid.TryParseExact(id, "N", out Guid guid), Is.True);
            Assert.That(guid, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void RepeatedCallsProduceDistinctIds()
        {
            var ids = new HashSet<string>();
            for (var i = 0; i < 1000; i++)
            {
                Assert.That(ids.Add(SpeciesIdGenerator.Generate()), Is.True);
            }
        }
    }
}
