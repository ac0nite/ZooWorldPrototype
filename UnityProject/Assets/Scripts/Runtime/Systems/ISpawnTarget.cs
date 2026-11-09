using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Definitions;

namespace ZooWorld.Systems
{
    public interface ISpawnTarget
    {
        int ActiveCount { get; }
        Animal Rent(AnimalDefinition definition, Vector3 position, Vector3 direction);
        bool IsPositionFree(Vector3 position, float clearance);
    }
}
