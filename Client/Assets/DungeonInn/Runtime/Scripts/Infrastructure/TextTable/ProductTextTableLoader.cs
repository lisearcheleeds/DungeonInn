using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using LighthouseExtends.Font;
using LighthouseExtends.TextTable;
using UnityEngine;
using VContainer;

namespace DungeonInn.Infrastructure.TextTable
{
    public sealed class ProductTextTableLoader : ITextTableLoader
    {
        const string TsvSubFolder = "TextTables";

        readonly IFontService fontService;

        [Inject]
        public ProductTextTableLoader(IFontService fontService)
        {
            this.fontService = fontService ?? throw new ArgumentNullException(nameof(fontService));
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        UniTask<IReadOnlyDictionary<string, string>> ITextTableLoader.LoadAsync(string languageCode, CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, string>();
            return UniTask.FromResult<IReadOnlyDictionary<string, string>>(result);
        }
#else
        UniTask<IReadOnlyDictionary<string, string>> ITextTableLoader.LoadAsync(string languageCode, CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, string>();
            var folderPath = Path.Combine(UnityEngine.Application.streamingAssetsPath, TsvSubFolder);

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
                catch (Exception exception)
                {
                    Debug.LogError($"[TextTable] Failed to load TSV file: '{filePath}'\n{exception}");
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
                foreach (var ch in value)
                {
                    if (seen.Add(ch))
                    {
                        uniqueChars.Append(ch);
                    }
                }
            }

            fontAsset.TryAddCharacters(uniqueChars.ToString());
        }

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
