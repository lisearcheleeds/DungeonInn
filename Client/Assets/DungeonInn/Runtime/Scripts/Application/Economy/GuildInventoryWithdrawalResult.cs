using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.Application.Economy
{
    public sealed class GuildInventoryWithdrawalResult
    {
        public GuildInventoryWithdrawalResult(IReadOnlyList<GuildInventoryWithdrawalSource> sources)
        {
            Sources = (sources ?? Array.Empty<GuildInventoryWithdrawalSource>()).ToArray();
        }

        public IReadOnlyList<GuildInventoryWithdrawalSource> Sources { get; }
    }
}
