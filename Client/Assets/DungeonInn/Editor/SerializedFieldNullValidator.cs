using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonInn.Editor
{
    public static class SerializedFieldNullValidator
    {
        const string MenuPath = "DungeonInn/Validate SerializedFields";
        const string NamespacePrefix = "DungeonInn";
        const string PrefabSearchFolder = "Assets/DungeonInn";

        [MenuItem(MenuPath)]
        public static void Validate()
        {
            var errorCount = 0;
            errorCount += ValidateOpenScenes();
            errorCount += ValidateProjectPrefabs();

            if (errorCount == 0)
            {
                Debug.Log("[SerializedFieldValidator] OK: All SerializedFields are assigned.");
                EditorUtility.DisplayDialog("Validate SerializedFields", "All SerializedFields are assigned.", "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "Validate SerializedFields",
                $"{errorCount} unassigned SerializedField(s) found. See Console for details.",
                "OK");
        }

        static int ValidateOpenScenes()
        {
            var errorCount = 0;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    errorCount += ValidateGameObject(root, $"Scene:{scene.name}");
                }
            }

            return errorCount;
        }

        static int ValidateProjectPrefabs()
        {
            var errorCount = 0;
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabSearchFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                errorCount += ValidateGameObject(prefab, $"Prefab:{path}");
            }

            return errorCount;
        }

        static int ValidateGameObject(GameObject go, string context)
        {
            var errorCount = 0;
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

                errorCount += ValidateComponent(component, type, context);
            }

            return errorCount;
        }

        static int ValidateComponent(MonoBehaviour component, Type type, string context)
        {
            var errorCount = 0;
            foreach (var field in CollectSerializedReferenceFields(type))
            {
                var value = field.GetValue(component);
                if (!IsNullOrFakeNull(value))
                {
                    continue;
                }

                Debug.LogError(
                    $"[SerializedFieldValidator] {context} / {component.gameObject.name} / {type.Name}.{field.Name} is not assigned",
                    component);
                errorCount++;
            }

            return errorCount;
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
