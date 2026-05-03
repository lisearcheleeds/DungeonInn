using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Inn;
using DungeonInn.Domain.World;

namespace DungeonInn.Application.UseCase
{
    public class PurchaseInnParcelUseCase
    {
        public UniTask<Room> ExecuteAsync(InnLand land, GridPosition origin, Money cost)
        {
            if (!land.CanPurchaseAt(origin, cost))
            {
                throw new InvalidOperationException();
            }

            var room = land.PurchaseParcel(origin, cost);
            return UniTask.FromResult(room);
        }
    }
}
