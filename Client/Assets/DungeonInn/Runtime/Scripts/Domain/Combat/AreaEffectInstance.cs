using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Combat
{
    public sealed class AreaEffectInstance
    {
        readonly HashSet<Guid> hitActorIds = new();
        float elapsedSinceLastHitSeconds = float.MaxValue;
        bool hasApplied;

        public Guid Id { get; }
        public Guid AttackerActorId { get; }
        public int SourceFactionId { get; }
        public LayerPosition CenterPosition { get; }
        public WeaponAttackSpec AttackSpec { get; }
        public int SourceNodeId { get; }
        public CombatEffectExecutionId ExecutionId { get; }
        public AttackAreaSpec AreaSpec { get; }
        public double HalfAngleCos { get; }
        public int Damage { get; }
        public float RemainingDurationSeconds { get; private set; }
        public IReadOnlyCollection<Guid> HitActorIds => hitActorIds;

        public AreaEffectInstance(
            Guid id,
            Guid attackerActorId,
            int sourceFactionId,
            LayerPosition centerPosition,
            AttackAreaSpec areaSpec,
            int damage)
            : this(
                id,
                attackerActorId,
                sourceFactionId,
                centerPosition,
                null,
                0,
                CombatEffectExecutionId.New(),
                areaSpec,
                damage)
        {
        }

        public AreaEffectInstance(
            Guid id,
            Guid attackerActorId,
            int sourceFactionId,
            LayerPosition centerPosition,
            WeaponAttackSpec attackSpec,
            int sourceNodeId,
            CombatEffectExecutionId executionId,
            AttackAreaSpec areaSpec,
            int damage)
        {
            Id = id;
            AttackerActorId = attackerActorId;
            SourceFactionId = sourceFactionId;
            CenterPosition = centerPosition;
            AttackSpec = attackSpec;
            SourceNodeId = sourceNodeId;
            ExecutionId = executionId;
            AreaSpec = areaSpec ?? throw new ArgumentNullException(nameof(areaSpec));
            HalfAngleCos = CalculateHalfAngleCos(areaSpec);
            Damage = Math.Max(0, damage);
            RemainingDurationSeconds = areaSpec.DurationType == AttackAreaDurationType.Duration
                ? Math.Max(0, areaSpec.DurationTicks)
                : 0f;
        }

        public void Advance(float deltaGameSeconds)
        {
            var delta = Math.Max(0f, deltaGameSeconds);
            elapsedSinceLastHitSeconds += delta;
            if (AreaSpec.DurationType == AttackAreaDurationType.Duration)
            {
                RemainingDurationSeconds = Math.Max(0f, RemainingDurationSeconds - delta);
            }
        }

        public bool CanApply()
        {
            if (AreaSpec.DurationType == AttackAreaDurationType.Instant)
            {
                return !hasApplied;
            }

            return AreaSpec.HitIntervalType == AttackHitIntervalType.EverySecond
                ? !hasApplied || 1f <= elapsedSinceLastHitSeconds
                : 0f < RemainingDurationSeconds;
        }

        public bool CanHitActor(Guid actorId)
        {
            return AreaSpec.HitIntervalType != AttackHitIntervalType.OncePerTarget ||
                !hitActorIds.Contains(actorId);
        }

        public void MarkHitActor(Guid actorId)
        {
            hitActorIds.Add(actorId);
        }

        public void MarkApplied()
        {
            hasApplied = true;
            elapsedSinceLastHitSeconds = 0f;
        }

        public bool IsExpired => AreaSpec.DurationType == AttackAreaDurationType.Instant
            ? hasApplied
            : RemainingDurationSeconds <= 0f;

        static double CalculateHalfAngleCos(AttackAreaSpec areaSpec)
        {
            if (areaSpec.Shape != AttackAreaShape.Fan)
            {
                return 0.0;
            }

            var halfAngle = areaSpec.AngleDegrees * 0.5f;
            if (90f <= halfAngle)
            {
                return 0.0;
            }

            return Math.Cos(halfAngle * Math.PI / 180.0);
        }
    }
}
