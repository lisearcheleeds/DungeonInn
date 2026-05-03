using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Inn;

namespace DungeonInn.Application.UseCase
{
    public class RemoveBedUseCase
    {
        public UniTask ExecuteAsync(InnLand land, Room room, Guid bedId)
        {
            room.RemoveBed(bedId);
            return UniTask.CompletedTask;
        }
    }
}
