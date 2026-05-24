using System;

namespace DungeonInn.Application.World
{
    public sealed class AdventurerReturnPolicySettings
    {
        public int DecisionThresholdScore { get; }
        public int GoalCompletedScore { get; }
        public int CriticalHpScore { get; }
        public int LowHpWithoutRecoveryItemScore { get; }
        public float LowHpRatio { get; }
        public float CriticalHpRatio { get; }

        public AdventurerReturnPolicySettings(
            int decisionThresholdScore,
            int goalCompletedScore,
            int criticalHpScore,
            int lowHpWithoutRecoveryItemScore,
            float lowHpRatio,
            float criticalHpRatio)
        {
            DecisionThresholdScore = Math.Max(0, decisionThresholdScore);
            GoalCompletedScore = Math.Max(0, goalCompletedScore);
            CriticalHpScore = Math.Max(0, criticalHpScore);
            LowHpWithoutRecoveryItemScore = Math.Max(0, lowHpWithoutRecoveryItemScore);
            LowHpRatio = Math.Max(0f, lowHpRatio);
            CriticalHpRatio = Math.Max(0f, criticalHpRatio);
        }

        public static AdventurerReturnPolicySettings CreateDefault()
        {
            return new AdventurerReturnPolicySettings(100, 100, 100, 100, 0.6f, 0.3f);
        }
    }
}
