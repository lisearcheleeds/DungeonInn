using System;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
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
        readonly AdventurerBattleRecordService battleRecordService;
        readonly IActorProfileRegistry profileRegistry;
        DisposableBag bag;

        [Inject]
        public WorldGameLogPresenter(
            IGameEventBus eventBus,
            AdventurerBattleRecordService battleRecordService,
            IActorProfileRegistry profileRegistry)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
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
        }

        public void Dispose()
        {
            bag.Dispose();
        }

        void OnActorSpawned(ActorSpawned e)
        {
            Debug.Log($"[Event] {GetName(e.ActorId)} が現れた");
        }

        void OnActorEnteredDungeon(ActorEnteredDungeon e)
        {
            Debug.Log($"[Event] {GetName(e.ActorId)} がダンジョン {e.FloorIndex} 階に入った");
        }

        void OnActorExitedDungeon(ActorExitedDungeon e)
        {
            Debug.Log($"[Event] {GetName(e.ActorId)} がダンジョンから帰還した");
        }

        void OnActorStartedReturning(ActorStartedReturning e)
        {
            Debug.Log($"[Actor] {GetName(e.ActorId)} starts returning");
        }

        void OnActorRecoveringAtInn(ActorRecoveringAtInn e)
        {
            Debug.Log($"[Inn] {GetName(e.ActorId)} recovering HP {e.CurrentHp}/{e.MaxHp}");
        }

        void OnActorFullyRecovered(ActorFullyRecovered e)
        {
            Debug.Log($"[Inn] {GetName(e.ActorId)} fully recovered");
        }

        void OnEncounterStarted(CombatEncounterStarted e)
        {
            Debug.Log($"[Combat] {GetName(e.ActorId)} encountered {GetName(e.TargetActorId)}");
        }

        void OnAttackOccurred(CombatAttackOccurred e)
        {
            Debug.Log(
                $"[Combat] {GetName(e.AttackerActorId)} attacked {GetName(e.TargetActorId)} " +
                $"for {e.Damage} (HP {e.TargetRemainingHp})");
        }

        void OnActorDefeated(ActorDefeated e)
        {
            var killerText = e.KillerActorId.HasValue
                ? $" by {GetName(e.KillerActorId.Value)}"
                : string.Empty;
            Debug.Log($"[Combat] {GetName(e.ActorId)} defeated{killerText} ({e.Cause})");
        }

        void OnEncounterEnded(CombatEncounterEnded e)
        {
            if (!battleRecordService.TryGetRecord(e.ActorId, out var record))
            {
                return;
            }

            Debug.Log(
                $"[Record] {GetName(e.ActorId)}: " +
                $"{record.TotalCombats} combats, " +
                $"{record.TotalDamageDealt} total damage dealt");
        }

        void OnExperienceGranted(ExperienceGranted e)
        {
            Debug.Log($"[Growth] {GetName(e.ActorId)} gained {e.GainedXp} EXP (total: {e.TotalXp})");
        }

        void OnActorLeveledUp(ActorLeveledUp e)
        {
            Debug.Log($"[Growth] {GetName(e.ActorId)} leveled up! Lv.{e.PreviousLevel} → Lv.{e.NewLevel}");
        }

        void OnItemDropped(ItemDropped e)
        {
            Debug.Log($"[Drop] {GetName(e.ActorId)} dropped {e.ItemName} at {e.Position}");
        }

        void OnInnFeeCharged(InnFeeCharged e)
        {
            Debug.Log($"[Inn] {GetName(e.ActorId)} paid {e.FeeAmount}G for inn room (remaining: {e.ActorRemainingGold}G)");
            Debug.Log($"[Guild] Treasury +{e.FeeAmount}G (total: {e.GuildGold}G)");
        }

        string GetName(Guid actorId)
        {
            return profileRegistry.TryGetProfile(actorId, out var profile)
                ? profile.DisplayName
                : actorId.ToString("N")[..8];
        }
    }
}
