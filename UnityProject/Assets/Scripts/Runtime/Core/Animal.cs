using System;
using UnityEngine;
using ZooWorld.Definitions;
using ZooWorld.Interaction;
using ZooWorld.Movement;

namespace ZooWorld.Core
{
    public sealed class Animal : MonoBehaviour
    {
        private IMovementStrategy _movement;

        public long ActivationId { get; private set; }
        public bool IsAlive { get; private set; }
        public AnimalDefinition Definition { get; private set; }
        public InteractionData Interaction { get; private set; }

        public void Activate(long activationId)
        {
            ValidateActivation(activationId);
            ActivationId = activationId;
            IsAlive = true;
        }

        public void Activate(long activationId, AnimalDefinition definition, MovementContext context)
        {
            ValidateActivation(activationId);
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            InteractionData interaction = definition.InteractionProfile.CreateSnapshot();
            IMovementStrategy movement = definition.MovementProfile.CreateStrategy(context);
            _movement = movement ?? throw new InvalidOperationException("Movement profile returned no strategy.");
            Definition = definition;
            Interaction = interaction;
            ActivationId = activationId;
            IsAlive = true;
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
            movement?.Dispose();
        }

        private void FixedUpdate()
        {
            if (IsAlive)
            {
                _movement?.FixedTick(Time.fixedDeltaTime);
            }
        }

        private void OnDestroy()
        {
            Deactivate();
        }

        private void ValidateActivation(long activationId)
        {
            if (activationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(activationId));
            }

            if (ActivationId != 0)
            {
                throw new InvalidOperationException("Release the previous activation before reuse.");
            }
        }
    }
}
