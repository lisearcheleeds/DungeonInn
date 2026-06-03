#if DEBUG
using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using DungeonInn.Debugging;
using DungeonInn.View.Scene.Bridge;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World.Debug
{
    public sealed class WorldActorSelectionAutomation : IInitializable, IDisposable
    {
        readonly ActorSelectionService actorSelectionService;
        readonly IActorSelectionCandidateProvider candidateProvider;
        readonly List<Guid> actorIdBuffer = new();
        readonly System.Random random = new();

        [Inject]
        public WorldActorSelectionAutomation(
            ActorSelectionService actorSelectionService,
            IActorSelectionCandidateProvider candidateProvider)
        {
            this.actorSelectionService =
                actorSelectionService ?? throw new ArgumentNullException(nameof(actorSelectionService));
            this.candidateProvider = candidateProvider ?? throw new ArgumentNullException(nameof(candidateProvider));
        }

        public void Initialize()
        {
            PlayModeAutomation.RegisterWorldActions(SelectRandomActor);
        }

        public void Dispose()
        {
            PlayModeAutomation.ClearWorldActions(SelectRandomActor);
        }

        bool SelectRandomActor()
        {
            candidateProvider.CopyActorIdsTo(actorIdBuffer);
            if (actorIdBuffer.Count == 0)
            {
                return false;
            }

            var actorId = actorIdBuffer[random.Next(actorIdBuffer.Count)];
            actorSelectionService.Select(actorId);
            UnityEngine.Debug.Log($"[WorldActorSelectionAutomation] Selected actor. ActorId={actorId}");
            return true;
        }
    }
}
#endif
