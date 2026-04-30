using DungeonInn.Runtime.Scripts.Extensions;
using DungeonInn.Runtime.Scripts.View.World;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonInn.Runtime.Scripts.View.HUD
{
    public class FloorSwitcherView : MonoBehaviour
    {
        [SerializeField] Button[] floorButtons;

        IWorldRenderer worldRenderer;
        readonly CompositeDisposable disposables = new();

        void Start()
        {
            worldRenderer = Object.FindFirstObjectByType<WorldRenderer>();

            for (int i = 0; i < floorButtons.Length; i++)
            {
                int floorIndex = i;
                floorButtons[i].SubscribeOnClick(() => worldRenderer?.SetActiveFloor(floorIndex))
                    .AddTo(disposables);
            }
        }

        void OnDestroy()
        {
            disposables.Dispose();
        }
    }
}
