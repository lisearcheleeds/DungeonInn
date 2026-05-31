using DungeonInn.Application.GameLoop;
using System;
using System.Collections.Generic;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using R3;

namespace DungeonInn.Application.Economy
{
    public sealed class InnEconomyStatisticsService : IDisposable
    {
        readonly Dictionary<int, MutableStatistics> statisticsByDay = new();
        readonly CompositeDisposable disposables = new();
        readonly IGameClock gameClock;

        public InnEconomyStatisticsService(
            IEventSubscriber eventSubscriber,
            IGameClock gameClock)
        {
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            if (eventSubscriber == null)
            {
                throw new ArgumentNullException(nameof(eventSubscriber));
            }

            eventSubscriber.OnEvent<InnFeeCharged>()
                .Subscribe(OnInnFeeCharged)
                .AddTo(disposables);
            eventSubscriber.OnEvent<InnSatisfactionChanged>()
                .Subscribe(OnInnSatisfactionChanged)
                .AddTo(disposables);
        }

        public InnEconomyStatistics GetByDay(int day)
        {
            return GetMutableStatistics(day).ToStatistics();
        }

        public void Dispose()
        {
            disposables.Dispose();
        }

        void OnInnFeeCharged(InnFeeCharged gameEvent)
        {
            var statistics = GetMutableStatistics(gameClock.CurrentDay);
            statistics.Guests++;
            statistics.Sales += gameEvent.FeeAmount;
        }

        void OnInnSatisfactionChanged(InnSatisfactionChanged gameEvent)
        {
            var statistics = GetMutableStatistics(gameClock.CurrentDay);
            statistics.SatisfactionDelta += gameEvent.Delta;
            if (gameEvent.Reason == InnSatisfactionChangeReason.WaitingForInn)
            {
                statistics.RejectedGuests++;
            }
        }

        MutableStatistics GetMutableStatistics(int day)
        {
            if (!statisticsByDay.TryGetValue(day, out var statistics))
            {
                statistics = new MutableStatistics();
                statisticsByDay[day] = statistics;
            }

            return statistics;
        }

        sealed class MutableStatistics
        {
            public int Guests { get; set; }
            public int RejectedGuests { get; set; }
            public int Sales { get; set; }
            public int SatisfactionDelta { get; set; }

            public InnEconomyStatistics ToStatistics()
            {
                return new InnEconomyStatistics(
                    Guests,
                    RejectedGuests,
                    Sales,
                    SatisfactionDelta);
            }
        }
    }
}
