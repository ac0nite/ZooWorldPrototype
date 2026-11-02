using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Movement;

namespace ZooWorld.Definitions
{
    [CreateAssetMenu(menuName = "Zoo World/Linear Movement Profile")]
    public sealed class LinearMovementProfile : MovementProfile
    {
        [SerializeField] float speed = 3;
        public float Speed => speed;

        public override void Validate() => RequirePositive(speed, nameof(speed));

        public override IMovementStrategy CreateStrategy(MovementContext context)
        {
            Validate();
            return new LinearMovementStrategy(context, speed);
        }
    }
}
