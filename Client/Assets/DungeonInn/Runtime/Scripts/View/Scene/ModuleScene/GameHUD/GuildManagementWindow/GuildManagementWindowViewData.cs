using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class GuildManagementWindowViewData
    {
        public GuildManagementWindowViewData(
            IReadOnlyList<string> kpis,
            IReadOnlyList<GuildFacilityRowViewData> facilities,
            IReadOnlyList<string> inventory,
            IReadOnlyList<string> transactions)
        {
            Kpis = (kpis ?? Array.Empty<string>()).ToArray();
            Facilities = (facilities ?? Array.Empty<GuildFacilityRowViewData>()).ToArray();
            Inventory = (inventory ?? Array.Empty<string>()).ToArray();
            Transactions = (transactions ?? Array.Empty<string>()).ToArray();
        }

        public IReadOnlyList<string> Kpis { get; }
        public IReadOnlyList<GuildFacilityRowViewData> Facilities { get; }
        public IReadOnlyList<string> Inventory { get; }
        public IReadOnlyList<string> Transactions { get; }
    }
}
