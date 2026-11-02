using UnityEngine;

namespace ZooWorld.Definitions
{
    [CreateAssetMenu(menuName = "Zoo World/Interaction Category")]
    public sealed class InteractionCategory : ScriptableObject
    {
        [SerializeField] string id;
        public string Id => id;
    }
}
