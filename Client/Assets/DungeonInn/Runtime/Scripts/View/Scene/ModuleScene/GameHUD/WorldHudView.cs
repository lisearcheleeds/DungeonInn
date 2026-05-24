using LighthouseExtends.UIComponent.Button;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class WorldHudView : MonoBehaviour
    {
        [SerializeField] TMP_Text dayTimeText;
        [SerializeField] TMP_Text goldText;
        [SerializeField] TMP_Text alertText;
        [SerializeField] LHButton pauseButton;
        [SerializeField] LHButton speedNormalButton;
        [SerializeField] LHButton speedFastButton;

        TMP_Text pauseButtonLabel;

        public void SetDayTime(string text)
        {
            if (dayTimeText != null)
            {
                dayTimeText.text = text;
            }
        }

        public void SetGold(string text)
        {
            if (goldText != null)
            {
                goldText.text = text;
            }
        }

        public void ShowAlert(string text)
        {
            if (alertText == null)
            {
                return;
            }

            alertText.gameObject.SetActive(true);
            alertText.text = text;
        }

        public void HideAlert()
        {
            if (alertText != null)
            {
                alertText.gameObject.SetActive(false);
            }
        }

        public void SetPauseButtonLabel(string text)
        {
            EnsurePauseButtonLabel();
            if (pauseButtonLabel != null)
            {
                pauseButtonLabel.text = text;
            }
        }

        public void AddPauseListener(UnityAction onClick)
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(onClick);
            }
        }

        public void AddSpeedNormalListener(UnityAction onClick)
        {
            if (speedNormalButton != null)
            {
                speedNormalButton.onClick.AddListener(onClick);
            }
        }

        public void AddSpeedFastListener(UnityAction onClick)
        {
            if (speedFastButton != null)
            {
                speedFastButton.onClick.AddListener(onClick);
            }
        }

        void Awake()
        {
            EnsurePauseButtonLabel();
        }

        void EnsurePauseButtonLabel()
        {
            if (pauseButtonLabel != null || pauseButton == null)
            {
                return;
            }

            pauseButtonLabel = pauseButton.GetComponentInChildren<TMP_Text>(true);
        }
    }
}
