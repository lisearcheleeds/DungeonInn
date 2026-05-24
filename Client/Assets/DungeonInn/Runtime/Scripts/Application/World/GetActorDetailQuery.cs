using System;
using System.Collections.Generic;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.World
{
    public sealed class GetActorDetailQuery
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
        readonly List<string> equipmentNameBuffer = new();
        readonly List<ActorEffectIconViewData> effectBuffer = new();

        [Inject]
        public GetActorDetailQuery(
            IGameWorldStateReader worldState,
            IMasterRepository masterRepository,
            IActorProfileRegistry actorProfileRegistry)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.actorProfileRegistry = actorProfileRegistry ?? throw new ArgumentNullException(nameof(actorProfileRegistry));
        }

        public ActorDetailViewData? Query(Guid actorId)
        {
            var actor = worldState.FindActor(actorId);
            if (actor == null)
            {
                return null;
            }

            var displayName = actorProfileRegistry.TryGetProfile(actorId, out var profile)
                ? profile.DisplayName
                : "Unknown";

            equipmentNameBuffer.Clear();
            foreach (var slot in AllEquipmentSlots)
            {
                var itemId = actor.Equipment.GetEquippedItemId(slot);
                if (itemId.HasValue)
                {
                    var itemMaster = masterRepository.GetItemMaster(itemId.Value);
                    equipmentNameBuffer.Add(itemMaster != null ? itemMaster.Name : "-");
                }
                else
                {
                    equipmentNameBuffer.Add("-");
                }
            }

            effectBuffer.Clear();
            foreach (var effect in actor.ActorEffects)
            {
                if (effect.IsExpired)
                {
                    continue;
                }

                var effectMaster = masterRepository.GetActorEffectMaster(effect.ActorEffectMasterId);
                var remainingSeconds = effect.DurationSeconds - effect.ElapsedSeconds;
                effectBuffer.Add(new ActorEffectIconViewData(
                    effect.ActorEffectMasterId,
                    effectMaster.Name,
                    remainingSeconds));
            }

            return new ActorDetailViewData(
                actor.Id,
                actor.Position,
                displayName,
                actor.Level,
                actor.Stats.Strength,
                actor.Stats.Dexterity,
                actor.Stats.Constitution,
                actor.Stats.Intelligence,
                actor.Stats.Wisdom,
                actor.Stats.Charisma,
                actor.Hp,
                actor.Params.MaxHp,
                actor.Mp,
                actor.Params.MaxMp,
                actor.Fatigue,
                actor.InjurySeverity,
                actor.Inventory.Gold,
                equipmentNameBuffer.ToArray(),
                effectBuffer.Count == 0 ? Array.Empty<ActorEffectIconViewData>() : effectBuffer.ToArray());
        }

    }
}
