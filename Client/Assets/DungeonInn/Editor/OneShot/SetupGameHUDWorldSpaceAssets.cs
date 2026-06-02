using System;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditor.SceneManagement;
using UnityEngine;
using VContainer.Unity;
using DungeonInn.View.Scene.ModuleScene.GameHUD;
using Object = UnityEngine.Object;

namespace DungeonInn.Editor.OneShot
{
    public static class SetupGameHUDWorldSpaceAssets
    {
        const string GameHUDScenePath = "Assets/DungeonInn/Runtime/Scene/ModuleScene/GameHUD.unity";
        const string GameHUDPrefabDirectory = "Assets/DungeonInn/Runtime/Prefab/GameHUD";
        const string ActorStatusViewPrefabPath = GameHUDPrefabDirectory + "/ActorStatusView.prefab";
        const string DamageNumberViewPrefabPath = GameHUDPrefabDirectory + "/DamageNumberView.prefab";
        const string BaseSpritePath = "Assets/DungeonInn/Runtime/StaticResources/Scene/Common/Sprite/BaseSprite.png";
        const string HpBarShaderPath = "Assets/DungeonInn/Runtime/Shaders/GameHUD/HpBar.shader";
        const string SpriteOverlayShaderPath = "Assets/DungeonInn/Runtime/Shaders/GameHUD/SpriteOverlay.shader";
        const string HpBarMaterialPath = "Assets/DungeonInn/Runtime/Art/Materials/GameHUD/HpBar.mat";
        const string SpriteOverlayMaterialPath = "Assets/DungeonInn/Runtime/Art/Materials/GameHUD/SpriteOverlay.mat";
        const string EffectDummySpritePath = "Assets/DungeonInn/Runtime/Art/Sprites/Effect/Dummy.png";
        const string DamageDigitAtlasPath = "Assets/DungeonInn/Runtime/Art/Sprites/UI/DamageDigits.png";
        const string RenderingLayerName = "World";
        const string WorldSortingLayerName = "World";
        const string WorldEffectSortingLayerName = "WorldEffect";
        const string GameHUDSortingLayerName = "GameHUD";
        const string GameHUDOverlaySortingLayerName = "GameHUDOverlay";
        const int DamageDigitCount = 10;
        const int DamageDigitWidthPixels = 30;
        const int DamageDigitHeightPixels = 50;
        const float DamageDigitPixelsPerUnit = 100f;
        const int ActorStatusHpSortingOrder = 100;
        const int ActorStatusIconSortingOrder = 110;
        const int DamageNumberSortingOrder = 200;

        public static void Run()
        {
            EnsureSortingLayers();
            EnsureDamageDigitAtlasImporter();
            EnsureGameHUDScene();
            EnsureActorStatusViewPrefab();
            EnsureDamageNumberViewPrefab();
            VisualAssetSetup.RunAddressablesSetup();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DungeonInn] GameHUD world-space scene and prefabs updated.");
        }

