using System;
using DungeonInn.Application.Event;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class GrantExperienceUseCase
    {
        readonly GrantExperienceService grantExperienceService;

        [Inject]
        public GrantExperienceUseCase(GrantExperienceService grantExperienceService)
        {
            this.grantExperienceService = grantExperienceService ?? throw new ArgumentNullException(nameof(grantExperienceService));
        }

        public GrantExperienceUseCase(IMasterRepository masterRepository, IEventPublisher eventPublisher)
            : this(new GrantExperienceService(masterRepository, eventPublisher))
        {
        }

        public void Execute(Actor killer, Actor defeated)
        {
            grantExperienceService.Execute(killer, defeated);
        }
    }
}
