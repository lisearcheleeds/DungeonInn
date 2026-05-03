using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.Inn;

namespace DungeonInn.Application.UseCase
{
    public class AdventurerCheckOutUseCase
    {
        public UniTask<Money> ExecuteAsync(
            InnLand land,
            AdventurerCharacter adventurer,
            Bed bed,
            Room room,
            InnFeeCalculator feeCalculator,
            SatisfactionCalculator satisfactionCalculator,
            InnConfigData config)
        {
            var isPrivateRoom = room.Beds.Count == 1;
            var satisfaction = satisfactionCalculator.Calculate(room.Density, isPrivateRoom);
            adventurer.UpdateSatisfaction(satisfaction);
            var fee = feeCalculator.Calculate(
                satisfaction,
                new Money(config.BaseFee),
                new Money(config.MaxTip),
                config.TipThreshold);
            land.AddFunds(fee);
            bed.CheckOut();
            return UniTask.FromResult(fee);
        }
    }
}
