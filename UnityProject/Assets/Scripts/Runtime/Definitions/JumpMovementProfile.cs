using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Movement;

namespace ZooWorld.Definitions
{
    [CreateAssetMenu(menuName = "Zoo World/Jump Movement Profile")]
    public sealed class JumpMovementProfile : MovementProfile
    {
        [SerializeField] private float _distance = 2;
        [SerializeField] private float _height = 1;
        [SerializeField] private float _waitInterval = 1;

        public float Distance => _distance;
        public float Height => _height;
        public float WaitInterval => _waitInterval;

        public override void Validate()
        {
            RequirePositive(_distance, nameof(_distance));
            RequirePositive(_height, nameof(_height));
            RequirePositive(_waitInterval, nameof(_waitInterval));
            ValidateTuning();
        }

        public override IMovementStrategy CreateStrategy(MovementContext context)
        {
            return new JumpMovementStrategy(context, _distance, _height, _waitInterval, Tuning);
        }
    }
}
