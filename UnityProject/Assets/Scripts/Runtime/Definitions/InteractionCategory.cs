using UnityEngine;

namespace ZooWorld.Definitions
{
    [CreateAssetMenu(menuName = "Zoo World/Interaction Category")]
    public sealed class InteractionCategory : ScriptableObject
    {
        [SerializeField] private string _id;

        public string Id => _id;
    }
}
