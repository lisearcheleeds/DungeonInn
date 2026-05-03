using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Inn;
using DungeonInn.Domain.World;

namespace DungeonInn.Application.UseCase
{
    public class PlaceBedUseCase
    {
        public UniTask ExecuteAsync(InnLand land, Room room, GridPosition worldPos)
        {
            if (land.UnplacedBedCount <= 0)
            {
                throw new InvalidOperationException("No unplaced beds.");
            }

            if (!room.CanPlaceBedAt(worldPos))
            {
                throw new InvalidOperationException("Cannot place bed.");
            }

            var bed = new Bed(Guid.NewGuid(), worldPos);
            room.PlaceBed(bed);
            return UniTask.CompletedTask;
        }
    }
}
