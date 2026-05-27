using System;
using System.Collections.Generic;

namespace DungeonInn.Application.SaveLoad
{
    public sealed class TutorialProgressService
    {
        readonly HashSet<string> seenStepIds = new();

        public TutorialSaveData CreateSaveData()
        {
            var stepIds = new string[seenStepIds.Count];
            seenStepIds.CopyTo(stepIds);
            return new TutorialSaveData
            {
                seenStepIds = stepIds
            };
        }

        public void Restore(TutorialSaveData saveData)
        {
            seenStepIds.Clear();
            if (saveData?.seenStepIds == null)
            {
                return;
            }

            foreach (var stepId in saveData.seenStepIds)
            {
                if (!string.IsNullOrWhiteSpace(stepId))
                {
                    seenStepIds.Add(stepId);
                }
            }
        }

        public bool HasSeen(string stepId)
        {
            return seenStepIds.Contains(stepId ?? throw new ArgumentNullException(nameof(stepId)));
        }

        public void MarkSeen(string stepId)
        {
            seenStepIds.Add(stepId ?? throw new ArgumentNullException(nameof(stepId)));
        }
    }
}
