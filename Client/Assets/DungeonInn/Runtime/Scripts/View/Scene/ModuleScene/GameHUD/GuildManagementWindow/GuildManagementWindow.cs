using System;
using System.Collections.Generic;
using LighthouseExtends.ScreenStack;
using LighthouseExtends.UIComponent.Button;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class GuildManagementWindow :
        GameHudScreenStackWindowBase,
        IScreenStackSetup<GuildManagementWindowData>
    {
        GuildManagementWindowData screenStackData;
        readonly List<LHButton> upgradeButtons = new();
        GuildManagementTab activeTab = GuildManagementTab.Overview;

        enum GuildManagementTab
        {
            Overview,
            Inventory,
            Facilities
        }

        public void Setup(GuildManagementWindowData screenStackData)
        {
            this.screenStackData = screenStackData ?? throw new ArgumentNullException(nameof(screenStackData));
            activeTab = GuildManagementTab.Overview;
            Refresh();
        }

        void Refresh()
        {
            ApplyContent("Guild Management", string.Empty);
            SetBodyTextVisible(false);
            RenderContent(screenStackData.ViewData);
            Debug.Log(
                $"[GameHUD.ScreenStack] GuildManagementWindow displayed. " +
                $"Facilities={screenStackData.ViewData.Facilities.Count} Inventory={screenStackData.ViewData.Inventory.Count}");
        }

        void RenderContent(GuildManagementWindowViewData viewData)
        {
            var bodyRoot = GetBodyRoot();
            ClearUpgradeButtons();
            ClearChildren(bodyRoot);

            CreateTabs(bodyRoot, viewData);
            switch (activeTab)
            {
                case GuildManagementTab.Overview:
                    RenderOverview(bodyRoot, viewData);
                    break;
                case GuildManagementTab.Inventory:
                    RenderInventory(bodyRoot, viewData);
                    break;
                case GuildManagementTab.Facilities:
                    RenderFacilities(bodyRoot, viewData);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void CreateTabs(RectTransform bodyRoot, GuildManagementWindowViewData viewData)
        {
            CreateTabButton(bodyRoot, viewData, GuildManagementTab.Overview, "概要", 0);
            CreateTabButton(bodyRoot, viewData, GuildManagementTab.Inventory, "インベントリ", 1);
            CreateTabButton(bodyRoot, viewData, GuildManagementTab.Facilities, "施設", 2);
        }

        void CreateTabButton(
            RectTransform bodyRoot,
            GuildManagementWindowViewData viewData,
            GuildManagementTab tab,
            string label,
            int index)
        {
            var buttonObject = new GameObject(
                $"{tab}TabButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LHButton));
            buttonObject.transform.SetParent(bodyRoot, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(index * 168f, 0f);
            rect.sizeDelta = new Vector2(156f, 42f);
            rect.pivot = new Vector2(0f, 1f);

            var image = buttonObject.GetComponent<Image>();
            image.color = activeTab == tab
                ? new Color(0.2f, 0.32f, 0.42f, 1f)
                : new Color(0.14f, 0.15f, 0.16f, 1f);

            var button = buttonObject.GetComponent<LHButton>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                activeTab = tab;
                RenderContent(viewData);
            });

            var text = CreateAnchoredText(buttonObject.transform, label, 17, Vector2.zero, rect.sizeDelta);
            text.alignment = TextAlignmentOptions.Center;
        }

        static void RenderOverview(RectTransform bodyRoot, GuildManagementWindowViewData viewData)
        {
            CreatePanel(bodyRoot, "KPI", viewData.Kpis, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -62f), new Vector2(370f, 230f));
            CreatePanel(bodyRoot, "Transactions", viewData.Transactions, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(394f, -62f), new Vector2(822f, 500f));
        }

        static void RenderInventory(RectTransform bodyRoot, GuildManagementWindowViewData viewData)
        {
            CreatePanel(bodyRoot, "Combined Inventory", viewData.Inventory, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -62f), new Vector2(560f, 500f));
        }

        void RenderFacilities(RectTransform bodyRoot, GuildManagementWindowViewData viewData)
        {
            for (var index = 0; index < viewData.Facilities.Count; index++)
            {
                CreateFacilityRow(bodyRoot, viewData.Facilities[index], index);
            }
        }

        void ClearUpgradeButtons()
        {
            foreach (var button in upgradeButtons)
            {
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                Destroy(button.gameObject);
            }

            upgradeButtons.Clear();
        }

        void CreateFacilityUpgradeButton(RectTransform rowRoot, GuildFacilityRowViewData facility, int index)
        {
            var buttonObject = new GameObject(
                $"UpgradeFacility{index}Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LHButton));
            buttonObject.transform.SetParent(rowRoot, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-18f, -75f);
            rect.sizeDelta = new Vector2(144f, 42f);
            rect.pivot = new Vector2(1f, 0.5f);

            var image = buttonObject.GetComponent<Image>();
            image.color = facility.CanUpgrade
                ? new Color(0.18f, 0.34f, 0.48f, 1f)
                : new Color(0.16f, 0.17f, 0.18f, 1f);

            var button = buttonObject.GetComponent<LHButton>();
            button.targetGraphic = image;
            button.interactable = facility.CanUpgrade;
            if (facility.CanUpgrade)
            {
                button.onClick.AddListener(() => UpgradeFacility(facility.FacilityId));
            }
            upgradeButtons.Add(button);

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 15;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.text = facility.CanUpgrade ? $"Upgrade {index + 1}" : $"Locked {index + 1}";
        }

        void UpgradeFacility(Guid facilityId)
        {
            screenStackData.UpgradeFacility(facilityId);
            Debug.Log($"[GameHUD.ScreenStack] Facility upgraded from UI. FacilityId={facilityId}");
            screenStackData.Refresh();
            Refresh();
        }

        void CreateFacilityRow(RectTransform bodyRoot, GuildFacilityRowViewData facility, int index)
        {
            var rowObject = new GameObject($"FacilityRow{index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rowObject.transform.SetParent(bodyRoot, false);
            var rect = rowObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(0f, -62f - index * 168f);
            rect.sizeDelta = new Vector2(0f, 150f);
            rect.pivot = new Vector2(0.5f, 1f);

            rowObject.GetComponent<Image>().color = facility.CanUpgrade
                ? new Color(0.1f, 0.15f, 0.18f, 0.94f)
                : new Color(0.13f, 0.13f, 0.14f, 0.94f);

            CreateAnchoredText(rowObject.transform, facility.Description, 18, new Vector2(18f, -16f), new Vector2(780f, 42f));
            CreateAnchoredText(rowObject.transform, $"Upgrade: {facility.UpgradeStatus}", 16, new Vector2(18f, -58f), new Vector2(780f, 30f))
                .color = facility.CanUpgrade ? new Color(0.7f, 0.92f, 1f, 1f) : new Color(0.8f, 0.72f, 0.64f, 1f);
            CreateAnchoredText(
                rowObject.transform,
                facility.Lineup.Count == 0 ? "Lineup: None" : $"Lineup: {string.Join(", ", facility.Lineup)}",
                16,
                new Vector2(18f, -92f),
                new Vector2(940f, 42f));
            CreateFacilityUpgradeButton(rect, facility, index);
        }

        static void CreatePanel(
            RectTransform bodyRoot,
            string title,
            IReadOnlyList<string> lines,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            var panelObject = new GameObject($"{title}Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(bodyRoot, false);
            var rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.pivot = new Vector2(anchorMin.x, 1f);
            panelObject.GetComponent<Image>().color = new Color(0.11f, 0.12f, 0.13f, 0.94f);

            CreateAnchoredText(panelObject.transform, title, 19, new Vector2(14f, -12f), new Vector2(size.x - 28f, 28f))
                .fontStyle = FontStyles.Bold;
            CreateAnchoredText(
                panelObject.transform,
                lines.Count == 0 ? "None" : string.Join("\n", lines),
                15,
                new Vector2(14f, -44f),
                new Vector2(size.x - 28f, size.y - 54f));
        }

        static void ClearChildren(RectTransform root)
        {
            for (var index = root.childCount - 1; 0 <= index; index--)
            {
                Destroy(root.GetChild(index).gameObject);
            }
        }

        static TMP_Text CreateAnchoredText(Transform parent, string text, int fontSize, Vector2 position, Vector2 size)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var textComponent = textObject.GetComponent<TMP_Text>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.TopLeft;
            textComponent.textWrappingMode = TextWrappingModes.Normal;

            var rect = textComponent.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.pivot = new Vector2(0f, 1f);
            return textComponent;
        }
    }
}
