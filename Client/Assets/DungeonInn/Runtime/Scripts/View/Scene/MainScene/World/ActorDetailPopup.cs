using DungeonInn.Application.World;
using TMPro;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ActorDetailPopup : MonoBehaviour
    {
        // Actor の右横にずらす量。Popup 幅の半分程度が目安で Inspector から調整可能
        [SerializeField] float popupOffsetX = 120f;

        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI levelText;
        [SerializeField] TextMeshProUGUI hpText;
        [SerializeField] TextMeshProUGUI mpText;
        [SerializeField] TextMeshProUGUI fatigueText;
        [SerializeField] TextMeshProUGUI goldText;
        [SerializeField] TextMeshProUGUI[] statTexts;
        [SerializeField] TextMeshProUGUI[] equipmentTexts;

        public void SetContent(ActorDetailViewData viewData)
        {
            if (nameText != null)
            {
                nameText.text = viewData.DisplayName;
            }

            if (levelText != null)
            {
                levelText.text = $"Lv.{viewData.Level}";
            }

            if (hpText != null)
            {
                hpText.text = $"HP {viewData.CurrentHp}/{viewData.MaxHp}";
            }

            if (mpText != null)
            {
                mpText.text = $"MP {viewData.CurrentMp}/{viewData.MaxMp}";
            }

            if (fatigueText != null)
            {
                fatigueText.text = $"Fatigue {viewData.Fatigue}";
            }

            if (goldText != null)
            {
                goldText.text = $"Gold {viewData.Gold}";
            }

            if (statTexts != null && 6 <= statTexts.Length)
            {
                if (statTexts[0] != null)
                {
                    statTexts[0].text = $"STR {viewData.Strength}";
                }

                if (statTexts[1] != null)
                {
                    statTexts[1].text = $"DEX {viewData.Dexterity}";
                }

                if (statTexts[2] != null)
                {
                    statTexts[2].text = $"CON {viewData.Constitution}";
                }

                if (statTexts[3] != null)
                {
                    statTexts[3].text = $"INT {viewData.Intelligence}";
                }

                if (statTexts[4] != null)
                {
                    statTexts[4].text = $"WIS {viewData.Wisdom}";
                }

                if (statTexts[5] != null)
                {
                    statTexts[5].text = $"CHA {viewData.Charisma}";
                }
            }

            if (equipmentTexts != null)
            {
                for (var i = 0; i < equipmentTexts.Length; i++)
                {
                    if (equipmentTexts[i] == null)
                    {
                        continue;
                    }

                    equipmentTexts[i].text = i < viewData.EquipmentNames.Count ? viewData.EquipmentNames[i] : "-";
                }
            }
        }

        public void SetPosition(Vector2 screenPosition)
        {
            var rt = (RectTransform)transform;
            rt.position = new Vector3(screenPosition.x + popupOffsetX, screenPosition.y, 0f);
        }
    }
}
