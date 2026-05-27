using System;
using System.Collections.Generic;
using System.IO;
using DungeonInn.Application.SaveLoad;
using UnityEngine;
using VContainer;

namespace DungeonInn.Infrastructure.SaveLoad
{
    public sealed class JsonGameSaveRepository : IGameSaveRepository
    {
        const int SaveVersion = 1;
        const int FixedSlotCount = 3;
        const string SaveDirectoryName = "DungeonInnSaves";

        readonly string saveDirectoryPath;

        public int SlotCount => FixedSlotCount;

        [Inject]
        public JsonGameSaveRepository()
        {
            saveDirectoryPath = Path.Combine(UnityEngine.Application.persistentDataPath, SaveDirectoryName);
        }

        public void Save(int slotId, GameSaveData saveData)
        {
            ValidateSlotId(slotId);
            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            Directory.CreateDirectory(saveDirectoryPath);
            for (var candidateSlotId = 1; candidateSlotId <= FixedSlotCount; candidateSlotId++)
            {
                if (candidateSlotId == slotId || !TryLoad(candidateSlotId, out var existingSaveData))
                {
                    continue;
                }

                existingSaveData.metadata.isLatest = false;
                Write(candidateSlotId, existingSaveData);
            }

            saveData.version = SaveVersion;
            saveData.metadata.slotId = slotId;
            saveData.metadata.isLatest = true;
            Write(slotId, saveData);
        }

        public bool TryLoad(int slotId, out GameSaveData saveData)
        {
            ValidateSlotId(slotId);
            var filePath = GetSlotFilePath(slotId);
            if (!File.Exists(filePath))
            {
                saveData = null;
                return false;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                saveData = JsonUtility.FromJson<GameSaveData>(json);
                return saveData != null && saveData.metadata != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveLoad] Failed to read save slot {slotId}: {exception.Message}");
                saveData = null;
                return false;
            }
        }

        public IReadOnlyList<GameSaveSlotSummary> GetSlotSummaries()
        {
            var summaries = new List<GameSaveSlotSummary>(FixedSlotCount);
            for (var slotId = 1; slotId <= FixedSlotCount; slotId++)
            {
                summaries.Add(CreateSummary(slotId));
            }

            return summaries;
        }

        public bool TryGetLatestSlot(out GameSaveSlotSummary summary)
        {
            summary = null;
            foreach (var candidate in GetSlotSummaries())
            {
                if (candidate.IsEmpty)
                {
                    continue;
                }

                if (candidate.IsLatest)
                {
                    summary = candidate;
                    return true;
                }

                if (summary == null ||
                    string.CompareOrdinal(summary.SavedAtUtc, candidate.SavedAtUtc) < 0)
                {
                    summary = candidate;
                }
            }

            return summary != null;
        }

        GameSaveSlotSummary CreateSummary(int slotId)
        {
            if (!TryLoad(slotId, out var saveData))
            {
                return GameSaveSlotSummary.Empty(slotId);
            }

            return new GameSaveSlotSummary(
                slotId,
                false,
                saveData.metadata.isLatest,
                saveData.metadata.savedAtUtc ?? string.Empty,
                saveData.metadata.displayDay,
                saveData.metadata.seed);
        }

        void Write(int slotId, GameSaveData saveData)
        {
            var json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(GetSlotFilePath(slotId), json);
        }

        string GetSlotFilePath(int slotId)
        {
            return Path.Combine(saveDirectoryPath, $"slot{slotId}.json");
        }

        static void ValidateSlotId(int slotId)
        {
            if (slotId < 1 || FixedSlotCount < slotId)
            {
                throw new ArgumentOutOfRangeException(nameof(slotId));
            }
        }
    }
}
