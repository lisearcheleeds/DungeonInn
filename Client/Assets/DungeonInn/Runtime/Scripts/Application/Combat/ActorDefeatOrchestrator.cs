using System;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.Combat
{
    public sealed class ActorDefeatOrchestrator
    {
        readonly CombatDefeatResolver combatDefeatResolver;
        readonly GrantExperienceService grantExperienceService;
        readonly DropItemService dropItemService;

        [Inject]
        public ActorDefeatOrchestrator(
            CombatDefeatResolver combatDefeatResolver,
            GrantExperienceService grantExperienceService,
            DropItemService dropItemService)
        {
            this.combatDefeatResolver = combatDefeatResolver ?? throw new ArgumentNullException(nameof(combatDefeatResolver));
            this.grantExperienceService = grantExperienceService ?? throw new ArgumentNullException(nameof(grantExperienceService));
            this.dropItemService = dropItemService ?? throw new ArgumentNullException(nameof(dropItemService));
        }

        public void Execute(IGameWorldState worldState, Actor attacker, Actor target)
        {
            if (attacker != null)
            {
                grantExperienceService.Execute(attacker, target);
            }

            dropItemService.Execute(target, worldState);
            combatDefeatResolver.Resolve(worldState, attacker, target);
        }
    }
}
