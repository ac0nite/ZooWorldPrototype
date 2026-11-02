using System;
using System.Collections.Generic;

namespace ZooWorld.Interaction
{
    public sealed class InteractionData
    {
        private readonly HashSet<string> _edibleCategoryIds;

        public string CategoryId { get; }

        public InteractionData(string categoryId, IEnumerable<string> edibleCategoryIds)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                throw new ArgumentException("Category ID must not be empty.", nameof(categoryId));
            }

            if (edibleCategoryIds == null)
            {
                throw new ArgumentNullException(nameof(edibleCategoryIds));
            }

            CategoryId = categoryId;
            _edibleCategoryIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in edibleCategoryIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("Edible category ID must not be empty.", nameof(edibleCategoryIds));
                }

                _edibleCategoryIds.Add(id);
            }
        }

        public bool CanConsume(InteractionData other)
        {
            return _edibleCategoryIds.Contains(other.CategoryId);
        }
    }
}