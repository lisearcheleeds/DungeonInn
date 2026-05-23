using System;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.World
{
    public sealed class PlayerEventLogFormatter
    {
        readonly IActorProfileRegistry actorProfileRegistry;
        readonly IMasterRepository masterRepository;

        [Inject]
        public PlayerEventLogFormatter(
            IActorProfileRegistry actorProfileRegistry,
            IMasterRepository masterRepository)
        {
            this.actorProfileRegistry = actorProfileRegistry ?? throw new ArgumentNullException(nameof(actorProfileRegistry));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public string Format(IGameEvent gameEvent)
        {
            switch (gameEvent)
            {
                case CombatAttackOccurred e:
                    return $"{GetActorName(e.AttackerActorId)} が {GetActorName(e.TargetActorId)} に {e.Damage} ダメージ";
                case ProjectileFired e:
                    return $"{GetActorName(e.AttackerActorId)} が {GetActorName(e.TargetActorId)} に向け投射";
                case ProjectileHit e:
                    return $"{GetActorName(e.AttackerActorId)} の投射物が {GetActorName(e.TargetActorId)} に {e.Damage} ダメージ";
                case AreaEffectCreated e:
                    return $"{GetActorName(e.AttackerActorId)} が範囲攻撃を発動";
                case AreaEffectHit e:
                    return $"{GetActorName(e.AttackerActorId)} の範囲攻撃が {GetActorName(e.TargetActorId)} に {e.Damage} ダメージ";
                case ActorDefeated e:
                    return $"{GetActorName(e.ActorId)} が倒された";
                case ItemDropped e:
                    return $"{GetActorName(e.ActorId)} が {GetItemName(e.ItemInstance.Stack.ItemId)} を落とした";
                case ItemPickedUp e:
                    return $"{GetActorName(e.ActorId)} が {GetItemName(e.ItemInstance.Stack.ItemId)} を拾った";
                case ActorLeveledUp e:
                    return $"{GetActorName(e.ActorId)} が Lv.{e.NewLevel} になった";
                case ActorRecoveringAtInn e:
                    return $"{GetActorName(e.ActorId)} が宿屋で回復中 ({e.CurrentHp}/{e.MaxHp})";
                case ActorFullyRecovered e:
                    return $"{GetActorName(e.ActorId)} が完全回復した";
                default:
                    return string.Empty;
            }
        }

        string GetActorName(Guid actorId)
        {
            return actorProfileRegistry.TryGetProfile(actorId, out var profile) ? profile.DisplayName : "Unknown";
        }

        string GetItemName(int itemId)
        {
            var master = masterRepository.GetItemMaster(itemId);
            return master != null ? master.Name : "Unknown";
        }
    }
}
