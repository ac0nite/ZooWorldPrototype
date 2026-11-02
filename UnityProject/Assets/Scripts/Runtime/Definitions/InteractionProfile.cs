using System;
using System.Collections.Generic;
using UnityEngine;
using ZooWorld.Interaction;

namespace ZooWorld.Definitions
{
    [CreateAssetMenu(menuName = "Zoo World/Interaction Profile")]
    public sealed class InteractionProfile : ScriptableObject
    {
        [SerializeField] private InteractionCategory _category;
        [SerializeField] private InteractionCategory[] _edibleCategories = Array.Empty<InteractionCategory>();

        public InteractionCategory Category => _category;
        public IReadOnlyList<InteractionCategory> EdibleCategories => Array.AsReadOnly(_edibleCategories);

        public void Validate()
        {
            if (_category == null || string.IsNullOrWhiteSpace(_category.Id))
            {
                throw new InvalidOperationException($"Interaction profile '{name}' requires a category with a non-empty ID.");
            }

            if (_edibleCategories == null)
            {
                throw new InvalidOperationException($"Interaction profile '{name}' has a null edible category collection.");
            }

            for (var i = 0; i < _edibleCategories.Length; i++)
            {
                if (_edibleCategories[i] == null || string.IsNullOrWhiteSpace(_edibleCategories[i].Id))
                {
                    throw new InvalidOperationException($"Interaction profile '{name}' has an invalid edible category at index {i}.");
                }
            }
        }

        public InteractionData CreateSnapshot()
        {
            Validate();
            var ids = new string[_edibleCategories.Length];
            for (var i = 0; i < _edibleCategories.Length; i++)
            {
                ids[i] = _edibleCategories[i].Id;
            }

            return new InteractionData(_category.Id, ids);
        }
    }
}
