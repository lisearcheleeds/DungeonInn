using System;
using System.Collections.Generic;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.World
{
    public sealed class GetSelectedActorInspectorQuery
    {
        static readonly EquipmentSlot[] AllEquipmentSlots =
        {
            EquipmentSlot.Weapon,
            EquipmentSlot.Armor,
            EquipmentSlot.Accessory
        };

        readonly IGameWorldStateReader worldState;
        readonly IMasterRepository masterRepository;
        readonly IActorProfileRegistry actorProfileRegistry;
        readonly List<SelectedActorInspectorStatLineViewData> statBuffer = new();
        readonly List<SelectedActorInspectorEquipmentLineViewData> equipmentBuffer = new();
        readonly List<SelectedActorInspectorItemViewData> inventoryBuffer = new();
        readonly List<SelectedActorInspectorEffectLineViewData> effectBuffer = new();

        [Inject]
        public GetSelectedActorInspectorQuery(
            IGameWorldStateReader worldState,
            IMasterRepository masterRepository,
            IActorProfileRegistry actorProfileRegistry)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.actorProfileRegistry = actorProfileRegistry ?? throw new ArgumentNullException(nameof(actorProfileRegistry));
        }

        public SelectedActorInspectorViewData Query(Guid actorId)
        {
            var actor = worldState.FindActor(actorId);
            if (actor == null)
            {
                return null;
            }

            actorProfileRegistry.TryGetProfile(actorId, out var profile);
            var archetype = ResolveArchetype(actor, profile);
            var species = ResolveSpecies(actor, archetype, profile);
            FillStats(actor);
            FillEquipment(actor);
            FillInventory(actor);
            FillEffects(actor);

            return new SelectedActorInspectorViewData(
                actor.Id,
                ResolveDisplayName(actor, profile),
                ResolveActorKindText(actor, archetype, profile),
                ResolveRoleText(archetype, species),
                ResolveLifecycleText(actor),
                FormatGoal(actor.CurrentGoal),
                actor.Level,
                actor.Hp,
                actor.Params.MaxHp,
                actor.Mp,
                actor.Params.MaxMp,
                actor.Fatigue,
                actor.InjurySeverity,
                actor.Inventory.Gold,
                statBuffer,
                equipmentBuffer,
                inventoryBuffer,
                effectBuffer);
        }

        void FillStats(Actor actor)
        {
            statBuffer.Clear();
            statBuffer.Add(new SelectedActorInspectorStatLineViewData("STR", actor.Stats.Strength));
            statBuffer.Add(new SelectedActorInspectorStatLineViewData("DEX", actor.Stats.Dexterity));
            statBuffer.Add(new SelectedActorInspectorStatLineViewData("CON", actor.Stats.Constitution));
            statBuffer.Add(new SelectedActorInspectorStatLineViewData("INT", actor.Stats.Intelligence));
            statBuffer.Add(new SelectedActorInspectorStatLineViewData("WIS", actor.Stats.Wisdom));
            statBuffer.Add(new SelectedActorInspectorStatLineViewData("CHA", actor.Stats.Charisma));
        }

        void FillEquipment(Actor actor)
        {
            equipmentBuffer.Clear();
            foreach (var slot in AllEquipmentSlots)
            {
                var itemId = actor.Equipment.GetEquippedItemId(slot);
                var itemName = itemId.HasValue ? ResolveItemName(itemId.Value) : "-";
                equipmentBuffer.Add(new SelectedActorInspectorEquipmentLineViewData(slot.ToString(), itemName));
            }
        }

        void FillInventory(Actor actor)
        {
            inventoryBuffer.Clear();
            foreach (var kvp in actor.Inventory.ItemCounts)
            {
                if (kvp.Key == SpecialItemIds.Money || kvp.Value <= 0)
                {
                    continue;
                }

                inventoryBuffer.Add(new SelectedActorInspectorItemViewData(kvp.Key, ResolveItemName(kvp.Key), kvp.Value));
            }
        }

        void FillEffects(Actor actor)
        {
            effectBuffer.Clear();
            foreach (var effect in actor.ActorEffects)
            {
                if (effect.IsExpired)
                {
                    continue;
                }

                var displayName = masterRepository.ActorEffectMasters.TryGetValue(
                    effect.ActorEffectMasterId,
                    out var effectMaster)
                    ? effectMaster.Name
                    : $"Effect {effect.ActorEffectMasterId}";
                var remainingSeconds = effect.DurationSeconds - effect.ElapsedSeconds;
                effectBuffer.Add(new SelectedActorInspectorEffectLineViewData(
                    effect.ActorEffectMasterId,
                    displayName,
                    remainingSeconds));
            }
        }

        ActorArchetypeMaster ResolveArchetype(Actor actor, ActorProfile profile)
        {
            var archetypeId = 0 < actor.ArchetypeId ? actor.ArchetypeId : profile?.ArchetypeId ?? 0;
            return 0 < archetypeId && masterRepository.ActorArchetypeMasters.TryGetValue(archetypeId, out var archetype)
                ? archetype
                : null;
        }

        SpeciesMaster ResolveSpecies(Actor actor, ActorArchetypeMaster archetype, ActorProfile profile)
        {
            var speciesId = archetype?.SpeciesId ?? profile?.SpeciesId ?? 0;
            if (speciesId <= 0 && actor.Behavior is MonsterBehavior monsterBehavior)
            {
                speciesId = monsterBehavior.SpeciesId;
            }

            return 0 < speciesId && masterRepository.SpeciesMasters.TryGetValue(speciesId, out var species)
                ? species
                : null;
        }

        string ResolveDisplayName(Actor actor, ActorProfile profile)
        {
            if (profile != null && !string.IsNullOrWhiteSpace(profile.DisplayName))
            {
                return profile.DisplayName;
            }

            return $"Actor {actor.Id.ToString()[..8]}";
        }

        static string ResolveActorKindText(Actor actor, ActorArchetypeMaster archetype, ActorProfile profile)
        {
            if (archetype != null && archetype.BehaviorType != ActorBehaviorType.None)
            {
                return archetype.BehaviorType.ToString();
            }

            if (profile != null && profile.BehaviorType != ActorBehaviorType.None)
            {
                return profile.BehaviorType.ToString();
            }

            return actor.Behavior switch
            {
                AdventurerBehavior => ActorBehaviorType.Adventurer.ToString(),
                MonsterBehavior => ActorBehaviorType.Monster.ToString(),
                _ => actor.Behavior.GetType().Name
            };
        }

        static string ResolveRoleText(ActorArchetypeMaster archetype, SpeciesMaster species)
        {
            if (archetype != null)
            {
                return archetype.Name;
            }

            return species != null ? species.Name : "-";
        }

        static string ResolveLifecycleText(Actor actor)
        {
            return actor.Behavior switch
            {
                AdventurerBehavior adventurerBehavior => adventurerBehavior.LifecycleState.ToString(),
                MonsterBehavior => "Hostile",
                _ => "-"
            };
        }

        static string FormatGoal(ActorGoal goal)
        {
            if (goal == null || goal.Type == ActorGoalType.None)
            {
                return "-";
            }

            var progress = 0 < goal.TargetCount
                ? $" {goal.ProgressCount}/{goal.TargetCount}"
                : string.Empty;
            var target = 0 < goal.TargetId ? $" #{goal.TargetId}" : string.Empty;
            return $"{goal.Type}{target}{progress}";
        }

        string ResolveItemName(int itemId)
        {
            return masterRepository.ItemMasters.TryGetValue(itemId, out var itemMaster)
                ? itemMaster.Name
                : $"Item {itemId}";
        }
    }
}
