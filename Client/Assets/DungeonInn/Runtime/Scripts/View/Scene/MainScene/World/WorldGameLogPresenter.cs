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

        string GetName(Guid actorId)
        {
            return profileRegistry.TryGetProfile(actorId, out var profile)
                ? profile.DisplayName
                : actorId.ToString("N")[..8];
        }
    }
}
