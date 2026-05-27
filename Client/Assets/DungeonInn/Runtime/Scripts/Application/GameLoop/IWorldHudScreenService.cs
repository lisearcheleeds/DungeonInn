using DungeonInn.Application.Economy;

namespace DungeonInn.Application.GameLoop
{
    public interface IWorldHudScreenService
    {
        bool CanGetInnEconomyStatus { get; }

        GameTimeState GetTimeState();

        InnEconomyStatus GetInnEconomyStatus();

        GameTimeState TogglePause();

        void SetTimeScale(float timeScale);
    }
}
