namespace DungeonInn.Domain.Actor
{
    public interface IActorBehavior
    {
        void OnRecovered(
            int hpAmount,
            int mpAmount,
            int fatigueReduction,
            int stressReduction,
            int injuryReduction);
    }
}
