using UnityEngine;
using VContainer;

namespace ZooWorld.Tests
{
    public sealed class InjectionProbe : MonoBehaviour
    {
        public LifecycleProbe Dependency { get; private set; }
        public bool InjectedBeforeEnable { get; private set; }
        public bool InjectedBeforeStart { get; private set; }
        public int InjectionCount { get; private set; }

        [Inject]
        public void Construct(LifecycleProbe dependency)
        {
            Dependency = dependency;
            InjectionCount++;
        }

        void OnEnable() => InjectedBeforeEnable = Dependency != null;
        void Start() => InjectedBeforeStart = Dependency != null;
    }
}
