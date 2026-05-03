using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.Inn;

namespace DungeonInn.Application.UseCase
{
    public class AdventurerCheckInUseCase
    {
        public UniTask<Bed> ExecuteAsync(InnLand land, AdventurerCharacter adventurer)
        {
            foreach (var room in land.Rooms)
            {
                var bed = room.Beds.FirstOrDefault(b => !b.IsOccupied);
                if (bed != null)
                {
                    bed.CheckIn();
                    return UniTask.FromResult(bed);
                }
            }

            return UniTask.FromResult<Bed>(null);
        }
    }
}
