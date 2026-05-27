using System;
using DungeonInn.Application.World;
using DungeonInn.Domain.Facility;

namespace DungeonInn.Application.Economy
{
    public sealed class FacilityEffectService
    {
        public float CalculateInnHpRecoveryPercentPerMinute(
            Facility facility,
            InnBalanceSettings innBalanceSettings)
        {
            if (facility == null)
            {
                throw new ArgumentNullException(nameof(facility));
            }

            if (innBalanceSettings == null)
            {
                throw new ArgumentNullException(nameof(innBalanceSettings));
            }

            if (facility.Type != FacilityType.Inn)
            {
                throw new InvalidOperationException("Facility is not inn.");
            }

            return innBalanceSettings.HpRecoveryPercentPerMinute * Math.Max(1, facility.Quality);
        }
    }
}
