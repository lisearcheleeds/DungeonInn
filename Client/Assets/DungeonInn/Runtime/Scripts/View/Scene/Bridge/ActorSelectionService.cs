using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using R3;
using VContainer;

namespace DungeonInn.View.Scene.Bridge
{
    public sealed class ActorSelectionService : IActorSelectionReader, IDisposable
    {
        readonly IActorSelectionCandidateProvider candidateProvider;
        readonly ReactiveProperty<Guid?> selectedActorId = new(null);
        readonly List<Guid> sortedActorIdBuffer = new();

        public ReactiveProperty<Guid?> SelectedActorId => selectedActorId;
        ReadOnlyReactiveProperty<Guid?> IActorSelectionReader.SelectedActorId => selectedActorId;

        [Inject]
        public ActorSelectionService(IActorSelectionCandidateProvider candidateProvider)
        {
            this.candidateProvider = candidateProvider ?? throw new ArgumentNullException(nameof(candidateProvider));
        }

        public void Select(Guid actorId)
        {
            selectedActorId.Value = actorId;
        }

        public void Deselect()
        {
            selectedActorId.Value = null;
        }

        public void SelectNext()
        {
            if (!selectedActorId.Value.HasValue)
            {
                return;
            }

            RefreshSortedBuffer();
            if (sortedActorIdBuffer.Count == 0)
            {
                return;
            }

            var currentIndex = sortedActorIdBuffer.IndexOf(selectedActorId.Value.Value);
            if (currentIndex < 0)
            {
                selectedActorId.Value = sortedActorIdBuffer[0];
                return;
            }

            var nextIndex = currentIndex + 1;
            if (sortedActorIdBuffer.Count <= nextIndex)
            {
                nextIndex = 0;
            }

            selectedActorId.Value = sortedActorIdBuffer[nextIndex];
        }

        public void SelectPrevious()
        {
            if (!selectedActorId.Value.HasValue)
            {
                return;
            }

            RefreshSortedBuffer();
            if (sortedActorIdBuffer.Count == 0)
            {
                return;
            }

            var currentIndex = sortedActorIdBuffer.IndexOf(selectedActorId.Value.Value);
            if (currentIndex < 0)
            {
                selectedActorId.Value = sortedActorIdBuffer[sortedActorIdBuffer.Count - 1];
                return;
            }

            var prevIndex = currentIndex - 1;
            if (prevIndex < 0)
            {
                prevIndex = sortedActorIdBuffer.Count - 1;
            }

            selectedActorId.Value = sortedActorIdBuffer[prevIndex];
        }

        public void Dispose()
        {
            selectedActorId.Dispose();
        }

        void RefreshSortedBuffer()
        {
            candidateProvider.CopyActorIdsTo(sortedActorIdBuffer);
            sortedActorIdBuffer.Sort();
        }
    }
}
