using System;
using DungeonInn.Application.Profiles;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldCombatLogPresenter : IInitializable, IDisposable
    {
        readonly IGameEventBus eventBus;
        readonly AdventurerBattleRecordService battleRecordService;
        readonly IActorProfileRegistry profileRegistry;
        DisposableBag bag;

        [Inject]
        public WorldCombatLogPresenter(
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
