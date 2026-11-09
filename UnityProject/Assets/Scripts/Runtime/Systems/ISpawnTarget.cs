using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Definitions;

namespace ZooWorld.Systems
{
    public interface ISpawnTarget
    {
        int ActiveCount { get; }
        bool TryRent(AnimalDefinition definition, Vector3 position, Vector3 direction, out Animal animal);
        bool IsPositionFree(Vector3 position, float clearance);
    }
}
