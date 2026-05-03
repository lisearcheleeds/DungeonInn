using DungeonInn.Domain.Character;
using UnityEngine;

namespace DungeonInn.Infrastructure.Repository
{
    [CreateAssetMenu(menuName = "DungeonInn/Config/MonsterConfig")]
    public class MonsterConfigSO : ScriptableObject
    {
        public int BaseMaxHp = 50;
        public int BaseAttackPower = 8;
        public int BaseDefense = 3;
        public float PatrolSpeed = 2.0f;
        public float FloorScaleMultiplier = 0.3f;

        public MonsterConfigData ToData()
        {
            return new MonsterConfigData
            {
                BaseMaxHp = BaseMaxHp,
                BaseAttackPower = BaseAttackPower,
                BaseDefense = BaseDefense,
                PatrolSpeed = PatrolSpeed,
                FloorScaleMultiplier = FloorScaleMultiplier,
            };
        }
    }
}
