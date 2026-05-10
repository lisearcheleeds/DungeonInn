using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using LighthouseExtends.InputLayer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonInn.Input.Layer
{
    public sealed class WorldSceneInputLayer : IInputLayer
    {
        readonly InputAction togglePauseAction;
        readonly InputAction showInnStatusAction;
        readonly IGameWorldState worldState;
        readonly ToggleGamePauseUseCase toggleGamePauseUseCase;
        readonly GetInnEconomyStatusUseCase getInnEconomyStatusUseCase;

        public WorldSceneInputLayer(
            InputActions inputActions,
            IGameWorldState worldState,
            ToggleGamePauseUseCase toggleGamePauseUseCase,
            GetInnEconomyStatusUseCase getInnEconomyStatusUseCase)
        {
            togglePauseAction = inputActions.Scene.Get().FindAction("TogglePause", true);
            showInnStatusAction = inputActions.Scene.Get().FindAction("ShowInnStatus", true);
            this.worldState = worldState;
            this.toggleGamePauseUseCase = toggleGamePauseUseCase;
            this.getInnEconomyStatusUseCase = getInnEconomyStatusUseCase;
        }

        public bool BlocksAllInput => false;

        public bool OnActionStarted(InputAction.CallbackContext callbackContext)
        {
            return false;
        }

        public bool OnActionPerformed(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.action.id == togglePauseAction.id)
            {
                TogglePauseAsync().Forget();
                return true;
            }

            if (callbackContext.action.id == showInnStatusAction.id)
            {
                if (!worldState.IsInitialized)
                {
                    return true;
                }

                ShowInnStatusAsync().Forget();
                return true;
            }

            return false;
        }

        public bool OnActionCanceled(InputAction.CallbackContext callbackContext)
        {
            return false;
        }

        async UniTask TogglePauseAsync()
        {
            var timeState = await toggleGamePauseUseCase.ExecuteAsync();
            Debug.Log(timeState.IsPaused ? "[Time] Paused" : "[Time] Resumed");
        }

        async UniTask ShowInnStatusAsync()
        {
            var status = await getInnEconomyStatusUseCase.ExecuteAsync();
            Debug.Log(
                $"[InnStatus] Day={status.CurrentDay} Guests={status.GuestsToday} " +
                $"Demand={status.DemandToday} Rejected={status.RejectedGuestsToday} " +
                $"Occupancy={status.OccupiedRooms}/{status.RoomCapacity} ({status.OccupancyPercent}%) " +
                $"Sales={status.SalesToday}G Satisfaction={status.SatisfactionDeltaToday:+#;-#;0} " +
                $"Reputation={status.Reputation} Treasury={status.GuildGold}G " +
                $"Stock(Sword={status.RookieSwordStock}, Armor={status.RookieArmorStock})");
        }
    }
}
