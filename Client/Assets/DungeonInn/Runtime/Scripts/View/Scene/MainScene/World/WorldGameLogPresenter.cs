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

            eventBus.OnEvent<ActorRecoveringAtInn>()
                .Subscribe(OnActorRecoveringAtInn)
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
            Debug.Log($"[Actor] {GetName(gameEvent.ActorId)} starts returning");
        }

        void OnActorRecoveringAtInn(ActorRecoveringAtInn gameEvent)
        {
            Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} recovering HP {gameEvent.CurrentHp}/{gameEvent.MaxHp}");
        }

        void OnActorFullyRecovered(ActorFullyRecovered gameEvent)
        {
            Debug.Log($"[Inn] {GetName(gameEvent.ActorId)} fully recovered");
        }

        void OnEncounterStarted(CombatEncounterStarted gameEvent)
        {
            Debug.Log($"[Combat] {GetName(gameEvent.ActorId)} encountered {GetName(gameEvent.TargetActorId)}");
        }

        void OnAttackOccurred(CombatAttackOccurred gameEvent)
        {
            Debug.Log(
                $"[Combat] {GetName(gameEvent.AttackerActorId)} attacked {GetName(gameEvent.TargetActorId)} " +
                $"for {gameEvent.Damage} (HP {gameEvent.TargetRemainingHp})");
        }

        void OnActorDefeated(ActorDefeated gameEvent)
        {
            var killerText = gameEvent.KillerActorId.HasValue
                ? $" by {GetName(gameEvent.KillerActorId.Value)}"
                : string.Empty;
            Debug.Log($"[Combat] {GetName(gameEvent.ActorId)} defeated{killerText} ({gameEvent.Cause})");
        }

        void OnEncounterEnded(CombatEncounterEnded gameEvent)
        {
            if (!battleRecordService.TryGetRecord(gameEvent.ActorId, out var record))
            {
                return;
            }

            Debug.Log(
                $"[Record] {GetName(gameEvent.ActorId)}: " +
                $"{record.TotalCombats} combats, " +
                $"{record.TotalDamageDealt} total damage dealt");
        }

        void OnExperienceGranted(ExperienceGranted gameEvent)
        {
            Debug.Log($"[Growth] {GetName(gameEvent.ActorId)} gained {gameEvent.GainedXp} EXP (total: {gameEvent.TotalXp})");
        }

        void OnActorLeveledUp(ActorLeveledUp gameEvent)
        {
            Debug.Log($"[Growth] {GetName(gameEvent.ActorId)} leveled up! Lv.{gameEvent.PreviousLevel} → Lv.{gameEvent.NewLevel}");
        }

        void OnItemDropped(ItemDropped gameEvent)
        {
            Debug.Log($"[Drop] {GetName(gameEvent.ActorId)} dropped item#{gameEvent.ItemInstance.Stack.ItemId} x{gameEvent.ItemInstance.Stack.Count}");
        }

        void OnItemPickedUp(ItemPickedUp gameEvent)
        {
            if (gameEvent.ItemInstance.Stack.ItemId == Domain.Item.SpecialItemIds.Money)
            {
                var actor = worldState.FindActor(gameEvent.ActorId);
                var walletText = actor == null ? "unknown" : $"{actor.Inventory.Gold}G";
                Debug.Log($"[Item] {GetName(gameEvent.ActorId)} picked up {gameEvent.ItemInstance.Stack.Count}G (wallet: {walletText})");
                return;
            }

            Debug.Log($"[Item] {GetName(gameEvent.ActorId)} picked up item#{gameEvent.ItemInstance.Stack.ItemId} x{gameEvent.ItemInstance.Stack.Count}");
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
