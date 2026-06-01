using System.Collections.Generic;
using DungeonInn.Domain.Facility;

namespace DungeonInn.Application.Facilities
{
    public interface IFacilityBuildingDefinitionRepository
    {
        IReadOnlyList<FacilityBuildingDefinition> GetDefinitions();
    }
}
