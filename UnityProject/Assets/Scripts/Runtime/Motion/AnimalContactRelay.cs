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
            if (_resolver == null || _animal == null || !_animal.IsAlive || collision.contactCount == 0)
            {
                return;
            }

            Animal other = collision.collider.GetComponentInParent<Animal>();
            if (other == null)
            {
                return;
            }

            var contact = new AnimalContact(_animal, _animal.ActivationId, other, other.ActivationId,
                collision.GetContact(0).point, collision.relativeVelocity);
            _resolver.Resolve(contact);
        }
    }
}
