using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.World;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public static class GameEventAlertFormatter
    {
        public static string Format(IGameEvent gameEvent)
        {
            switch (gameEvent)
            {
                case ActorAiDecisionRecorded:
                    return "Actor AI decision recorded";
                case ActorDefeated:
                    return "Actor defeated";
                case ActorDeparted:
                    return "Actor departed";
                case ActorEnteredDungeon:
                    return "Actor entered dungeon";
                case ActorExitedDungeon:
                    return "Actor exited dungeon";
                case ActorFullyRecovered:
                    return "Actor fully recovered";
                case ActorGoalCompleted:
                    return "Actor goal completed";
                case ActorLeveledUp:
                    return "Actor leveled up";
                case ActorRecoveringAtInn:
                    return "Actor recovering at inn";
                case ActorReservedInn:
                    return "Actor reserved inn";
                case ActorSpawned:
                    return "Actor spawned";
                case ActorStartedReturning:
                    return "Actor started returning";
                case ActorWaitingForInn:
                    return "Actor waiting for inn";
                case AreaEffectCreated:
                    return "Area effect created";
                case AreaEffectHit:
                    return "Area effect hit";
                case CombatAttackOccurred:
                    return "Combat attack occurred";
                case CombatEncounterEnded:
                    return "Combat encounter ended";
                case CombatEncounterStarted:
                    return "Combat encounter started";
                case DailyInnReportGenerated:
                    return "Daily inn report generated";
                case DungeonFloorRegeneratedEvent:
                    return "Dungeon floor regenerated";
                case EquipmentChanged:
                    return "Equipment changed";
                case ExperienceGranted:
                    return "Experience granted";
                case GuildSupplyReplenished:
                    return "Guild supply replenished";
                case InnFeeCharged:
                    return "Inn fee charged";
                case InnSatisfactionChanged:
                    return "Inn satisfaction changed";
                case ItemDropped:
                    return "Item dropped";
                case ItemPickedUp:
                    return "Item picked up";
                case ItemSold:
                    return "Item sold";
                case MapLayerAddedEvent:
                    return "Map layer added";
                case ProjectileFired:
                    return "Projectile fired";
                case ProjectileHit:
                    return "Projectile hit";
                default:
                    return string.Empty;
            }
        }
    }
}
