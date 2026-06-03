#if DEBUG
using System;
using UnityEngine;

namespace DungeonInn.Debugging
{
    public static class PlayModeAutomation
    {
        static Action titleNewGameAction;
        static Action titleSeedStartAction;
        static Func<bool> selectRandomActorAction;

        public static bool ClickTitleNewGame()
        {
            if (titleNewGameAction == null)
            {
                Debug.LogError("[PlayModeAutomation] Title NewGame action is not registered.");
                return false;
            }

            titleNewGameAction.Invoke();
            Debug.Log("[PlayModeAutomation] Title NewGame action invoked.");
            return true;
        }

        public static bool ClickTitleSeedStart()
        {
            if (titleSeedStartAction == null)
            {
                Debug.LogError("[PlayModeAutomation] Title Seed Start action is not registered.");
                return false;
            }

            titleSeedStartAction.Invoke();
            Debug.Log("[PlayModeAutomation] Title Seed Start action invoked.");
            return true;
        }

        public static bool SelectRandomActor()
        {
            if (selectRandomActorAction == null)
            {
                Debug.LogError("[PlayModeAutomation] Random actor selection action is not registered.");
                return false;
            }

            var selected = selectRandomActorAction.Invoke();
            if (selected)
            {
                Debug.Log("[PlayModeAutomation] Random actor selected.");
            }
            else
            {
                Debug.LogWarning("[PlayModeAutomation] Random actor selection skipped because no actor candidate exists.");
            }

            return selected;
        }

        public static void RegisterTitleActions(Action newGameAction, Action seedStartAction)
        {
            titleNewGameAction = newGameAction;
            titleSeedStartAction = seedStartAction;
        }

        public static void ClearTitleActions(Action newGameAction, Action seedStartAction)
        {
            if (titleNewGameAction == newGameAction)
            {
                titleNewGameAction = null;
            }

            if (titleSeedStartAction == seedStartAction)
            {
                titleSeedStartAction = null;
            }
        }

        public static void RegisterWorldActions(Func<bool> selectRandomActor)
        {
            selectRandomActorAction = selectRandomActor;
        }

        public static void ClearWorldActions(Func<bool> selectRandomActor)
        {
            if (selectRandomActorAction == selectRandomActor)
            {
                selectRandomActorAction = null;
            }
        }
    }
}
#endif
