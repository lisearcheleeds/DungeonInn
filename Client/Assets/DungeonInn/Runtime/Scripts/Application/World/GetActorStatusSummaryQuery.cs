using System;
using System.Collections.Generic;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.World
{
    public sealed class GetActorStatusSummaryQuery
    {
        readonly IGameWorldStateReader worldState;
        readonly IMasterRepository masterRepository;
        readonly List<ActorEffectIconViewData> effectBuffer = new();
        readonly Dictionary<Guid, CachedEffectIcons> cachedEffectIconsByActorId = new();

        [Inject]
        public GetActorStatusSummaryQuery(
            IGameWorldStateReader worldState,
            IMasterRepository masterRepository)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public ActorStatusViewData? Query(Guid actorId)
        {
            var actor = worldState.FindActor(actorId);
            if (actor == null)
            {
                cachedEffectIconsByActorId.Remove(actorId);
                return null;
            }

            var maxHp = actor.Params.MaxHp;
            var hpRatio = 0 < maxHp ? (float)actor.Hp / maxHp : 0f;

            var activeEffects = GetActiveEffects(actorId, actor.ActorEffects);

            return new ActorStatusViewData(actorId, Math.Clamp(hpRatio, 0f, 1f), activeEffects);
        }

        IReadOnlyList<ActorEffectIconViewData> GetActiveEffects(
            Guid actorId,
            IReadOnlyList<Domain.Actor.ActorEffectInstance> actorEffects)
        {
            var signature = CalculateEffectSignature(actorEffects);
            if (cachedEffectIconsByActorId.TryGetValue(actorId, out var cached) &&
                cached.Signature == signature)
            {
                return cached.Effects;
            }

            effectBuffer.Clear();
            foreach (var effect in actorEffects)
            {
                if (effect.IsExpired)
                {
                    continue;
                }

                var master = masterRepository.GetActorEffectMaster(effect.ActorEffectMasterId);
                var remainingSeconds = effect.DurationSeconds - effect.ElapsedSeconds;
                effectBuffer.Add(new ActorEffectIconViewData(
                    effect.ActorEffectMasterId,
                    master.Name,
                    remainingSeconds));
            }

            var activeEffects = effectBuffer.Count == 0
                ? Array.Empty<ActorEffectIconViewData>()
                : effectBuffer.ToArray();
            cachedEffectIconsByActorId[actorId] = new CachedEffectIcons(signature, activeEffects);
            return activeEffects;
        }

        static int CalculateEffectSignature(IReadOnlyList<Domain.Actor.ActorEffectInstance> actorEffects)
        {
            unchecked
            {
                var hash = 17;
                for (var index = 0; index < actorEffects.Count; index++)
                {
                    var effect = actorEffects[index];
                    if (effect.IsExpired)
                    {
                        continue;
                    }

                    var remainingSeconds = Math.Max(0f, effect.DurationSeconds - effect.ElapsedSeconds);
                    hash = hash * 31 + effect.ActorEffectMasterId;
                    hash = hash * 31 + effect.InstanceId.GetHashCode();
                    hash = hash * 31 + (int)MathF.Ceiling(remainingSeconds);
                }

                return hash;
            }
        }

        readonly struct CachedEffectIcons
        {
            public CachedEffectIcons(int signature, IReadOnlyList<ActorEffectIconViewData> effects)
            {
                Signature = signature;
                Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            }

            public int Signature { get; }
            public IReadOnlyList<ActorEffectIconViewData> Effects { get; }
        }
    }
}
