using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;
using R3;

namespace DungeonInn.Tests.EditMode
{
    public sealed class AdvanceProjectileUseCaseTests
    {
        [Test]
        public void ProjectileHitDealsDamagePublishesEventsAndRemovesProjectile()
        {
            var worldState = new GameWorldState();
            var combatService = new ActorCombatService();
            var eventBus = new CollectingGameEventBus();
            var useCase = new AdvanceProjectileUseCase(
                combatService,
                eventBus,
                CreateGrantExperienceUseCase(eventBus),
                CreateDropItemUseCase(eventBus));
            var attacker = CreateActor("Attacker", 1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f), 50);
            var target = CreateActor("Target", 2, new LayerPosition(MapLayerId.DungeonFloor(1), 6f, 5f), 50);
            var projectile = new ProjectileInstance(
                Guid.NewGuid(),
                attacker.Id,
                target.Id,
                attacker.Position,
                target.Position,
                7,
                10f,
                10f);
            worldState.RegisterActor(attacker);
            worldState.RegisterActor(target);
            worldState.AddProjectile(projectile);

            useCase.ExecuteAsync(worldState, 1f).GetAwaiter().GetResult();

            var projectileHits = eventBus.GetEvents<ProjectileHit>();
            var attacks = eventBus.GetEvents<CombatAttackOccurred>();
            Assert.That(worldState.Projectiles.Count, Is.EqualTo(0));
            Assert.That(target.Hp, Is.EqualTo(43));
            Assert.That(projectileHits.Count, Is.EqualTo(1));
            Assert.That(projectileHits[0].ProjectileId, Is.EqualTo(projectile.Id));
            Assert.That(projectileHits[0].Damage, Is.EqualTo(7));
            Assert.That(attacks.Count, Is.EqualTo(1));
            Assert.That(attacks[0].Damage, Is.EqualTo(7));
            Assert.That(attacks[0].TargetRemainingHp, Is.EqualTo(43));
        }

        sealed class CollectingGameEventBus : IGameEventBus
        {
            readonly List<IGameEvent> events = new();

            public void Publish(IGameEvent gameEvent) => events.Add(gameEvent);

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
                => throw new NotSupportedException();

            public IReadOnlyList<T> GetEvents<T>() where T : class, IGameEvent
                => events.OfType<T>().ToList();
        }

        sealed class ThrowingMasterRepository : IMasterRepository
        {
            public IReadOnlyDictionary<int, ItemMaster> ItemMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, EquipmentMaster> EquipmentMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, WeaponMaster> WeaponMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> WeaponTypeCombatMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, ActorArchetypeMaster> ActorArchetypeMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, AdventurerSpawnMaster> AdventurerSpawnMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, ActorEffectMaster> ActorEffectMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, SpeciesMaster> SpeciesMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, SpawnTableMaster> SpawnTableMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, LevelTable> LevelTables => throw new NotSupportedException();
            public IReadOnlyDictionary<int, DungeonFloorExplorationMaster> DungeonFloorExplorationMasters => throw new NotSupportedException();
            public ItemMaster GetItemMaster(int itemId) => throw new NotSupportedException();
            public EquipmentMaster GetEquipmentMaster(int itemId) => throw new NotSupportedException();
            public WeaponMaster GetWeaponMaster(int itemId) => throw new NotSupportedException();
            public WeaponTypeCombatMaster GetWeaponTypeCombatMaster(WeaponType weaponType) => throw new NotSupportedException();
            public ActorArchetypeMaster GetActorArchetypeMaster(int archetypeId) => throw new NotSupportedException();
            public AdventurerSpawnMaster GetAdventurerSpawnMaster(int adventurerSpawnId) => throw new NotSupportedException();
            public ActorEffectMaster GetActorEffectMaster(int actorEffectId) => throw new NotSupportedException();
            public SpeciesMaster GetSpeciesMaster(int speciesId) => throw new NotSupportedException();
            public SpawnTableMaster GetSpawnTableMaster(int spawnTableId) => throw new NotSupportedException();
            public LevelTable GetLevelTable(int levelTableId) => throw new NotSupportedException();
            public DungeonFloorExplorationMaster GetDungeonFloorExplorationMaster(int floorIndex) => throw new NotSupportedException();
            public int GetMaxStackCount(int itemId) => throw new NotSupportedException();
        }

        sealed class ZeroGameRandom : IGameRandom
        {
            public int Next() => 0;
            public int Next(int maxExclusive) => 0;
            public int Next(int minInclusive, int maxExclusive) => minInclusive;
        }

        static GrantExperienceUseCase CreateGrantExperienceUseCase(IGameEventBus eventBus)
        {
            return new GrantExperienceUseCase(new ThrowingMasterRepository(), eventBus);
        }

        static DropItemUseCase CreateDropItemUseCase(IGameEventBus eventBus)
        {
            return new DropItemUseCase(new ZeroGameRandom(), eventBus);
        }

        static Actor CreateActor(string name, int factionId, LayerPosition position, int hp)
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                0,
                hp,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(factionId, $"Faction {factionId}"),
                new AdventurerBehavior(0));
        }
    }
}
