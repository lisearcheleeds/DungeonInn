using System;
using R3;

namespace DungeonInn.View.Scene.Bridge
{
    public interface IActorSelectionReader
    {
        ReadOnlyReactiveProperty<Guid?> SelectedActorId { get; }
    }
}
