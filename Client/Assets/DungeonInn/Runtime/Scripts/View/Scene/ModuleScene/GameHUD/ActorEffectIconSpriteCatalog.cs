using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using UnityEngine;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class ActorEffectIconSpriteCatalog : MonoBehaviour
    {
        [SerializeField] Entry[] entries;

        readonly HashSet<int> missingLoggedActorEffectMasterIds = new();

        public bool TryGetSprite(ActorEffectIconViewData effect, out Sprite sprite)
        {
            if (entries != null)
            {
                for (var index = 0; index < entries.Length; index++)
                {
                    var entry = entries[index];
                    if (entry != null &&
                        entry.ActorEffectMasterId == effect.ActorEffectMasterId &&
                        entry.Sprite != null)
                    {
                        sprite = entry.Sprite;
                        return true;
                    }
                }
            }

            if (missingLoggedActorEffectMasterIds.Add(effect.ActorEffectMasterId))
            {
                Debug.LogWarning(
                    $"[ActorEffectIconSpriteCatalog] Missing icon sprite. ActorEffectMasterId={effect.ActorEffectMasterId}");
            }

            sprite = null;
            return false;
        }

#if UNITY_EDITOR
        public void EditorAssign(Entry[] newEntries)
        {
            entries = newEntries;
        }
#endif

        [Serializable]
        public sealed class Entry
        {
            [SerializeField] int actorEffectMasterId;
            [SerializeField] Sprite sprite;

            public Entry(int actorEffectMasterId, Sprite sprite)
            {
                this.actorEffectMasterId = actorEffectMasterId;
                this.sprite = sprite;
            }

            public int ActorEffectMasterId => actorEffectMasterId;
            public Sprite Sprite => sprite;
        }
    }
}
