using UnityEngine;

namespace YourProduct.Core
{
    [CreateAssetMenu(fileName = "RootLifetimeScopeSettings", menuName = "Scriptable Objects/RootLifetimeScopeSettings")]
    public class RootLifetimeScopeSettings : ScriptableObject
    {
        [SerializeField] ProductLifetimeScope productLifetimeScopePrefab;

        public ProductLifetimeScope ProductLifetimeScopePrefab => productLifetimeScopePrefab;
    }
}