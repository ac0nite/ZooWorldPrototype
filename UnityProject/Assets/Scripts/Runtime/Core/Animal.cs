using System;
using UnityEngine;

namespace ZooWorld.Core
{
    public sealed class Animal : MonoBehaviour
    {
        public long ActivationId { get; private set; }
        public bool IsAlive { get; private set; }

        public void Activate(long activationId)
        {
            if (activationId <= 0) throw new ArgumentOutOfRangeException(nameof(activationId));
            if (ActivationId != 0) throw new InvalidOperationException("Release the previous activation before reuse.");
            ActivationId = activationId;
            IsAlive = true;
        }

        public bool TryDie()
        {
            if (!IsAlive) return false;
            IsAlive = false;
            return true;
        }

        public void Deactivate()
        {
            IsAlive = false;
            ActivationId = 0;
        }
    }
}
