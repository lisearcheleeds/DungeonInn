using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;

namespace DungeonInn.Application.UseCase
{
    public sealed class AssignStaffUseCase
    {
        public UniTask ExecuteAsync(
            AdventurerGuild guild,
            Character staff,
            Guid facilityId,
            IReadOnlyDictionary<Guid, Character> staffById)
        {
            guild.AssignStaff(staff, facilityId);
            guild.RecalculateFacilityPoints(staffById);
            return UniTask.CompletedTask;
        }
    }
}
