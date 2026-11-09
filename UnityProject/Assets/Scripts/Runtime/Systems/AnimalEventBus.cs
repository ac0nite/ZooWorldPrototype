using System;
using UnityEngine;
using ZooWorld.Interaction;

namespace ZooWorld.Systems
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class AnimalEventBus
    {
        public event Action<AnimalDied> Died;
        public event Action<AnimalConsumed> Consumed;

        internal void PublishDied(AnimalDied payload)
        {
            InvokeSafely(Died, payload);
        }

        internal void PublishConsumed(AnimalConsumed payload)
        {
            InvokeSafely(Consumed, payload);
        }

        private static void InvokeSafely<T>(Action<T> handlers, T payload)
        {
            if (handlers == null)
            {
                return;
            }

            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            // invocation list of Action<T> contains only Action<T> — the cast is safe.
            foreach (Action<T> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(payload);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
