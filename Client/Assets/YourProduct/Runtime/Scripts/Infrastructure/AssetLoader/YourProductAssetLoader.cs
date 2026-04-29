using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LighthouseExtends.Font;
using LighthouseExtends.ScreenStack;
using LighthouseExtends.TextTable;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace YourProduct.Infrastructure.AssetLoader
{
    public sealed class YourProductAssetLoader : IScreenStackInstanceFactory, ITextTableLoader
    {
        const string TsvSubFolder = "TextTables";

        readonly IObjectResolver objectResolver;
        readonly IFontService fontService;

        [Inject]
        public YourProductAssetLoader(IObjectResolver objectResolver, IFontService fontService)
        {
            this.objectResolver = objectResolver;
            this.fontService = fontService;
        }

        async UniTask<TScreenStack> IScreenStackInstanceFactory.CreateScreenStackInstance<TScreenStack>(string screenStackAddress, IScreenStackData data, CancellationToken ct)
        {
            var request = Resources.LoadAsync<GameObject>(screenStackAddress);
            await request.ToUniTask(cancellationToken: ct);
            var prefab = request.asset as GameObject;
            var gameObject = objectResolver.Instantiate(prefab);
            return gameObject.GetComponents<MonoBehaviour>().OfType<TScreenStack>().First();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        async UniTask<IReadOnlyDictionary<string, string>> ITextTableLoader.LoadAsync(string languageCode, CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, string>();
            var folderUrl = $"{Application.streamingAssetsPath}/{TsvSubFolder}";

            // Load domain list to enumerate TSV files (Directory.GetFiles is unavailable on WebGL)
            var manifestUrl = $"{folderUrl}/TextTableDomains.txt";
            string manifestContent;
            using (var request = UnityWebRequest.Get(manifestUrl))
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[TextTable] Failed to load manifest: '{manifestUrl}'\n{request.error}");
                    return result;
                }
                manifestContent = request.downloadHandler.text;
            }

            var domains = manifestContent.Split('\n')
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line))
                .ToArray();

            var tasks = domains.Select(domain => FetchTsvAsync($"{folderUrl}/{domain}.{languageCode}.tsv", cancellationToken));
            var contents = await UniTask.WhenAll(tasks);

            for (var i = 0; i < domains.Length; i++)
            {
                if (contents[i] == null)
                {
                    continue;
                }

                var fileName = $"{domains[i]}.{languageCode}.tsv";
                try
                {
                    ParseTsv(contents[i], fileName, languageCode, result);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TextTable] Failed to parse TSV file: '{fileName}'\n{e}");
                }
            }

            if (result.Count == 0)
            {
                Debug.LogWarning($"[TextTable] No entries loaded for language '{languageCode}'. Folder: '{folderUrl}'");
            }

            PrewarmFontAtlas(languageCode, result);
            return result;
        }
#else
        UniTask<IReadOnlyDictionary<string, string>> ITextTableLoader.LoadAsync(string languageCode, CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, string>();
            var folderPath = Path.Combine(Application.streamingAssetsPath, TsvSubFolder);

            if (!Directory.Exists(folderPath))
            {
                Debug.LogError($"[TextTable] TSV folder not found: '{folderPath}'");
                return UniTask.FromResult<IReadOnlyDictionary<string, string>>(result);
            }

            foreach (var filePath in Directory.GetFiles(folderPath, "*.tsv"))
            {
                // Expected filename format: {domain}.{language}.tsv (e.g., "SceneHome.ja.tsv")
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                var dotIndex = fileNameWithoutExt.LastIndexOf('.');
                if (dotIndex < 0)
                {
                    continue;
                }

                var language = fileNameWithoutExt.Substring(dotIndex + 1);
                if (language != languageCode)
                {
                    continue;
                }

                try
                {
                    var content = File.ReadAllText(filePath);
                    ParseTsv(content, fileNameWithoutExt, languageCode, result);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TextTable] Failed to load TSV file: '{filePath}'\n{e}");
                }
            }

            if (result.Count == 0)
            {
                Debug.LogWarning($"[TextTable] No entries loaded for language '{languageCode}'. Folder: '{folderPath}'");
            }

            PrewarmFontAtlas(languageCode, result);
            return UniTask.FromResult<IReadOnlyDictionary<string, string>>(result);
        }
#endif

        void PrewarmFontAtlas(string languageCode, IReadOnlyDictionary<string, string> table)
        {
            var fontAsset = fontService.GetFont(languageCode);
            if (fontAsset == null)
            {
                return;
            }

            var uniqueChars = new System.Text.StringBuilder();
            var seen = new HashSet<char>();
            foreach (var value in table.Values)
            {
                foreach (var c in value)
                {
                    if (seen.Add(c))
                    {
                        uniqueChars.Append(c);
                    }
                }
            }

            fontAsset.TryAddCharacters(uniqueChars.ToString());
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        static async UniTask<string> FetchTsvAsync(string url, CancellationToken cancellationToken)
        {
            using var request = UnityWebRequest.Get(url);
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            return request.result == UnityWebRequest.Result.Success ? request.downloadHandler.text : null;
        }
#endif

        static void ParseTsv(string content, string assetName, string languageCode, Dictionary<string, string> table)
        {
            var lines = content.Split('\n');
            var isHeader = true;

            foreach (var line in lines)
            {
                if (isHeader)
                {
                    isHeader = false;
                    continue;
                }

                var trimmed = line.TrimEnd('\r');
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                var tabIndex = trimmed.IndexOf('\t');
                if (tabIndex < 0)
                {
                    continue;
                }

                var key = trimmed.Substring(0, tabIndex).Trim('\0', '\uFEFF');
                var text = trimmed.Substring(tabIndex + 1).Trim('\0', '\uFEFF');

                if (table.ContainsKey(key))
                {
#if DEBUG
                    throw new InvalidOperationException(
                        $"[TextTable] Duplicate key '{key}' found in '{assetName}' (language: {languageCode}).");
#else
                    table[key] = text;
#endif
                }
                else
                {
                    table[key] = text;
                }
            }
        }
    }
}
