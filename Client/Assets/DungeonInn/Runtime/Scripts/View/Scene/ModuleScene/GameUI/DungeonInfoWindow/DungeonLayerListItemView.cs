using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonInn.View.Scene.ModuleScene.GameUI.ScreenStack
{
    public sealed class DungeonLayerListItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        TMP_Text popupText;
        GameObject popupRoot;
        DungeonLayerPopupViewData popupData;

        public void Initialize(DungeonLayerPopupViewData popupData)
        {
            this.popupData = popupData;
            EnsurePopup();
            popupText.text = FormatPopup(popupData);
            popupRoot.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            EnsurePopup();
            popupRoot.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        void EnsurePopup()
        {
            if (popupRoot != null)
            {
                return;
            }

            popupRoot = new GameObject("Popup", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
            popupRoot.transform.SetParent(transform, false);
            var rectTransform = popupRoot.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(24f, 0f);
            rectTransform.sizeDelta = new Vector2(360f, 240f);
            rectTransform.pivot = new Vector2(0f, 1f);

            popupText = popupRoot.GetComponent<TMP_Text>();
            popupText.fontSize = 16;
            popupText.color = Color.white;
            popupText.alignment = TextAlignmentOptions.TopLeft;
            popupText.textWrappingMode = TextWrappingModes.Normal;
        }

        static string FormatPopup(DungeonLayerPopupViewData data)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Spawn monsters");
            if (data.Monsters.Count == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                foreach (var monster in data.Monsters)
                {
                    builder.AppendLine($"- {monster.MonsterName} {monster.LevelRange} {monster.Weight}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("Drop items");
            if (data.Drops.Count == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                foreach (var drop in data.Drops)
                {
                    builder.AppendLine(
                        $"- {drop.ItemName} / {drop.SourceMonsterName} / {drop.Chance} / {drop.CountRange}");
                }
            }

            return builder.ToString();
        }
    }
}

