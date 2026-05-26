using System;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class DungeonLayerMonsterSpawnViewData
    {
        public DungeonLayerMonsterSpawnViewData(string monsterName, string levelRange, string weight)
        {
            MonsterName = string.IsNullOrWhiteSpace(monsterName)
                ? throw new ArgumentException("Monster name is required.", nameof(monsterName))
                : monsterName;
            LevelRange = levelRange ?? string.Empty;
            Weight = weight ?? string.Empty;
        }

        public string MonsterName { get; }
        public string LevelRange { get; }
        public string Weight { get; }
    }
}
