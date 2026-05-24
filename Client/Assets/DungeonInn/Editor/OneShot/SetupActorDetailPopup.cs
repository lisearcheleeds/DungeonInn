using System;
using DungeonInn.View.Scene.ModuleScene.GameHUD;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonInn.Editor.OneShot
{
    public static class SetupActorDetailPopupOneShot
    {
        const string GameHUDPrefabDirectory = "Assets/DungeonInn/Runtime/Prefab/GameHUD";
        const string ActorDetailPopupPrefabPath = GameHUDPrefabDirectory + "/ActorDetailPopup.prefab";

        public static void Run()
        {
            CreateOrLoadActorDetailPopupPrefab();
        }

        static ActorDetailPopup CreateOrLoadActorDetailPopupPrefab()
        {
            EnsureDirectory(GameHUDPrefabDirectory);
            var prefabExists = AssetDatabase.AssetPathExists(ActorDetailPopupPrefabPath);
            var popupObject = prefabExists
                ? PrefabUtility.LoadPrefabContents(ActorDetailPopupPrefabPath)
                : new GameObject("ActorDetailPopup", typeof(RectTransform));

            ConfigureActorDetailPopupPrefab(popupObject);
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(popupObject, ActorDetailPopupPrefabPath);

            if (prefabExists)
            {
                PrefabUtility.UnloadPrefabContents(popupObject);
            }
            else
            {
                Object.DestroyImmediate(popupObject);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DungeonInn] Updated {ActorDetailPopupPrefabPath}");
            return savedPrefab != null ? savedPrefab.GetComponent<ActorDetailPopup>() : null;
        }

        static void ConfigureActorDetailPopupPrefab(GameObject popupObject)
        {
            popupObject.name = "ActorDetailPopup";
            var rootRect = popupObject.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = popupObject.AddComponent<RectTransform>();
            }

            while (0 < rootRect.childCount)
            {
                Object.DestroyImmediate(rootRect.GetChild(0).gameObject);
            }

            rootRect.sizeDelta = new Vector2(240f, 210f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            var popup = popupObject.GetComponent<ActorDetailPopup>();
            if (popup == null)
            {
                popup = popupObject.AddComponent<ActorDetailPopup>();
            }

            var bodyColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            var statColor = new Color(0.82f, 0.9f, 1f, 1f);
            var equipmentColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            var nameText = CreateActorDetailPopupText(
                rootRect,
                "NameText",
                "Name",
                new Vector2(8f, -8f),
                new Vector2(224f, 20f),
                18,
                new Color(0.95f, 0.95f, 0.95f, 1f));
            var levelText = CreateActorDetailPopupText(
                rootRect,
                "LevelText",
                "Lv.1",
                new Vector2(8f, -30f),
                new Vector2(104f, 18f),
                14,
                bodyColor);
            var hpText = CreateActorDetailPopupText(
                rootRect,
                "HpText",
                "HP 0/0",
                new Vector2(8f, -50f),
                new Vector2(104f, 18f),
                14,
                bodyColor);
            var mpText = CreateActorDetailPopupText(
                rootRect,
                "MpText",
                "MP 0/0",
                new Vector2(120f, -50f),
                new Vector2(104f, 18f),
                14,
                bodyColor);
            var fatigueText = CreateActorDetailPopupText(
                rootRect,
                "FatigueText",
                "Fatigue 0",
                new Vector2(8f, -70f),
                new Vector2(104f, 18f),
                14,
                bodyColor);
            var goldText = CreateActorDetailPopupText(
                rootRect,
                "GoldText",
                "Gold 0",
                new Vector2(120f, -70f),
                new Vector2(104f, 18f),
                14,
                bodyColor);
            var statTexts = new[]
            {
                CreateActorDetailPopupText(
                    rootRect,
                    "StatStrengthText",
                    "STR 0",
                    new Vector2(8f, -96f),
                    new Vector2(68f, 18f),
                    13,
                    statColor),
                CreateActorDetailPopupText(
                    rootRect,
                    "StatDexterityText",
                    "DEX 0",
                    new Vector2(84f, -96f),
                    new Vector2(68f, 18f),
                    13,
                    statColor),
                CreateActorDetailPopupText(
                    rootRect,
                    "StatConstitutionText",
                    "CON 0",
                    new Vector2(160f, -96f),
                    new Vector2(68f, 18f),
                    13,
                    statColor),
                CreateActorDetailPopupText(
                    rootRect,
                    "StatIntelligenceText",
                    "INT 0",
                    new Vector2(8f, -116f),
                    new Vector2(68f, 18f),
                    13,
                    statColor),
                CreateActorDetailPopupText(
                    rootRect,
                    "StatWisdomText",
                    "WIS 0",
                    new Vector2(84f, -116f),
                    new Vector2(68f, 18f),
                    13,
                    statColor),
                CreateActorDetailPopupText(
                    rootRect,
                    "StatCharismaText",
                    "CHA 0",
                    new Vector2(160f, -116f),
                    new Vector2(68f, 18f),
                    13,
                    statColor)
            };
            var equipmentTexts = new[]
            {
                CreateActorDetailPopupText(
                    rootRect,
                    "EquipmentWeaponText",
                    "Weapon -",
                    new Vector2(8f, -148f),
                    new Vector2(104f, 18f),
                    13,
                    equipmentColor),
                CreateActorDetailPopupText(
                    rootRect,
                    "EquipmentArmorText",
                    "Armor -",
                    new Vector2(120f, -148f),
                    new Vector2(104f, 18f),
                    13,
                    equipmentColor),
                CreateActorDetailPopupText(
                    rootRect,
                    "EquipmentAccessoryText",
                    "Accessory -",
                    new Vector2(8f, -168f),
                    new Vector2(216f, 18f),
                    13,
                    equipmentColor)
            };

            var serializedPopup = new SerializedObject(popup);
            serializedPopup.FindProperty("popupOffsetX").floatValue = 120f;
            serializedPopup.FindProperty("nameText").objectReferenceValue = nameText;
            serializedPopup.FindProperty("levelText").objectReferenceValue = levelText;
            serializedPopup.FindProperty("hpText").objectReferenceValue = hpText;
            serializedPopup.FindProperty("mpText").objectReferenceValue = mpText;
            serializedPopup.FindProperty("fatigueText").objectReferenceValue = fatigueText;
            serializedPopup.FindProperty("goldText").objectReferenceValue = goldText;
            AssignObjectArray(serializedPopup.FindProperty("statTexts"), statTexts);
            AssignObjectArray(serializedPopup.FindProperty("equipmentTexts"), equipmentTexts);
            serializedPopup.ApplyModifiedPropertiesWithoutUndo();
        }

        static Component CreateActorDetailPopupText(
            Transform parent,
            string objectName,
            string text,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            Color color)
        {
            var textType = ResolveTextMeshProType();
            var textObject = new GameObject(objectName, typeof(RectTransform));
            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            var label = (Component)textObject.AddComponent(textType);
            var serializedLabel = new SerializedObject(label);
            serializedLabel.FindProperty("m_text").stringValue = text;
            serializedLabel.FindProperty("m_fontSize").floatValue = fontSize;
            serializedLabel.FindProperty("m_fontColor").colorValue = color;
            serializedLabel.FindProperty("m_RaycastTarget").boolValue = false;
            serializedLabel.FindProperty("m_HorizontalAlignment").intValue = 1;
            serializedLabel.FindProperty("m_VerticalAlignment").intValue = 256;
            serializedLabel.ApplyModifiedPropertiesWithoutUndo();
            return label;
        }

        static Type ResolveTextMeshProType()
        {
            var textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (textType == null)
            {
                throw new InvalidOperationException("TextMeshProUGUI type was not found.");
            }

            return textType;
        }

        static void AssignObjectArray(SerializedProperty property, Component[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
