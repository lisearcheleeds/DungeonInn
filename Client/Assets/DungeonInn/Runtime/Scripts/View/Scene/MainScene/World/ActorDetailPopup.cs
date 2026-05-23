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
            nameText.text = viewData.DisplayName;
            levelText.text = $"Lv.{viewData.Level}";
            hpText.text = $"HP {viewData.CurrentHp}/{viewData.MaxHp}";
            mpText.text = $"MP {viewData.CurrentMp}/{viewData.MaxMp}";
            fatigueText.text = $"Fatigue {viewData.Fatigue}";
            goldText.text = $"Gold {viewData.Gold}";

            statTexts[0].text = $"STR {viewData.Strength}";
            statTexts[1].text = $"DEX {viewData.Dexterity}";
            statTexts[2].text = $"CON {viewData.Constitution}";
            statTexts[3].text = $"INT {viewData.Intelligence}";
            statTexts[4].text = $"WIS {viewData.Wisdom}";
            statTexts[5].text = $"CHA {viewData.Charisma}";

            for (var i = 0; i < equipmentTexts.Length; i++)
            {
                equipmentTexts[i].text = i < viewData.EquipmentNames.Count ? viewData.EquipmentNames[i] : "-";
            }
        }

        public void SetPosition(Vector2 screenPosition)
        {
            var rt = (RectTransform)transform;
            rt.position = new Vector3(screenPosition.x + popupOffsetX, screenPosition.y, 0f);
        }
    }
}
