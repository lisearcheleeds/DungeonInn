using DungeonInn.Domain.Character;
using UnityEngine;

namespace DungeonInn.Infrastructure.Repository
{
    [CreateAssetMenu(menuName = "DungeonInn/Config/AdventurerConfig")]
    public class AdventurerConfigSO : ScriptableObject
    {
        public int BaseMaxHp = 100;
        public int BaseAttackPower = 10;
        public int BaseDefense = 5;
        public float BaseMoveSpeed = 3.0f;
        public float BaseAttackSpeed = 1.0f;
        public float SatisfactionThreshold = 0.7f;
        public float RestDuration = 30.0f;
        public float ExploreDuration = 60.0f;

        public AdventurerConfigData ToData()
        {
            return new AdventurerConfigData
            {
                BaseMaxHp = BaseMaxHp,
                BaseAttackPower = BaseAttackPower,
                BaseDefense = BaseDefense,
                BaseMoveSpeed = BaseMoveSpeed,
                BaseAttackSpeed = BaseAttackSpeed,
                SatisfactionThreshold = SatisfactionThreshold,
                RestDuration = RestDuration,
                ExploreDuration = ExploreDuration,
            };
        }
    }
}
