using System;
using System.Collections.Generic;

namespace DungeonInn.Application.Facilities
{
    public sealed class ActorFacilityPresenceService
    {
        readonly Dictionary<Guid, Guid> facilityIdByActorId = new();

        public void Enter(Guid actorId, Guid facilityId)
        {
            if (actorId == Guid.Empty)
            {
                throw new ArgumentException("Actor id is required.", nameof(actorId));
            }

            if (facilityId == Guid.Empty)
            {
                throw new ArgumentException("Facility id is required.", nameof(facilityId));
            }

            facilityIdByActorId[actorId] = facilityId;
        }

        public void Exit(Guid actorId)
        {
            facilityIdByActorId.Remove(actorId);
        }

        public bool IsInsideFacility(Guid actorId)
        {
            return facilityIdByActorId.ContainsKey(actorId);
        }
    }
}
