using UnityEngine;

namespace DungeonInn.Core
{
    [CreateAssetMenu(fileName = "RootLifetimeScopeSettings", menuName = "Scriptable Objects/RootLifetimeScopeSettings")]
    public class RootLifetimeScopeSettings : ScriptableObject
    {
        [SerializeField] ProductLifetimeScope productLifetimeScopePrefab;

        public ProductLifetimeScope ProductLifetimeScopePrefab => productLifetimeScopePrefab;
    }
}