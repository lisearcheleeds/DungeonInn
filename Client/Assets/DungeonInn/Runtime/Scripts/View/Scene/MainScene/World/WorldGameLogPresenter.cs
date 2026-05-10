using System;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Profiles;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldGameLogPresenter : IInitializable, IDisposable
    {
        readonly IGameEventBus eventBus;
        readonly IGameWorldState worldState;
        readonly AdventurerBattleRecordService battleRecordService;
        readonly IActorProfileRegistry profileRegistry;
        DisposableBag bag;

        [Inject]
        public WorldGameLogPresenter(
            IGameEventBus eventBus,
            IGameWorldState worldState,
            AdventurerBattleRecordService battleRecordService,
            IActorProfileRegistry profileRegistry)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.battleRecordService = battleRecordService ?? throw new ArgumentNullException(nameof(battleRecordService));
            this.profileRegistry = profileRegistry ?? throw new ArgumentNullException(nameof(profileRegistry));
        }

        public void Initialize()
        {
            eventBus.OnEvent<ActorSpawned>()
                .Subscribe(OnActorSpawned)
                .AddTo(ref bag);

            eventBus.OnEvent<ActorEnteredDungeon>()
                .Subscribe(OnActorEnteredDungeon)
                .AddTo(ref bag);

            eventBus.OnEvent<ActorExitedDungeon>()
                .Subscribe(OnActorExitedDungeon)
                .AddTo(ref bag);

            eventBus.OnEvent<ActorStartedReturning>()
                .Subscribe(OnActorStartedReturning)
                .AddTo(ref bag);

            eventBus.OnEvent<ActorGoalCompleted>()
                .Subscribe(OnActorGoalCompleted)
                .AddTo(ref bag);

            eventBus.OnEvent<ActorRecoveringAtInn>()
                .Subscribe(OnActorRecoveringAtInn)
                .AddTo(ref bag);

            eventBus.OnEvent<ActorWaitingForInn>()
                .Subscribe(OnActorWaitingForInn)
                .AddTo(ref bag);

            eventBus.OnEvent<ActorReservedInn>()
                .Subscribe(OnActorReservedInn)
                .AddTo(ref bag);

            eventBus.OnEvent<ActorFullyRecovered>()
                .Subscribe(OnActorFullyRecovered)
                .AddTo(ref bag);

            eventBus.OnEvent<CombatEncounterStarted>()
                .Subscribe(OnEncounterStarted)
                .AddTo(ref bag);

            eventBus.OnEvent<CombatAttackOccurred>()
                .Subscribe(OnAttackOccurred)
                .AddTo(ref bag);

            eventBus.OnEvent<ActorDefeated>()
                .Subscribe(OnActorDefeated)
                .AddTo(ref bag);

            eventBus.OnEvent<CombatEncounterEnded>()
                .Subscribe(OnEncounterEnded)
                .AddTo(ref bag);

            eventBus.OnEvent<InnFeeCharged>()
                .Subscribe(OnInnFeeCharged)
                .AddTo(ref bag);

            eventBus.OnEvent<ExperienceGranted>()
                .Subscribe(OnExperienceGranted)
                .AddTo(ref bag);

            eventBus.OnEvent<ActorLeveledUp>()
                .Subscribe(OnActorLeveledUp)
                .AddTo(ref bag);

            eventBus.OnEvent<ItemDropped>()
                .Subscribe(OnItemDropped)
                .AddTo(ref bag);

            eventBus.OnEvent<ItemPickedUp>()
                .Subscribe(OnItemPickedUp)
                .AddTo(ref bag);

            eventBus.OnEvent<EquipmentChanged>()
                .Subscribe(OnEquipmentChanged)
                .AddTo(ref bag);

            eventBus.OnEvent<ItemSold>()
                .Subscribe(OnItemSold)
                .AddTo(ref bag);
        }

        public void Dispose()
        {
            bag.Dispose();
        }

        void OnActorSpawned(ActorSpawned gameEvent)
        {
            Debug.Log($"[Event] {GetName(gameEvent.ActorId)} が現れた");
        }

        void OnActorEnteredDungeon(ActorEnteredDungeon gameEvent)
        {
            Debug.Log($"[Event] {GetName(gameEvent.ActorId)} がダンジョン {gameEvent.FloorIndex} 階に入った");
        }

        void OnActorExitedDungeon(ActorExitedDungeon gameEvent)
        {
            Debug.Log($"[Event] {GetName(gameEvent.ActorId)} がダンジョンから帰還した");
        }

        void OnActorStartedReturning(ActorStartedReturning gameEvent)
        {
            Debug.Log($"[Actor] {GetName(gameEvent.ActorId)} は帰還を始めた");
        }

        void OnActorGoalCompleted(ActorGoalCompleted gameEvent)
        {
            Debug.Log(
                $"[Goal] {GetName(gameEvent.ActorId)} は目標を達成した: " +
                $"{gameEvent.GoalType} {gameEvent.ProgressCount}/{gameEvent.TargetCount}");
        }

        void OnActorRecoveringAtInn(ActorRecoveringAtInn gameEvent)
        {
            Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} は回復中 {gameEvent.CurrentHp}/{gameEvent.MaxHp}");
        }

        void OnActorWaitingForInn(ActorWaitingForInn gameEvent)
        {
            var facility = worldState.Guild.GetFacility(gameEvent.InnFacilityId);
            var activeReservations = worldState.Guild.CountActiveInnReservations(gameEvent.InnFacilityId);
            if (facility.Capacity <= activeReservations)
            {
                Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} waiting for inn vacancy (all {facility.Capacity} rooms occupied)");
                return;
            }

            var actor = worldState.FindActor(gameEvent.ActorId);
            var currentGold = actor == null ? 0 : actor.Inventory.Gold;
            Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} waiting for inn fee ({currentGold}/{Domain.Common.GameConstants.InnFeePerStay}G)");
        }

        void OnActorReservedInn(ActorReservedInn gameEvent)
        {
            var facility = worldState.Guild.GetFacility(gameEvent.InnFacilityId);
            var activeReservations = worldState.Guild.CountActiveInnReservations(gameEvent.InnFacilityId);
            Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} reserved inn room ({activeReservations}/{facility.Capacity})");
        }

        void OnActorFullyRecovered(ActorFullyRecovered gameEvent)
        {
            Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} は全回復した");
        }

        void OnEncounterStarted(CombatEncounterStarted gameEvent)
        {
            Debug.Log($"[Combat] {GetName(gameEvent.ActorId)} は {GetName(gameEvent.TargetActorId)} と遭遇した");
        }

        void OnAttackOccurred(CombatAttackOccurred gameEvent)
        {
            Debug.Log(
                $"[Combat] {GetName(gameEvent.AttackerActorId)} は {GetName(gameEvent.TargetActorId)} に攻撃！ " +
                $"ダメージ: {gameEvent.Damage} (HP {gameEvent.TargetRemainingHp})");
        }

        void OnActorDefeated(ActorDefeated gameEvent)
        {
            var killerText = gameEvent.KillerActorId.HasValue
                ? $" by {GetName(gameEvent.KillerActorId.Value)}"
                : string.Empty;
            Debug.Log($"[Combat] {GetName(gameEvent.ActorId)} は {killerText} に倒された ({gameEvent.Cause})");
        }

        void OnEncounterEnded(CombatEncounterEnded gameEvent)
        {
            if (!battleRecordService.TryGetRecord(gameEvent.ActorId, out var record))
            {
                return;
            }

            Debug.Log(
                $"[Record] {GetName(gameEvent.ActorId)}: は戦闘を終了した" +
                $"{record.TotalCombats} combats, " +
                $"{record.TotalDamageDealt} total damage dealt");
        }

        void OnExperienceGranted(ExperienceGranted gameEvent)
        {
            Debug.Log($"[Growth] {GetName(gameEvent.ActorId)} は {gameEvent.GainedXp} EXP を得た (total: {gameEvent.TotalXp})");
        }

        void OnActorLeveledUp(ActorLeveledUp gameEvent)
        {
            Debug.Log($"[Growth] {GetName(gameEvent.ActorId)} LEVEL UP! Lv.{gameEvent.PreviousLevel} → Lv.{gameEvent.NewLevel}");
        }

        void OnEquipmentChanged(EquipmentChanged gameEvent)
        {
            if (gameEvent.PreviousItemId.HasValue)
            {
                Debug.Log($"[Equip] {GetName(gameEvent.ActorId)} はアイテムを装備した item#{gameEvent.NewItemId} at {gameEvent.Slot} (入れ替え item#{gameEvent.PreviousItemId})");
            }
            else
            {
                Debug.Log($"[Equip] {GetName(gameEvent.ActorId)} はアイテムを装備した item#{gameEvent.NewItemId} at {gameEvent.Slot}");
            }
        }

        void OnItemSold(ItemSold gameEvent)
        {
            Debug.Log($"[Shop] {GetName(gameEvent.ActorId)} はアイテムを売却した item#{gameEvent.Stack.ItemId} x{gameEvent.Stack.Count} for {gameEvent.TotalPrice}G (wallet: {gameEvent.ActorGold}G)");
        }

        void OnItemDropped(ItemDropped gameEvent)
        {
            Debug.Log($"[Drop] {GetName(gameEvent.ActorId)} はアイテムを落とした item#{gameEvent.ItemInstance.Stack.ItemId} x{gameEvent.ItemInstance.Stack.Count}");
        }

        void OnItemPickedUp(ItemPickedUp gameEvent)
        {
            if (gameEvent.ItemInstance.Stack.ItemId == Domain.Item.SpecialItemIds.Money)
            {
                var actor = worldState.FindActor(gameEvent.ActorId);
                var walletText = actor == null ? "unknown" : $"{actor.Inventory.Gold}G";
                Debug.Log($"[Item] {GetName(gameEvent.ActorId)} は {gameEvent.ItemInstance.Stack.Count}G を拾った (wallet: {walletText})");
                return;
            }

            Debug.Log($"[Item] {GetName(gameEvent.ActorId)} は item#{gameEvent.ItemInstance.Stack.ItemId} x{gameEvent.ItemInstance.Stack.Count} を拾った");
        }

        void OnInnFeeCharged(InnFeeCharged gameEvent)
        {
            Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} paid {gameEvent.FeeAmount}G for inn room (remaining: {gameEvent.ActorRemainingGold}G)");
            Debug.Log($"[Guild] Treasury +{gameEvent.FeeAmount}G (total: {gameEvent.GuildGold}G)");
        }

        string GetName(Guid actorId)
        {
            return profileRegistry.TryGetProfile(actorId, out var profile)
                ? profile.DisplayName
                : actorId.ToString("N")[..8];
        }
    }
}
