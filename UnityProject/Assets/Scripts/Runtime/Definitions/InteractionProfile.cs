using System;
using UnityEngine;
using ZooWorld.Interaction;

namespace ZooWorld.Definitions
{
    [CreateAssetMenu(menuName = "Zoo World/Interaction Profile")]
    public sealed class InteractionProfile : ScriptableObject
    {
        [SerializeField] InteractionCategory category;
        [SerializeField] InteractionCategory[] edibleCategories = Array.Empty<InteractionCategory>();

        public InteractionData CreateSnapshot()
        {
            if (category == null || string.IsNullOrWhiteSpace(category.Id))
                throw new InvalidOperationException($"Interaction profile '{name}' requires a category with a non-empty ID.");
            if (edibleCategories == null)
                throw new InvalidOperationException($"Interaction profile '{name}' has a null edible category collection.");
            var ids = new string[edibleCategories.Length];
            for (var i = 0; i < edibleCategories.Length; i++)
            {
                if (edibleCategories[i] == null || string.IsNullOrWhiteSpace(edibleCategories[i].Id))
                    throw new InvalidOperationException($"Interaction profile '{name}' has an invalid edible category at index {i}.");
                ids[i] = edibleCategories[i].Id;
            }
            return new InteractionData(category.Id, ids);
        }
    }
}
