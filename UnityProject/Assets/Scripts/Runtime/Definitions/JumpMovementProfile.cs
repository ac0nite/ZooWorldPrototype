using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Movement;

namespace ZooWorld.Definitions
{
    [CreateAssetMenu(menuName = "Zoo World/Jump Movement Profile")]
    public sealed class JumpMovementProfile : MovementProfile
    {
        [SerializeField] float distance = 2;
        [SerializeField] float height = 1;
        [SerializeField] float waitInterval = 1;
        public float Distance => distance;
        public float Height => height;
        public float WaitInterval => waitInterval;

        public override void Validate()
        {
            RequirePositive(distance, nameof(distance));
            RequirePositive(height, nameof(height));
            RequirePositive(waitInterval, nameof(waitInterval));
        }

        public override IMovementStrategy CreateStrategy(MovementContext context)
        {
            Validate();
            return new JumpMovementStrategy(context, distance, height, waitInterval);
        }
    }
}
