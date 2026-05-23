namespace DungeonInn.Domain.Guild
{
    public sealed class InnEconomyState
    {
        public int Reputation { get; private set; }

        public InnEconomyState()
            : this(10)
        {
        }

        public InnEconomyState(int reputation)
        {
            Reputation = System.Math.Max(0, reputation);
        }
    }
}
