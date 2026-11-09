using UnityEngine;
using ZooWorld.Definitions;
using ZooWorld.Interaction;
using ZooWorld.Movement;
using ZooWorld.Motion;
using ZooWorld.Systems;

namespace ZooWorld.Core
{
    public sealed class Animal : MonoBehaviour
    {
        private IMovementStrategy _movement;
        private IAnimalMotor _motor;
        private ArenaBounds _bounds;

        public long ActivationId { get; private set; }
        public bool IsAlive { get; private set; }
        public AnimalDefinition Definition { get; private set; }
        public InteractionData Interaction { get; private set; }

        internal bool TryActivate(long activationId)
        {
            if (activationId <= 0 || ActivationId != 0)
            {
                return false;
            }

            ActivationId = activationId;
            IsAlive = true;
            return true;
        }

        public bool TryActivate(long activationId, AnimalDefinition definition, MovementContext context)
        {
            if (activationId <= 0 || ActivationId != 0 || definition == null || context == null)
            {
                return false;
            }

            InteractionData interaction = definition.InteractionProfile.CreateSnapshot();
            IMovementStrategy movement = definition.MovementProfile.CreateStrategy(context);
            Debug.Assert(movement != null, "Movement profile must return a strategy.");
            if (movement == null)
            {
                return false;
            }

            _movement = movement;
            _motor = context.Motor;
            _bounds = context.Bounds;
            Definition = definition;
            Interaction = interaction;
            ActivationId = activationId;
            IsAlive = true;
            return true;
        }

        public bool TryDie()
        {
            if (!IsAlive)
            {
                return false;
            }

            IsAlive = false;
            return true;
        }

        public void Deactivate()
        {
            IsAlive = false;
            ActivationId = 0;
            Definition = null;
            Interaction = null;
            IMovementStrategy movement = _movement;
            _movement = null;
            _motor = null;
            _bounds = null;
            movement?.Dispose();
        }

        internal void FixedUpdate()
        {
            if (!IsAlive)
            {
                return;
            }

            if (_bounds != null && _motor != null &&
                _bounds.TryGetInwardDirection(_motor.Position, out Vector3 direction))
            {
                _movement?.SteerInward(direction);
            }

            _movement?.FixedTick(Time.fixedDeltaTime);
        }

        private void OnDestroy()
        {
            Deactivate();
        }
    }
}
