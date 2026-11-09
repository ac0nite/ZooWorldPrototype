using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using VContainer;
using VContainer.Unity;
using ZooWorld.Definitions;
using ZooWorld.Systems;

namespace ZooWorld.Diagnostics
{
    public sealed class SimulationDiagnostics : IStartable, ITickable, IDisposable
    {
        public const string PrefillArgument = "-zooworld-prefill=";
        private const double ReportIntervalSeconds = 1;

        private readonly AnimalFactory _factory;
        private readonly AnimalCatalog _catalog;
        private readonly SimulationSettings _settings;
        private readonly AnimalInteractionResolver _resolver;
        private readonly GameStats _stats;
        private readonly System.Random _prefillRandom = new System.Random(0x5EED);

        private double _windowStart;
        private int _windowFrames;
        private float _windowWorstDelta;
        private long _windowPairsStart;
        private long _windowContactsStart;
        private long _windowAllocatedStart;
        private int _windowDeathsStart;
        private long _contactPairs;
        private int _targetPopulation;

        [Inject]
        public SimulationDiagnostics(AnimalFactory factory, AnimalCatalog catalog,
            SimulationSettings settings, AnimalInteractionResolver resolver, GameStats stats)
        {
            _factory = factory;
            _catalog = catalog;
            _settings = settings;
            _resolver = resolver;
            _stats = stats;
        }

        public void Start()
        {
            Physics.ContactEvent += OnPhysicsContact;
            _targetPopulation = ParsePrefillTarget(Environment.GetCommandLineArgs());
            int prefilled = Prefill(_targetPopulation);
            Debug.Log($"[ZooDiag] start unity={Application.unityVersion} dev={Debug.isDebugBuild} " +
                $"dt={Time.fixedDeltaTime:F3} targetFps={Application.targetFrameRate} " +
                $"vsync={QualitySettings.vSyncCount} res={Screen.width}x{Screen.height} prefilled={prefilled}");
            ResetWindow(Time.realtimeSinceStartupAsDouble);
        }

        public void Tick()
        {
            Prefill(_targetPopulation);
            _windowFrames++;
            float delta = Time.unscaledDeltaTime;
            if (delta > _windowWorstDelta)
            {
                _windowWorstDelta = delta;
            }

            double now = Time.realtimeSinceStartupAsDouble;
            double elapsed = now - _windowStart;
            if (elapsed < ReportIntervalSeconds)
            {
                return;
            }

            double allocatedMb = (Profiler.GetTotalAllocatedMemoryLong() - _windowAllocatedStart) / (elapsed * 1024 * 1024);
            Debug.Log(
                $"[ZooDiag] t={now:F1} fps={_windowFrames / elapsed:F1} worstMs={_windowWorstDelta * 1000:F1} " +
                $"cpuMs={CpuFrameMs():F1} gcMB={GC.GetTotalMemory(false) / 1048576.0:F1} " +
                $"allocMBs={allocatedMb:F2} active={_factory.ActiveCount} " +
                $"physPairs={(_contactPairs - _windowPairsStart) / elapsed:F0} " +
                $"contacts={(_resolver.ContactsReceived - _windowContactsStart) / elapsed:F0} " +
                $"deaths+={_stats.PreyDeaths + _stats.PredatorDeaths - _windowDeathsStart}");
            ResetWindow(now);
        }

        public void Dispose()
        {
            Physics.ContactEvent -= OnPhysicsContact;
        }

        public static int ParsePrefillTarget(IEnumerable<string> arguments)
        {
            if (arguments == null)
            {
                return 0;
            }

            foreach (string argument in arguments)
            {
                if (argument != null && argument.StartsWith(PrefillArgument, StringComparison.Ordinal) &&
                    int.TryParse(argument.Substring(PrefillArgument.Length), out int target) && target > 0)
                {
                    return target;
                }
            }

            return 0;
        }

        private int Prefill(int target)
        {
            int desired = Math.Min(target, _settings.LiveCap);
            int prefilled = 0;
            ArenaBounds bounds = _factory.Bounds;
            if (desired <= 0)
            {
                return 0;
            }

            if (bounds == null)
            {
                Debug.LogWarning("[ZooDiag] prefill skipped: no arena bounds registered.");
                return 0;
            }

            IReadOnlyList<AnimalDefinition> definitions = _catalog.Definitions;
            float inset = _settings.SpawnInset;
            float range = 1 - inset * 2;
            while (_factory.ActiveCount < desired)
            {
                AnimalDefinition definition = definitions[_prefillRandom.Next(definitions.Count)];
                var normalized = new Vector2(
                    inset + range * (float)_prefillRandom.NextDouble(),
                    inset + range * (float)_prefillRandom.NextDouble());
                if (!bounds.TryGetPoint(normalized, out Vector3 position))
                {
                    Debug.LogWarning("[ZooDiag] prefill stopped: arena bounds projection failed.");
                    break;
                }

                position.y += _settings.SpawnHeight;
                double angle = _prefillRandom.NextDouble() * Math.PI * 2;
                if (!_factory.TryRent(definition, position,
                        new Vector3((float)Math.Cos(angle), 0, (float)Math.Sin(angle)), out _))
                {
                    Debug.LogWarning("[ZooDiag] prefill stopped: factory rent failed.");
                    break;
                }

                prefilled++;
            }

            return prefilled;
        }

        private void OnPhysicsContact(PhysicsScene scene, NativeArray<ContactPairHeader>.ReadOnly headers)
        {
            long pairs = 0;
            for (var i = 0; i < headers.Length; i++)
            {
                pairs += headers[i].pairCount;
            }

            Interlocked.Add(ref _contactPairs, pairs);
        }

        private void ResetWindow(double now)
        {
            _windowStart = now;
            _windowFrames = 0;
            _windowWorstDelta = 0;
            _windowPairsStart = _contactPairs;
            _windowContactsStart = _resolver.ContactsReceived;
            _windowAllocatedStart = Profiler.GetTotalAllocatedMemoryLong();
            _windowDeathsStart = _stats.PreyDeaths + _stats.PredatorDeaths;
        }

        private static float CpuFrameMs()
        {
            FrameTimingManager.CaptureFrameTimings();
            var timings = new FrameTiming[1];
            return FrameTimingManager.GetLatestTimings(1, timings) > 0 ? (float)timings[0].cpuFrameTime : -1;
        }
    }
}
