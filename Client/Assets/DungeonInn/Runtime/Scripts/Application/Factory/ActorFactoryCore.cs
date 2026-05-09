using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;

namespace DungeonInn.Application.Factory
{
    internal static class ActorFactoryCore
    {
        public static Actor CreateActor(
            Guid actorId,
            ActorArchetypeMaster archetypeMaster,
            LevelTable levelTable,
            LayerPosition position,
            ActorFaction faction,
            int preferenceSeed,
            IActorBehavior behavior,
            IItemStackLimitResolver stackLimitResolver)
        {
            var initialXp = levelTable.GetExperienceForLevel(archetypeMaster.InitialLevel);
            var actor = new Actor(
                actorId,
                archetypeMaster.Id,
                archetypeMaster.BaseStats,
                new Inventory(stackLimitResolver),
                archetypeMaster.InitialLevel,
                initialXp,
                1,
                0,
                0,
                0,
                preferenceSeed,
                position,
                faction,
                behavior);
            actor.RecalculateLevel(levelTable);
            RecoverFully(actor);
            return actor;
        }

        static void RecoverFully(Actor actor)
        {
            actor.Recover(actor.Params.MaxHp, actor.Params.MaxMp, 0, 0, 0);
        }
    }
}
