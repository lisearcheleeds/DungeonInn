#if DEBUG
using System;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.World;
using R3;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World.Debug
{
    /// <summary>
    /// Debug diagnostics only. Do not use for runtime UI.
    /// This class may read broad world state only to enrich Debug.Log output.
    /// </summary>
    public sealed class WorldDebugGameLogPresenter : IInitializable, IDisposable
    {
        readonly IEventSubscriber eventSubscriber;
        readonly IGameWorldStateReader worldState;
        readonly AdventurerBattleRecordService battleRecordService;
        readonly IActorProfileRegistry profileRegistry;
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;
        DisposableBag bag;

        [Inject]
        public WorldDebugGameLogPresenter(
            IEventSubscriber eventSubscriber,
            IGameWorldStateReader worldState,
            AdventurerBattleRecordService battleRecordService,
            IActorProfileRegistry profileRegistry,
            IWorldGameSettingsRepository worldGameSettingsRepository)
        {
            this.eventSubscriber = eventSubscriber ?? throw new ArgumentNullException(nameof(eventSubscriber));
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.battleRecordService = battleRecordService ?? throw new ArgumentNullException(nameof(battleRecordService));
            this.profileRegistry = profileRegistry ?? throw new ArgumentNullException(nameof(profileRegistry));
            this.worldGameSettingsRepository =
                worldGameSettingsRepository ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
        }

        public void Initialize()
        {
            if (!UnityEngine.Debug.isDebugBuild)
            {
                return;
            }

            eventSubscriber.OnEvent<ActorSpawned>()
                .Subscribe(OnActorSpawned)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorEnteredDungeon>()
                .Subscribe(OnActorEnteredDungeon)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorExitedDungeon>()
                .Subscribe(OnActorExitedDungeon)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorStartedReturning>()
                .Subscribe(OnActorStartedReturning)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorGoalCompleted>()
                .Subscribe(OnActorGoalCompleted)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorRecoveringAtInn>()
                .Subscribe(OnActorRecoveringAtInn)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorWaitingForInn>()
                .Subscribe(OnActorWaitingForInn)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorReservedInn>()
                .Subscribe(OnActorReservedInn)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorDeparted>()
                .Subscribe(OnActorDeparted)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorFullyRecovered>()
                .Subscribe(OnActorFullyRecovered)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<CombatEncounterStarted>()
                .Subscribe(OnEncounterStarted)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<CombatAttackOccurred>()
                .Subscribe(OnAttackOccurred)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ProjectileFired>()
                .Subscribe(OnProjectileFired)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ProjectileHit>()
                .Subscribe(OnProjectileHit)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<AreaEffectCreated>()
                .Subscribe(OnAreaEffectCreated)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<AreaEffectHit>()
                .Subscribe(OnAreaEffectHit)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorDefeated>()
                .Subscribe(OnActorDefeated)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<CombatEncounterEnded>()
                .Subscribe(OnEncounterEnded)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<InnFeeCharged>()
                .Subscribe(OnInnFeeCharged)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ExperienceGranted>()
                .Subscribe(OnExperienceGranted)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorLeveledUp>()
                .Subscribe(OnActorLeveledUp)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ItemDropped>()
                .Subscribe(OnItemDropped)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ItemPickedUp>()
                .Subscribe(OnItemPickedUp)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<EquipmentChanged>()
                .Subscribe(OnEquipmentChanged)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ItemSold>()
                .Subscribe(OnItemSold)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<InnSatisfactionChanged>()
                .Subscribe(OnInnSatisfactionChanged)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<GuildSupplyReplenished>()
                .Subscribe(OnGuildSupplyReplenished)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<DailyInnReportGenerated>()
                .Subscribe(OnDailyInnReportGenerated)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<ActorAiDecisionRecorded>()
                .Subscribe(OnActorAiDecisionRecorded)
                .AddTo(ref bag);
        }

        public void Dispose()
        {
            bag.Dispose();
        }

        void OnActorSpawned(ActorSpawned gameEvent)
        {
            UnityEngine.Debug.Log($"[Event] {GetName(gameEvent.ActorId)} が現れた");
        }

        void OnActorEnteredDungeon(ActorEnteredDungeon gameEvent)
        {
            UnityEngine.Debug.Log($"[Event] {GetName(gameEvent.ActorId)} がダンジョン {gameEvent.FloorIndex} 階に入った");
        }

        void OnActorExitedDungeon(ActorExitedDungeon gameEvent)
        {
            UnityEngine.Debug.Log($"[Event] {GetName(gameEvent.ActorId)} がダンジョンから帰還した");
        }

        void OnActorStartedReturning(ActorStartedReturning gameEvent)
        {
            UnityEngine.Debug.Log($"[Actor] {GetName(gameEvent.ActorId)} は帰還を始めた");
        }

        void OnActorGoalCompleted(ActorGoalCompleted gameEvent)
        {
            UnityEngine.Debug.Log(
                $"[Goal] {GetName(gameEvent.ActorId)} は目標を達成した: " +
                $"{gameEvent.GoalType} {gameEvent.ProgressCount}/{gameEvent.TargetCount}");
        }

        void OnActorRecoveringAtInn(ActorRecoveringAtInn gameEvent)
        {
            // Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} は回復中 {gameEvent.CurrentHp}/{gameEvent.MaxHp}");
        }

        void OnActorWaitingForInn(ActorWaitingForInn gameEvent)
        {
            var facility = worldState.Guild.GetFacility(gameEvent.InnFacilityId);
            var activeReservations = worldState.Guild.CountActiveInnReservations(gameEvent.InnFacilityId);
            if (facility.Capacity <= activeReservations)
            {
                UnityEngine.Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} waiting for inn vacancy (all {facility.Capacity} rooms occupied)");
                return;
            }

            var actor = worldState.FindActor(gameEvent.ActorId);
            var currentGold = actor == null ? 0 : actor.Inventory.Gold;
            UnityEngine.Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} waiting for inn fee ({currentGold}/{worldGameSettingsRepository.GetInnBalanceSettings().FeePerStay}G)");
        }

        void OnActorReservedInn(ActorReservedInn gameEvent)
        {
            var facility = worldState.Guild.GetFacility(gameEvent.InnFacilityId);
            var activeReservations = worldState.Guild.CountActiveInnReservations(gameEvent.InnFacilityId);
            UnityEngine.Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} reserved inn room ({activeReservations}/{facility.Capacity})");
        }

        void OnActorDeparted(ActorDeparted gameEvent)
        {
            UnityEngine.Debug.Log($"[Guild] {GetName(gameEvent.ActorId)} departed after waiting {gameEvent.WaitedDays} days for inn");
        }

        void OnActorFullyRecovered(ActorFullyRecovered gameEvent)
        {
            UnityEngine.Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} は全回復した");
        }

        void OnEncounterStarted(CombatEncounterStarted gameEvent)
        {
            UnityEngine.Debug.Log($"[Combat] {GetName(gameEvent.ActorId)} は {GetName(gameEvent.TargetActorId)} と遭遇した");
        }

        void OnAttackOccurred(CombatAttackOccurred gameEvent)
        {
            UnityEngine.Debug.Log(
                $"[Combat] {GetName(gameEvent.AttackerActorId)} は {GetName(gameEvent.TargetActorId)} に攻撃！ " +
                $"ダメージ: {gameEvent.Damage} (HP {gameEvent.TargetRemainingHp})");
        }

        void OnProjectileFired(ProjectileFired gameEvent)
        {
            UnityEngine.Debug.Log($"[Combat] {GetName(gameEvent.AttackerActorId)} fired projectile at {GetName(gameEvent.TargetActorId)}");
        }

        void OnProjectileHit(ProjectileHit gameEvent)
        {
            UnityEngine.Debug.Log($"[Combat] Projectile hit {GetName(gameEvent.TargetActorId)} for {gameEvent.Damage}");
        }

        void OnAreaEffectCreated(AreaEffectCreated gameEvent)
        {
            UnityEngine.Debug.Log(
                $"[Combat] {GetName(gameEvent.AttackerActorId)} created area effect at " +
                $"({gameEvent.CenterPosition.X:0.0}, {gameEvent.CenterPosition.Z:0.0}) radius {gameEvent.RadiusMeters:0.0}m");
        }

        void OnAreaEffectHit(AreaEffectHit gameEvent)
        {
            UnityEngine.Debug.Log($"[Combat] Area effect hit {GetName(gameEvent.TargetActorId)} for {gameEvent.Damage}");
        }

        void OnActorDefeated(ActorDefeated gameEvent)
        {
            var killerText = gameEvent.KillerActorId.HasValue
                ? $" by {GetName(gameEvent.KillerActorId.Value)}"
                : string.Empty;
            UnityEngine.Debug.Log($"[Combat] {GetName(gameEvent.ActorId)} は {killerText} に倒された ({gameEvent.Cause})");
        }

        void OnEncounterEnded(CombatEncounterEnded gameEvent)
        {
            if (!battleRecordService.TryGetRecord(gameEvent.ActorId, out var record))
            {
                return;
            }

            UnityEngine.Debug.Log(
                $"[Record] {GetName(gameEvent.ActorId)}: は戦闘を終了した" +
                $"{record.TotalCombats} combats, " +
                $"{record.TotalDamageDealt} total damage dealt");
        }

        void OnExperienceGranted(ExperienceGranted gameEvent)
        {
            UnityEngine.Debug.Log($"[Growth] {GetName(gameEvent.ActorId)} は {gameEvent.GainedXp} EXP を得た (total: {gameEvent.TotalXp})");
        }

        void OnActorLeveledUp(ActorLeveledUp gameEvent)
        {
            UnityEngine.Debug.Log($"[Growth] {GetName(gameEvent.ActorId)} LEVEL UP! Lv.{gameEvent.PreviousLevel} → Lv.{gameEvent.NewLevel}");
        }

        void OnEquipmentChanged(EquipmentChanged gameEvent)
        {
            if (gameEvent.PreviousItemId.HasValue)
            {
                UnityEngine.Debug.Log($"[Equip] {GetName(gameEvent.ActorId)} はアイテムを装備した item#{gameEvent.NewItemId} at {gameEvent.Slot} (入れ替え item#{gameEvent.PreviousItemId})");
            }
            else
            {
                UnityEngine.Debug.Log($"[Equip] {GetName(gameEvent.ActorId)} はアイテムを装備した item#{gameEvent.NewItemId} at {gameEvent.Slot}");
            }
        }

        void OnItemSold(ItemSold gameEvent)
        {
            UnityEngine.Debug.Log($"[Shop] {GetName(gameEvent.ActorId)} はアイテムを売却した item#{gameEvent.Stack.ItemId} x{gameEvent.Stack.Count} for {gameEvent.TotalPrice}G (wallet: {gameEvent.ActorGold}G)");
        }

        void OnItemDropped(ItemDropped gameEvent)
        {
            UnityEngine.Debug.Log($"[Drop] {GetName(gameEvent.ActorId)} はアイテムを落とした item#{gameEvent.ItemInstance.Stack.ItemId} x{gameEvent.ItemInstance.Stack.Count}");
        }

        void OnItemPickedUp(ItemPickedUp gameEvent)
        {
            if (gameEvent.ItemInstance.Stack.ItemId == Domain.Item.SpecialItemIds.Money)
            {
                var actor = worldState.FindActor(gameEvent.ActorId);
                var walletText = actor == null ? "unknown" : $"{actor.Inventory.Gold}G";
                UnityEngine.Debug.Log($"[Item] {GetName(gameEvent.ActorId)} は {gameEvent.ItemInstance.Stack.Count}G を拾った (wallet: {walletText})");
                return;
            }

            UnityEngine.Debug.Log($"[Item] {GetName(gameEvent.ActorId)} は item#{gameEvent.ItemInstance.Stack.ItemId} x{gameEvent.ItemInstance.Stack.Count} を拾った");
        }

        void OnInnFeeCharged(InnFeeCharged gameEvent)
        {
            UnityEngine.Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} paid {gameEvent.FeeAmount}G for inn room (remaining: {gameEvent.ActorRemainingGold}G)");
            UnityEngine.Debug.Log($"[Guild] Treasury +{gameEvent.FeeAmount}G (total: {gameEvent.GuildGold}G)");
        }

        void OnInnSatisfactionChanged(InnSatisfactionChanged gameEvent)
        {
            UnityEngine.Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} satisfaction changed {gameEvent.Delta:+#;-#;0} ({gameEvent.Reason})");
        }

        void OnGuildSupplyReplenished(GuildSupplyReplenished gameEvent)
        {
            UnityEngine.Debug.Log(
                $"[Guild] Replenished item#{gameEvent.ItemId} x{gameEvent.Count} " +
                $"for {gameEvent.Cost}G (treasury: {gameEvent.RemainingGold}G)");
        }

        void OnDailyInnReportGenerated(DailyInnReportGenerated gameEvent)
        {
            var report = gameEvent.Report;
            UnityEngine.Debug.Log(
                $"[Daily] Day={report.Day} Guests={report.Guests} " +
                $"Demand={report.Demand} Rejected={report.RejectedGuests} " +
                $"Occupancy={report.OccupiedRooms}/{report.RoomCapacity} ({report.OccupancyPercent}%) " +
                $"Sales={report.Sales}G Satisfaction={report.SatisfactionDelta:+#;-#;0} " +
                $"Reputation={report.Reputation} Treasury={report.GuildGold}G " +
                $"Stock(Sword={report.RookieSwordStock}, Armor={report.RookieArmorStock})");
        }

        void OnActorAiDecisionRecorded(ActorAiDecisionRecorded gameEvent)
        {
            UnityEngine.Debug.Log($"[AI] {GetName(gameEvent.ActorId)} selected {gameEvent.DecisionType}: {FormatAiReason(gameEvent)}");
        }

        static string FormatAiReason(ActorAiDecisionRecorded gameEvent)
        {
            switch (gameEvent.ReasonType)
            {
                case AiDecisionReasonType.GoalCompleted:
                    return $"goal completed (score {gameEvent.Score})";
                case AiDecisionReasonType.CriticalHp:
                    return $"critical HP {gameEvent.CurrentHp}/{gameEvent.MaxHp}";
                case AiDecisionReasonType.LowHpWithoutRecoveryItem:
                    return $"low HP {gameEvent.CurrentHp}/{gameEvent.MaxHp} without recovery item";
                case AiDecisionReasonType.NoVacantInnRoom:
                    return "no vacant room";
                case AiDecisionReasonType.LowHpWithRecoveryItem:
                    return $"low HP {gameEvent.CurrentHp}/{gameEvent.MaxHp} with recovery item";
                case AiDecisionReasonType.NearestHostileInRange:
                    return $"nearest hostile {gameEvent.TargetActorId:N}";
                case AiDecisionReasonType.CombatPowerMatchesFloor:
                    return $"combat power matched floor {gameEvent.SelectedFloor}";
                case AiDecisionReasonType.FallbackToLowestFloor:
                    return $"fallback to floor {gameEvent.SelectedFloor}";
                case AiDecisionReasonType.FacilityUsageRequest:
                    return $"facility request {gameEvent.FacilityId:N}";
                default:
                    return gameEvent.ReasonType.ToString();
            }
        }

        string GetName(Guid actorId)
        {
            return profileRegistry.TryGetProfile(actorId, out var profile)
                ? profile.DisplayName
                : actorId.ToString("N")[..8];
        }
    }
}
#endif
