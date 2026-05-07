using System;
using System.Collections.Generic;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using R3;

namespace DungeonInn.Application.Combat
{
    public sealed class AdventurerBattleRecordService : IDisposable
    {
        readonly Dictionary<Guid, AdventurerBattleRecord> records = new();
        DisposableBag bag;

        public AdventurerBattleRecordService(IGameEventBus eventBus)
        {
            if (eventBus == null)
            {
                throw new ArgumentNullException(nameof(eventBus));
            }

            eventBus.OnEvent<CombatEncounterStarted>()
                .Subscribe(OnEncounterStarted)
                .AddTo(ref bag);

            eventBus.OnEvent<CombatAttackOccurred>()
                .Subscribe(OnAttackOccurred)
                .AddTo(ref bag);
        }

        public IReadOnlyDictionary<Guid, AdventurerBattleRecord> Records => records;

        public bool TryGetRecord(Guid actorId, out AdventurerBattleRecord record)
        {
            return records.TryGetValue(actorId, out record);
        }

        AdventurerBattleRecord GetOrCreate(Guid actorId)
        {
            if (!records.TryGetValue(actorId, out var record))
            {
                record = new AdventurerBattleRecord(actorId);
                records[actorId] = record;
            }

            return record;
        }

        void OnEncounterStarted(CombatEncounterStarted e)
        {
            GetOrCreate(e.ActorId).RecordCombatStarted();
        }

        void OnAttackOccurred(CombatAttackOccurred e)
        {
            GetOrCreate(e.AttackerActorId).RecordDamageDealt(e.Damage);
            GetOrCreate(e.TargetActorId).RecordDamageTaken(e.Damage);
            if (e.TargetRemainingHp <= 0)
            {
                GetOrCreate(e.AttackerActorId).RecordKill();
            }
        }

        public void Dispose()
        {
            bag.Dispose();
        }
    }
}
