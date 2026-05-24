using System;
using System.Collections.Generic;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class GetActorDetailQueryTests
    {
        [Test]
        public void QueryReturnsSnapshotListsAcrossRepeatedQueries()
        {
            var masterRepository = new HardcodedMasterRepository();
            var firstActor = CreateActor(new LayerPosition(MapLayerId.Ground, 5f, 5f));
            firstActor.Equip(
                masterRepository.GetEquipmentMaster(3001),
                masterRepository.GetWeaponMaster(3001));
            firstActor.Equip(masterRepository.GetEquipmentMaster(3003));
            firstActor.AddActorEffect(masterRepository.GetActorEffectMaster(1));
            var secondActor = CreateActor(new LayerPosition(MapLayerId.Ground, 10f, 10f));
            var profileRegistry = new ActorProfileRegistry();
            profileRegistry.Register(firstActor.Id, "First");
            profileRegistry.Register(secondActor.Id, "Second");
            var query = new GetActorDetailQuery(
                new TestWorldState(firstActor, secondActor),
                masterRepository,
                profileRegistry);

            var firstDetail = query.Query(firstActor.Id);
            var secondDetail = query.Query(secondActor.Id);

            Assert.That(firstDetail.HasValue, Is.True);
            Assert.That(secondDetail.HasValue, Is.True);
            Assert.That(firstDetail.Value.EquipmentNames.Count, Is.EqualTo(3));
            Assert.That(firstDetail.Value.EquipmentNames[0], Is.EqualTo("Novice Sword"));
            Assert.That(firstDetail.Value.EquipmentNames[1], Is.EqualTo("Cloth Armor"));
            Assert.That(firstDetail.Value.EquipmentNames[2], Is.EqualTo("-"));
            Assert.That(firstDetail.Value.ActiveEffects.Count, Is.EqualTo(1));
            Assert.That(firstDetail.Value.ActiveEffects[0].ActorEffectMasterId, Is.EqualTo(1));
            Assert.That(firstDetail.Value.ActiveEffects[0].DisplayName, Is.EqualTo("体力回復ポーション"));
            Assert.That(secondDetail.Value.EquipmentNames, Is.EqualTo(new[] { "-", "-", "-" }));
            Assert.That(secondDetail.Value.ActiveEffects, Is.Empty);
        }

        static Actor CreateActor(LayerPosition position)
        {
            return new Actor(
                Guid.NewGuid(),
                1,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                0,
                50,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0, AdventurerLifecycleState.Arrived),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        sealed class TestWorldState : IGameWorldStateReader
        {
            readonly IReadOnlyList<Actor> actors;
            readonly Dictionary<Guid, Actor> actorsById = new();

            public TestWorldState(params Actor[] actors)
            {
                this.actors = actors ?? Array.Empty<Actor>();
                foreach (var actor in this.actors)
                {
                    actorsById.Add(actor.Id, actor);
                }
            }

            public bool IsInitialized => true;
            public AdventurerGuild Guild => null;
            public GroundMap GroundMap => null;
            public Dungeon Dungeon => null;
            public InnEconomyState InnEconomy { get; } = new();
            public IReadOnlyList<Actor> Actors => actors;
            public IReadOnlyList<ItemInstance> Items => Array.Empty<ItemInstance>();
            public IReadOnlyList<ProjectileInstance> Projectiles => Array.Empty<ProjectileInstance>();
            public IReadOnlyList<AreaEffectInstance> AreaEffects => Array.Empty<AreaEffectInstance>();
            public SpawnScheduleState SpawnSchedule { get; } = new();

            public Actor FindActor(Guid actorId)
            {
                return actorsById.TryGetValue(actorId, out var actor) ? actor : null;
            }
        }
    }
}
