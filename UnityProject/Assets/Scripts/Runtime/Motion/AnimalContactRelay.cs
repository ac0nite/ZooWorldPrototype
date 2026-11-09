using UnityEngine;
using VContainer;
using ZooWorld.Core;
using ZooWorld.Interaction;
using ZooWorld.Systems;

namespace ZooWorld.Motion
{
    [RequireComponent(typeof(Animal))]
    public sealed class AnimalContactRelay : MonoBehaviour
    {
        private Animal _animal;
        private AnimalInteractionResolver _resolver;
        private bool _warnedMissingResolver;

        [Inject]
        public void Construct(AnimalInteractionResolver resolver)
        {
            _resolver = resolver;
        }

        private void Awake()
        {
            _animal = GetComponent<Animal>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_animal == null || !_animal.IsAlive || collision.contactCount == 0)
            {
                return;
            }

            if (_resolver == null)
            {
                if (!_warnedMissingResolver)
                {
                    _warnedMissingResolver = true;
                    Debug.LogWarning("AnimalContactRelay has no resolver injected; contacts are ignored.", this);
                }

                return;
            }

            Animal other = collision.collider.GetComponentInParent<Animal>();
            if (other == null)
            {
                return;
            }

            var contact = new AnimalContact(_animal, _animal.ActivationId, other, other.ActivationId,
                collision.GetContact(0).point);
            _resolver.Resolve(contact);
        }
    }
}
