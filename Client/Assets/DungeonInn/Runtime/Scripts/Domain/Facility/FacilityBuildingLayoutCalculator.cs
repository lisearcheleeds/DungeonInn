using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Facility
{
    public static class FacilityBuildingLayoutCalculator
    {
        public static FacilityInteractionPoint CalculateInteractionPoint(
            FacilityBuildingDefinition definition,
            MapLayer layer)
        {
            var entranceCenter = layer.GetCellCenter(definition.EntranceCell);
            var halfCell = layer.CellSizeMeters * 0.5f;
            var offsetX = 0f;
            var offsetZ = 0f;

            switch (definition.FacingDirection)
            {
                case BuildingFacingDirection.South:
                    offsetZ = -halfCell;
                    break;
                case BuildingFacingDirection.North:
                    offsetZ = halfCell;
                    break;
                case BuildingFacingDirection.West:
                    offsetX = -halfCell;
                    break;
                case BuildingFacingDirection.East:
                    offsetX = halfCell;
                    break;
            }

            return new FacilityInteractionPoint(
                new LayerPosition(layer.Id, entranceCenter.X + offsetX, entranceCenter.Z + offsetZ),
                halfCell);
        }
    }
}
