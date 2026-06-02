using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.View.Scene.ModuleScene.GameUI.ScreenStack
{
    public sealed class DungeonLayerPopupViewData
    {
        public DungeonLayerPopupViewData(
            IReadOnlyList<DungeonLayerMonsterSpawnViewData> monsters,
            IReadOnlyList<DungeonLayerItemDropViewData> drops)
        {
            Monsters = (monsters ?? Array.Empty<DungeonLayerMonsterSpawnViewData>()).ToArray();
            Drops = (drops ?? Array.Empty<DungeonLayerItemDropViewData>()).ToArray();
        }

        public IReadOnlyList<DungeonLayerMonsterSpawnViewData> Monsters { get; }
        public IReadOnlyList<DungeonLayerItemDropViewData> Drops { get; }
    }
}

