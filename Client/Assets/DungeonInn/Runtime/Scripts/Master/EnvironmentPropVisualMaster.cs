using System;

namespace DungeonInn.Master
{
    public sealed class EnvironmentPropVisualMaster
    {
        public EnvironmentPropVisualMaster(string key, string prefabAddress)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Environment prop visual key is required.", nameof(key));
            }

            Key = key;
            PrefabAddress = prefabAddress ?? string.Empty;
        }

        public string Key { get; }
        public string PrefabAddress { get; }
    }
}
