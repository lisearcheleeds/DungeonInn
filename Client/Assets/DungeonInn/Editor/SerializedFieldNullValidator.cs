using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonInn.Editor
{
    public static class SerializedFieldNullValidator
    {
        const string MenuPath = "DungeonInn/Validate SerializedFields";
        const string NamespacePrefix = "DungeonInn";
        internal const string PrefabSearchFolder = "Assets/DungeonInn";

        [MenuItem(MenuPath)]
        public static void Validate()
        {
            var errors = new List<string>();
            RunValidation(errors);

            if (errors.Count == 0)
            {
                Debug.Log("[SerializedFieldValidator] OK: All SerializedFields are assigned.");
                EditorUtility.DisplayDialog("Validate SerializedFields", "All SerializedFields are assigned.", "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "Validate SerializedFields",
                $"{errors.Count} unassigned SerializedField(s) found. See Console for details.",
                "OK");
        }

        internal static void RunValidation(List<string> errors)
        {
            ValidateAllScenes(errors);
            ValidateProjectPrefabs(errors);
        }

        static void ValidateAllScenes(List<string> errors)
        {
            var openScenePaths = CollectOpenScenePaths();

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    ValidateGameObject(root, $"Scene:{scene.name}", errors);
                }
            }

            var guids = AssetDatabase.FindAssets("t:Scene", new[] { PrefabSearchFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (openScenePaths.Contains(path))
                {
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                foreach (var root in scene.GetRootGameObjects())
                {
                    ValidateGameObject(root, $"Scene:{scene.name}", errors);
                }

                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static HashSet<string> CollectOpenScenePaths()
        {
            var paths = new HashSet<string>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                paths.Add(SceneManager.GetSceneAt(i).path);
            }

            return paths;
        }

        static void ValidateProjectPrefabs(List<string> errors)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabSearchFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                ValidateGameObject(prefab, $"Prefab:{path}", errors);
            }
        }

        static void ValidateGameObject(GameObject go, string context, List<string> errors)
        {
            foreach (var component in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null)
                {
                    continue;
                }

                var type = component.GetType();
                if (!type.FullName.StartsWith(NamespacePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                ValidateComponent(component, type, context, errors);
            }
        }

        static void ValidateComponent(MonoBehaviour component, Type type, string context, List<string> errors)
        {
            foreach (var field in CollectSerializedReferenceFields(type))
            {
                var value = field.GetValue(component);
                if (!IsNullOrFakeNull(value))
                {
                    continue;
                }

                var message =
                    $"[SerializedFieldValidator] {context} / {component.gameObject.name} / {type.Name}.{field.Name} is not assigned";
                Debug.LogError(message, component);
                errors.Add(message);
            }
        }

        static IEnumerable<FieldInfo> CollectSerializedReferenceFields(Type type)
        {
            var current = type;
            while (current != null && current != typeof(MonoBehaviour) && current != typeof(object))
            {
                foreach (var field in current.GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (field.GetCustomAttribute<SerializeField>() == null)
                    {
                        continue;
                    }

                    if (field.FieldType.IsValueType)
                    {
                        continue;
                    }

                    if (field.FieldType == typeof(string))
                    {
                        continue;
                    }

                    yield return field;
                }

                current = current.BaseType;
            }
        }

        static bool IsNullOrFakeNull(object value)
        {
            if (value == null)
            {
                return true;
            }

            if (value is UnityEngine.Object unityObject)
            {
                return !unityObject;
            }

            return false;
        }
    }
}
