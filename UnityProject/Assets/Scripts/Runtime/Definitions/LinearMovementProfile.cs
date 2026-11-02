using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Movement;

namespace ZooWorld.Definitions
{
    [CreateAssetMenu(menuName = "Zoo World/Linear Movement Profile")]
    public sealed class LinearMovementProfile : MovementProfile
    {
        [SerializeField] private float _speed = 3;

        public float Speed => _speed;

        public override void Validate() => RequirePositive(_speed, nameof(_speed));

        public override IMovementStrategy CreateStrategy(MovementContext context)
        {
            Validate();
            return new LinearMovementStrategy(context, _speed);
        }
    }
}
