using NUnit.Framework;
using ZooWorld.Diagnostics;

namespace ZooWorld.Tests
{
    public sealed class DiagnosticsTests
    {
        [Test, Description("Парсер prefill читает только корректные положительные значения")]
        public void ParsePrefillTargetReadsOnlyWellFormedPositiveValues()
        {
            Assert.That(SimulationDiagnostics.ParsePrefillTarget(null), Is.EqualTo(0));
            Assert.That(SimulationDiagnostics.ParsePrefillTarget(new string[0]), Is.EqualTo(0));
            Assert.That(SimulationDiagnostics.ParsePrefillTarget(
                new[] { "-screen-width", "1280" }), Is.EqualTo(0));
            Assert.That(SimulationDiagnostics.ParsePrefillTarget(
                new[] { "-zooworld-prefill=100" }), Is.EqualTo(100));
            Assert.That(SimulationDiagnostics.ParsePrefillTarget(
                new[] { "-zooworld-prefill=abc", "-zooworld-prefill=7" }), Is.EqualTo(7));
            Assert.That(SimulationDiagnostics.ParsePrefillTarget(
                new[] { "-zooworld-prefill=0", "-zooworld-prefill=-5" }), Is.EqualTo(0));
            Assert.That(SimulationDiagnostics.ParsePrefillTarget(
                new[] { "-zooworld-prefill=" }), Is.EqualTo(0));
        }
    }
}
