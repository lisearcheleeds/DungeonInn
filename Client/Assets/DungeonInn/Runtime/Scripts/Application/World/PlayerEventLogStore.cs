using System;
using System.Collections.Generic;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using R3;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.Application.World
{
    public sealed class PlayerEventLogStore : IInitializable, IDisposable
    {
        const int MaxEntryCount = 30;

        readonly IEventSubscriber eventSubscriber;
        readonly PlayerEventLogFormatter formatter;
        readonly IGameClock gameClock;
        readonly List<PlayerEventLogEntry> entries = new(MaxEntryCount);
        readonly Subject<PlayerEventLogEntry> onEntryAdded = new();
        DisposableBag bag;

        public Observable<PlayerEventLogEntry> OnEntryAdded => onEntryAdded;

        [Inject]
        public PlayerEventLogStore(
            IEventSubscriber eventSubscriber,
            PlayerEventLogFormatter formatter,
            IGameClock gameClock)
        {
            this.eventSubscriber = eventSubscriber ?? throw new ArgumentNullException(nameof(eventSubscriber));
            this.formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
        }

        public void Initialize()
        {
            eventSubscriber.OnEvent<CombatAttackOccurred>()
                .Subscribe(e => AddFromEvent(e, e.AttackerActorId)).AddTo(ref bag);
            eventSubscriber.OnEvent<ProjectileFired>()
                .Subscribe(e => AddFromEvent(e, e.AttackerActorId)).AddTo(ref bag);
            eventSubscriber.OnEvent<ProjectileHit>()
                .Subscribe(e => AddFromEvent(e, e.AttackerActorId)).AddTo(ref bag);
            eventSubscriber.OnEvent<AreaEffectCreated>()
                .Subscribe(e => AddFromEvent(e, e.AttackerActorId)).AddTo(ref bag);
            eventSubscriber.OnEvent<AreaEffectHit>()
                .Subscribe(e => AddFromEvent(e, e.AttackerActorId)).AddTo(ref bag);
            eventSubscriber.OnEvent<ActorDefeated>()
                .Subscribe(e => AddFromEvent(e, e.ActorId)).AddTo(ref bag);
            eventSubscriber.OnEvent<ItemDropped>()
                .Subscribe(e => AddFromEvent(e, e.ActorId)).AddTo(ref bag);
            eventSubscriber.OnEvent<ItemPickedUp>()
                .Subscribe(e => AddFromEvent(e, e.ActorId)).AddTo(ref bag);
            eventSubscriber.OnEvent<ActorLeveledUp>()
                .Subscribe(e => AddFromEvent(e, e.ActorId)).AddTo(ref bag);
            eventSubscriber.OnEvent<ActorRecoveringAtInn>()
                .Subscribe(e => AddFromEvent(e, e.ActorId)).AddTo(ref bag);
            eventSubscriber.OnEvent<ActorFullyRecovered>()
                .Subscribe(e => AddFromEvent(e, e.ActorId)).AddTo(ref bag);
        }

        public void Add(PlayerEventLogEntry entry)
        {
            if (MaxEntryCount <= entries.Count)
            {
                entries.RemoveAt(0);
            }
            entries.Add(entry);
            onEntryAdded.OnNext(entry);
        }

        public IReadOnlyList<PlayerEventLogEntry> GetRecentEntries(int count)
        {
            var start = entries.Count - count;
            if (start < 0) start = 0;
            return entries.GetRange(start, entries.Count - start);
        }

        public void Dispose()
        {
            bag.Dispose();
            onEntryAdded.Dispose();
        }

        void AddFromEvent(IGameEvent gameEvent, Guid relatedActorId)
        {
            var text = formatter.Format(gameEvent);
            if (string.IsNullOrEmpty(text)) return;
            Add(new PlayerEventLogEntry(gameClock.ElapsedRealTimeSeconds, text, relatedActorId));
        }
    }
}
