using TMPro;
using UnityEngine;

namespace DungeonInn.Runtime.Scripts.View.HUD
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] TMP_Text timeLabel;
        [SerializeField] TMP_Text goldLabel;

        void Start()
        {
            timeLabel.text = "Day 1  06:00";
            goldLabel.text = "Gold: 0";
        }
    }
}
