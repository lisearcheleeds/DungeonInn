using System;
using System.Linq;
using DungeonInn.Application.World;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Economy
{
    public sealed class GuildProgressService
    {
        readonly IGameWorldStateReader worldState;

        [Inject]
        public GuildProgressService(IGameWorldStateReader worldState)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
        }

        public int TotalFacilityLevel
        {
            get
            {
                if (!worldState.IsInitialized)
                {
                    return 0;
                }

                return worldState.Guild.Facilities.Sum(x => x.Level);
            }
        }

        public bool IsMarketOfferUnlocked(MarketOfferMaster offer)
        {
            if (offer == null)
            {
                throw new ArgumentNullException(nameof(offer));
            }

            return offer.RequiredGuildTotalLevel <= TotalFacilityLevel;
        }
    }
}