        static void EnsureGameHUDScene()
        {
            var scene = EditorSceneManager.OpenScene(GameHUDScenePath, OpenSceneMode.Single);
            var root = GameObject.Find("GameHUD");
            if (root == null)
            {
                root = new GameObject("GameHUD");
            }

            var hudRoot = root.transform.Find("HudRoot");
            if (hudRoot == null)
            {
                hudRoot = new GameObject("HudRoot").transform;
                hudRoot.SetParent(root.transform, false);
            }

            var moduleScene = root.GetComponent<GameHUDModuleScene>();
            if (moduleScene == null)
            {
                moduleScene = root.AddComponent<GameHUDModuleScene>();
            }

            if (root.GetComponent<GameHUDLifetimeScope>() == null)
            {
                root.AddComponent<GameHUDLifetimeScope>();
            }

            var iconSpriteCatalog = root.GetComponent<ActorEffectIconSpriteCatalog>();
            if (iconSpriteCatalog == null)
            {
                iconSpriteCatalog = root.AddComponent<ActorEffectIconSpriteCatalog>();
            }

            var effectIconSprite = LoadSprite(EffectDummySpritePath);
            iconSpriteCatalog.EditorAssign(new[]
            {
                new ActorEffectIconSpriteCatalog.Entry(1, effectIconSprite),
                new ActorEffectIconSpriteCatalog.Entry(2, effectIconSprite)
            });

            using var serialized = new SerializedObject(moduleScene);
            serialized.FindProperty("hudRoot").objectReferenceValue = hudRoot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ApplyRenderingLayerToHierarchy(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void EnsureActorStatusViewPrefab()
        {
            EnsureDirectory(GameHUDPrefabDirectory);
            var hpBarSprite = LoadSprite(BaseSpritePath);
            var hpBarMaterial = EnsureHpBarMaterial();
            var spriteOverlayMaterial = EnsureSpriteOverlayMaterial();
            var effectIconSprite = LoadSprite(EffectDummySpritePath);
            var viewObject = new GameObject("ActorStatusView");
            var view = viewObject.AddComponent<ActorStatusView>();
            var billboardRoot = new GameObject("BillboardRoot").transform;
            billboardRoot.SetParent(viewObject.transform, false);

            var hpBar = CreateRenderer("HpBar", billboardRoot, hpBarSprite, Color.white, new Vector3(0f, 0f, 0f));
            hpBar.drawMode = SpriteDrawMode.Sliced;
            hpBar.size = new Vector2(1.2f, 0.28f);
            hpBar.sharedMaterial = hpBarMaterial;
            hpBar.sortingLayerName = GameHUDSortingLayerName;
            hpBar.sortingOrder = ActorStatusHpSortingOrder;

            var statusIcons = new SpriteRenderer[4];
            for (var index = 0; index < statusIcons.Length; index++)
            {
                statusIcons[index] = CreateRenderer(
                    $"StatusIcon{index + 1}",
                    billboardRoot,
                    effectIconSprite,
                    Color.white,
                    new Vector3(-0.27f + index * 0.18f, 0.18f, 0f));
                statusIcons[index].transform.localScale = Vector3.one * 0.12f;
                statusIcons[index].drawMode = SpriteDrawMode.Sliced;
                statusIcons[index].size = Vector2.one * 0.12f;
                statusIcons[index].sharedMaterial = spriteOverlayMaterial;
                statusIcons[index].sortingLayerName = GameHUDSortingLayerName;
                statusIcons[index].sortingOrder = ActorStatusIconSortingOrder;
                statusIcons[index].gameObject.SetActive(false);
            }

            view.EditorAssign(billboardRoot, hpBar, statusIcons);
            ApplyRenderingLayerToHierarchy(viewObject);
            PrefabUtility.SaveAsPrefabAsset(viewObject, ActorStatusViewPrefabPath);
            Object.DestroyImmediate(viewObject);
            EnsureActorStatusPrefabRendererAssets(
                ActorStatusViewPrefabPath,
                hpBarSprite,
                hpBarMaterial,
                effectIconSprite,
                spriteOverlayMaterial);
            EnsurePrefabLayer(ActorStatusViewPrefabPath);
        }

        static void EnsureDamageNumberViewPrefab()
        {
            EnsureDirectory(GameHUDPrefabDirectory);
            var sprites = LoadDamageDigitSprites();
            var spriteOverlayMaterial = EnsureSpriteOverlayMaterial();
            var viewObject = new GameObject("DamageNumberView");
            var view = viewObject.AddComponent<DamageNumberView>();
            var digitRoot = new GameObject("DigitRoot").transform;
            digitRoot.SetParent(viewObject.transform, false);
            var digitTemplate = CreateRenderer("DigitTemplate", digitRoot, sprites[0], Color.white, Vector3.zero);
            digitTemplate.sharedMaterial = spriteOverlayMaterial;
            digitTemplate.sortingLayerName = GameHUDOverlaySortingLayerName;
            digitTemplate.sortingOrder = DamageNumberSortingOrder;
            digitTemplate.gameObject.SetActive(false);
            view.EditorAssign(digitRoot, digitTemplate, sprites);
            ApplyRenderingLayerToHierarchy(viewObject);
            PrefabUtility.SaveAsPrefabAsset(viewObject, DamageNumberViewPrefabPath);
            Object.DestroyImmediate(viewObject);
            EnsureDamageNumberPrefabRendererAssets(DamageNumberViewPrefabPath, sprites[0], spriteOverlayMaterial);
            EnsurePrefabLayer(DamageNumberViewPrefabPath);
        }

        static SpriteRenderer CreateRenderer(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            Vector3 localPosition)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 100;
            return renderer;
        }

        static void EnsureSortingLayers()
        {
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            using var serialized = new SerializedObject(tagManager);
            var sortingLayers = serialized.FindProperty("m_SortingLayers");
            EnsureSortingLayer(sortingLayers, WorldSortingLayerName, 10001);
            EnsureSortingLayer(sortingLayers, WorldEffectSortingLayerName, 10002);
            EnsureSortingLayer(sortingLayers, GameHUDSortingLayerName, 10003);
            EnsureSortingLayer(sortingLayers, GameHUDOverlaySortingLayerName, 10004);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureSortingLayer(SerializedProperty sortingLayers, string layerName, int uniqueId)
        {
            for (var index = 0; index < sortingLayers.arraySize; index++)
            {
                var layer = sortingLayers.GetArrayElementAtIndex(index);
                if (layer.FindPropertyRelative("name").stringValue == layerName)
                {
                    return;
                }
            }

            sortingLayers.InsertArrayElementAtIndex(sortingLayers.arraySize);
            var newLayer = sortingLayers.GetArrayElementAtIndex(sortingLayers.arraySize - 1);
            newLayer.FindPropertyRelative("name").stringValue = layerName;
            newLayer.FindPropertyRelative("uniqueID").intValue = uniqueId;
            newLayer.FindPropertyRelative("locked").boolValue = false;
        }

        static Material EnsureHpBarMaterial()
        {
            EnsureDirectory("Assets/DungeonInn/Runtime/Art/Materials/GameHUD");
            var material = AssetDatabase.LoadAssetAtPath<Material>(HpBarMaterialPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(HpBarShaderPath);
            if (shader == null)
            {
                Debug.LogWarning($"[DungeonInn] HP bar shader is missing. Path={HpBarShaderPath}");
                return material;
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, HpBarMaterialPath);
            }

            material.shader = shader;
            material.SetColor("_FillColor", new Color(0.1f, 0.9f, 0.25f, 1f));
            material.SetColor("_BackgroundColor", new Color(0.06f, 0.08f, 0.07f, 0.92f));
            material.SetColor("_BorderColor", new Color(0f, 0f, 0f, 1f));
            material.SetColor("_HighlightColor", new Color(0.58f, 1f, 0.55f, 1f));
            material.SetVector("_ReferenceSizePixels", new Vector4(120f, 28f, 0f, 0f));
            material.SetFloat("_VerticalMarginPixels", 10f);
            material.SetFloat("_BorderPixels", 1f);
            material.SetFloat("_HighlightPixels", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material EnsureSpriteOverlayMaterial()
        {
            EnsureDirectory("Assets/DungeonInn/Runtime/Art/Materials/GameHUD");
            var material = AssetDatabase.LoadAssetAtPath<Material>(SpriteOverlayMaterialPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(SpriteOverlayShaderPath);
            if (shader == null)
            {
                Debug.LogWarning($"[DungeonInn] Sprite overlay shader is missing. Path={SpriteOverlayShaderPath}");
                return material;
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, SpriteOverlayMaterialPath);
            }

            material.shader = shader;
            material.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void EnsureActorStatusPrefabRendererAssets(
            string prefabPath,
            Sprite hpBarSprite,
            Material hpBarMaterial,
            Sprite effectIconSprite,
            Material spriteOverlayMaterial)
        {
            if (hpBarSprite == null || hpBarMaterial == null || spriteOverlayMaterial == null)
            {
                Debug.LogWarning($"[DungeonInn] HP bar assets are missing for GameHUD prefab setup. Prefab={prefabPath}");
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            foreach (var renderer in prefabRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.name == "HpBar")
                {
                    renderer.sprite = hpBarSprite;
                    renderer.sharedMaterial = hpBarMaterial;
                    renderer.sortingLayerName = GameHUDSortingLayerName;
                    renderer.sortingOrder = ActorStatusHpSortingOrder;
                }
                else
                {
                    renderer.sprite = effectIconSprite;
                    renderer.sharedMaterial = spriteOverlayMaterial;
                    renderer.sortingLayerName = GameHUDSortingLayerName;
                    renderer.sortingOrder = ActorStatusIconSortingOrder;
                }

                EditorUtility.SetDirty(renderer);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        static void EnsureDamageNumberPrefabRendererAssets(
            string prefabPath,
            Sprite templateSprite,
            Material spriteOverlayMaterial)
        {
            if (templateSprite == null || spriteOverlayMaterial == null)
            {
                Debug.LogWarning($"[DungeonInn] Damage number assets are missing for GameHUD prefab setup. Prefab={prefabPath}");
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            foreach (var renderer in prefabRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sprite = templateSprite;
                renderer.sharedMaterial = spriteOverlayMaterial;
                renderer.sortingLayerName = GameHUDOverlaySortingLayerName;
                renderer.sortingOrder = DamageNumberSortingOrder;
                EditorUtility.SetDirty(renderer);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        static void EnsurePrefabLayer(string prefabPath)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            ApplyRenderingLayerToHierarchy(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        static void ApplyRenderingLayerToHierarchy(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var layer = LayerMask.NameToLayer(RenderingLayerName);
            if (layer < 0)
            {
                layer = 0;
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }

        static void EnsureDamageDigitAtlasImporter()
        {
            var importer = AssetImporter.GetAtPath(DamageDigitAtlasPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"[DungeonInn] Damage digit atlas is missing. Path={DamageDigitAtlasPath}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = DamageDigitPixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();

            var providerFactories = new SpriteDataProviderFactories();
            providerFactories.Init();
            var dataProvider = providerFactories.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            var rects = new SpriteRect[DamageDigitCount];
            var nameFileIdPairs = new SpriteNameFileIdPair[DamageDigitCount];
            for (var digit = 0; digit < DamageDigitCount; digit++)
            {
                var name = $"DamageDigit_{digit}";
                var spriteId = GUID.Generate();
                rects[digit] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(
                        digit * DamageDigitWidthPixels,
                        0f,
                        DamageDigitWidthPixels,
                        DamageDigitHeightPixels),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = spriteId
                };
                nameFileIdPairs[digit] = new SpriteNameFileIdPair(name, spriteId);
            }

            dataProvider.SetSpriteRects(rects);
            dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                .SetNameFileIdPairs(nameFileIdPairs);
            dataProvider.Apply();
            importer.SaveAndReimport();
        }

        static Sprite[] LoadDamageDigitSprites()
        {
            EnsureDamageDigitAtlasImporter();
            var assets = AssetDatabase.LoadAllAssetsAtPath(DamageDigitAtlasPath);
            var digitSprites = new Sprite[DamageDigitCount];
            foreach (var asset in assets)
            {
                if (asset is not Sprite sprite)
                {
                    continue;
                }

                if (!TryParseDamageDigitIndex(sprite.name, out var digit))
                {
                    continue;
                }

                digitSprites[digit] = sprite;
            }

            EnsureDamageDigitSpriteReferences(digitSprites);
            return digitSprites;
        }

        static bool TryParseDamageDigitIndex(string name, out int digit)
        {
            const string Prefix = "DamageDigit_";
            digit = -1;
            if (string.IsNullOrEmpty(name) || !name.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            if (!int.TryParse(name.Substring(Prefix.Length), out var parsedDigit))
            {
                return false;
            }

            if (parsedDigit < 0 || DamageDigitCount <= parsedDigit)
            {
                return false;
            }

            digit = parsedDigit;
            return true;
        }

        static void EnsureDamageDigitSpriteReferences(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length != DamageDigitCount)
            {
                throw new InvalidOperationException("[DungeonInn] Damage digit sprites must contain 10 entries.");
            }

            for (var digit = 0; digit < DamageDigitCount; digit++)
            {
                if (sprites[digit] == null)
                {
                    throw new InvalidOperationException($"[DungeonInn] Damage digit sprite is missing. Digit={digit} Path={DamageDigitAtlasPath}");
                }
            }
        }

        static Sprite LoadSprite(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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

