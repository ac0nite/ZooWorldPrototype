using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZooWorld.Definitions
{
    [CreateAssetMenu(menuName = "Zoo World/Animal Catalog")]
    public sealed class AnimalCatalog : ScriptableObject
    {
        [SerializeField] private AnimalDefinition[] _definitions = Array.Empty<AnimalDefinition>();

        private IReadOnlyList<AnimalDefinition> _definitionsReadOnly;

        public IReadOnlyList<AnimalDefinition> Definitions =>
            _definitionsReadOnly ??= Array.AsReadOnly(_definitions);

        [ContextMenu("Validate")]
        public void Validate()
        {
            if (_definitions == null || _definitions.Length == 0)
            {
                throw new InvalidOperationException($"Catalog '{name}' requires at least one species.");
            }

            var speciesIds = new HashSet<string>(StringComparer.Ordinal);
            var categories = new Dictionary<string, (InteractionCategory Category, string Source)>(StringComparer.Ordinal);
            double totalWeight = 0;
            for (var i = 0; i < _definitions.Length; i++)
            {
                AnimalDefinition definition = _definitions[i];
                if (definition == null)
                {
                    throw new InvalidOperationException($"Catalog '{name}' has a null species at index {i}.");
                }

                definition.Validate();
                if (!speciesIds.Add(definition.Id))
                {
                    throw new InvalidOperationException($"Catalog '{name}' has duplicate species ID '{definition.Id}' at '{definition.name}'.");
                }

                totalWeight += definition.SpawnWeight;
                InteractionProfile profile = definition.InteractionProfile;
                RegisterCategory(profile.Category, profile.name, categories);
                foreach (var category in profile.EdibleCategories)
                {
                    RegisterCategory(category, profile.name, categories);
                }
            }

            if (totalWeight <= 0)
            {
                throw new InvalidOperationException($"Catalog '{name}' requires a positive total spawn weight.");
            }
        }

        private void RegisterCategory(
            InteractionCategory category,
            string source,
            Dictionary<string, (InteractionCategory Category, string Source)> categories)
        {
            if (categories.TryGetValue(category.Id, out var existing) && existing.Category != category)
            {
                throw new InvalidOperationException(
                    $"Catalog '{name}' has duplicate category ID '{category.Id}' in profiles '{existing.Source}' and '{source}' " +
                    $"on different category assets '{existing.Category.name}' and '{category.name}'.");
            }

            if (!categories.ContainsKey(category.Id))
            {
                categories.Add(category.Id, (category, source));
            }
        }

        private void OnValidate()
        {
            _definitionsReadOnly = null;
        }
    }
}
