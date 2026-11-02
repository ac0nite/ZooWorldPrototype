using System;
using UnityEngine;
using ZooWorld.Physics;
using ZooWorld.Core;

namespace ZooWorld.Definitions
{
    [CreateAssetMenu(menuName = "Zoo World/Animal Definition")]
    public sealed class AnimalDefinition : ScriptableObject
    {
        [SerializeField] private string _id = SpeciesIdGenerator.Generate();
        [SerializeField] private string _displayName;
        [SerializeField] private AnimalRole _role;
        [SerializeField] private Animal _prefab;
        [SerializeField] private MovementProfile _movementProfile;
        [SerializeField] private InteractionProfile _interactionProfile;
        [SerializeField] private float _spawnWeight = 1;

        public string Id => _id;
        public string DisplayName => _displayName;
        public AnimalRole Role => _role;
        public Animal Prefab => _prefab;
        public MovementProfile MovementProfile => _movementProfile;
        public InteractionProfile InteractionProfile => _interactionProfile;
        public float SpawnWeight => _spawnWeight;

        [ContextMenu("Validate")]
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                throw new InvalidOperationException($"Species '{name}' requires a non-empty ID.");
            }

            if (float.IsNaN(_spawnWeight) || float.IsInfinity(_spawnWeight) || _spawnWeight < 0)
            {
                throw new InvalidOperationException($"Species '{name}' ({_id}) requires a finite non-negative spawn weight.");
            }

            if (_prefab == null || _movementProfile == null || _interactionProfile == null)
            {
                throw new InvalidOperationException($"Species '{name}' ({_id}) requires a prefab, movement profile and interaction profile.");
            }

            if (_prefab.GetComponent<Rigidbody>() == null ||
                _prefab.GetComponent<Collider>() == null ||
                _prefab.GetComponent<IAnimalMotor>() == null ||
                _prefab.GetComponent<AnimalContactRelay>() == null)
            {
                throw new InvalidOperationException($"Species '{name}' ({_id}) prefab requires Rigidbody, Collider, IAnimalMotor and AnimalContactRelay components on its root.");
            }

            _movementProfile.Validate();
            _interactionProfile.Validate();
        }
    }
}
