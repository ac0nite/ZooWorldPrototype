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
            CategoryId = categoryId;
            _edibleCategoryIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in edibleCategoryIds)
            {
                _edibleCategoryIds.Add(id);
            }
        }

        public bool CanConsume(InteractionData other)
        {
            return _edibleCategoryIds.Contains(other.CategoryId);
        }
    }
}