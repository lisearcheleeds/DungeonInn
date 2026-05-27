using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using VContainer;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class GameHudScreenStackViewDataFactory
    {
        readonly IDungeonInfoScreenService dungeonInfoScreenService;
        readonly IGuildManagementScreenService guildManagementScreenService;
        readonly IMarketScreenService marketScreenService;

        [Inject]
        public GameHudScreenStackViewDataFactory(
            IDungeonInfoScreenService dungeonInfoScreenService,
            IGuildManagementScreenService guildManagementScreenService,
            IMarketScreenService marketScreenService)
        {
            this.dungeonInfoScreenService =
                dungeonInfoScreenService ?? throw new ArgumentNullException(nameof(dungeonInfoScreenService));
            this.guildManagementScreenService =
                guildManagementScreenService ?? throw new ArgumentNullException(nameof(guildManagementScreenService));
            this.marketScreenService = marketScreenService ?? throw new ArgumentNullException(nameof(marketScreenService));
        }

        public DungeonInfoWindowViewData CreateDungeonInfo()
        {
            var layers = new List<DungeonLayerListItemViewData>();
            foreach (var summary in dungeonInfoScreenService.GetLayers())
            {
                var monsters = summary.MonsterSpawns
                    .Select(monster => new DungeonLayerMonsterSpawnViewData(
                        monster.MonsterName,
                        $"Lv.{monster.MinLevel}-{monster.MaxLevel}",
                        $"Weight {monster.Weight}"))
                    .ToArray();
                var drops = summary.ItemDrops
                    .Select(drop => new DungeonLayerItemDropViewData(
                        drop.ItemName,
                        drop.SourceMonsterName,
                        $"{drop.DropChance:P0}",
                        $"x{drop.MinCount}-{drop.MaxCount}"))
                    .ToArray();

                layers.Add(new DungeonLayerListItemViewData(
                    summary.FloorIndex == 0 ? "Ground" : $"Floor {summary.FloorIndex}",
                    summary.IsGenerated ? "Generated" : "Not generated",
                    $"Adventurers {summary.AdventurerCount} / Monsters {summary.MonsterCount}",
                    new DungeonLayerPopupViewData(monsters, drops)));
            }

            return new DungeonInfoWindowViewData(layers);
        }

        public GuildManagementWindowViewData CreateGuildManagement()
        {
            var summary = guildManagementScreenService.GetStatus();
            var kpi = summary.EconomyKpi;
            var kpis = new[]
            {
                $"Gold: {kpi.Gold:N0}",
                $"Sales: {kpi.Sales:N0}",
                $"Guests: {kpi.Guests:N0}",
                $"Occupancy: {kpi.OccupancyPercent}%",
                $"Reputation: {kpi.Reputation:N0}"
            };
            var facilities = summary.Facilities
                .Select(x => new GuildFacilityRowViewData(
                    x.FacilityId,
                    $"{x.Name} Lv.{x.Level} Q{x.Quality} Capacity {x.Capacity} " +
                    $"Funds {x.Gold:N0} Sales {x.Sales:N0} - {FormatUpgradePreview(x.UpgradePreview)}",
                    x.Lineup.Select(lineup => $"{lineup.ItemName} (Lv.{lineup.RequiredLevel})").ToArray(),
                    x.UpgradePreview.CanUpgrade,
                    FormatUpgradePreview(x.UpgradePreview)))
                .ToArray();
            var inventory = summary.CombinedInventory
                .Take(12)
                .Select(x => $"{x.ItemName}: {x.TotalCount:N0}")
                .ToArray();
            var transactions = summary.Transactions
                .Select(x => x.Description)
                .ToArray();
            return new GuildManagementWindowViewData(kpis, facilities, inventory, transactions);
        }

        public MarketWindowViewData CreateMarket()
        {
            return new MarketWindowViewData(marketScreenService.GetOffers()
                .Select(x => new MarketOfferViewData(
                    x.OfferId,
                    x.Requirements.Select(r => new MarketOfferRequirementViewData(
                        r.ItemName,
                        $"{r.OwnedCount:N0}/{r.RequiredCount:N0}",
                        r.MissingCount == 0 ? string.Empty : $"Missing {r.MissingCount:N0}")).ToArray(),
                    $"{x.RewardGold:N0} Gold",
                    x.CanFulfill ? "Ready" : "Not enough items",
                    x.CanFulfill))
                .ToArray());
        }

        static string FormatUpgradePreview(FacilityUpgradePreviewSummary preview)
        {
            if (!preview.HasNextUpgrade)
            {
                return preview.UnavailableReason;
            }

            var costs = string.Join(
                ", ",
                preview.Costs.Select(x => $"{x.ItemName} {x.OwnedCount:N0}/{x.RequiredCount:N0}"));
            var state = preview.CanUpgrade ? "Ready" : preview.UnavailableReason;
            return $"Upgrade Lv.{preview.FromLevel}->{preview.ToLevel} Q{preview.NextQuality} Cap {preview.NextCapacity} [{state}] {costs}";
        }
    }
}
