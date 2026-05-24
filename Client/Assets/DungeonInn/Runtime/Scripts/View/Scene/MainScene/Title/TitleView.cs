using System;
using LighthouseExtends.UIComponent.Button;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.Title
{
    public sealed class TitleView : MonoBehaviour
    {
        [SerializeField] LHButton startGameButton;

        public void SetStartGameListener(Action listener)
        {
            if (startGameButton == null)
            {
                return;
            }

            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(() => listener?.Invoke());
        }

        public void SetStartGameInteractable(bool interactable)
        {
            if (startGameButton != null)
            {
                startGameButton.interactable = interactable;
            }
        }
    }
}
