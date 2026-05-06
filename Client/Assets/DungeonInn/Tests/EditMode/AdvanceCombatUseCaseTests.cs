using System;
using System.Linq;
using DungeonInn.Application.Combat;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class AdvanceCombatUseCaseTests
    {
        [Test]
        public void DirectAttackDealsDamageWhenTargetIsInWeaponRange()
        {
            var clock = new FakeGameClock { ElapsedGameTimeSeconds = 0f };
            var worldState = new GameWorldState();
            var combatService = new ActorCombatService();
            var useCase = new AdvanceCombatUseCase(combatService, clock);
            var attacker = CreateActor("Attacker", 1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f), 50);
            var target = CreateActor("Target", 2, new LayerPosition(MapLayerId.DungeonFloor(1), 6f, 5f), 50);
            worldState.RegisterActor(attacker);
            worldState.RegisterActor(target);
            combatService.GetOrCreateCombatState(attacker.Id).SetTarget(target.Id);

            var result = useCase.ExecuteAsync(worldState, 0f).GetAwaiter().GetResult();

            Assert.That(result.Attacks.Count, Is.EqualTo(1));
            Assert.That(result.Attacks[0].Damage, Is.EqualTo(attacker.WeaponCombatParams.AttackSpec.Nodes[0].DamageSpec.Amount));
            Assert.That(target.Hp, Is.EqualTo(50 - result.Attacks[0].Damage));
        }

        [Test]
        public void AttackCooldownPreventsRepeatedAttackUntilEnoughGameSecondsPass()
        {
            var clock = new FakeGameClock { ElapsedGameTimeSeconds = 0f };
            var worldState = new GameWorldState();
            var combatService = new ActorCombatService();
            var useCase = new AdvanceCombatUseCase(combatService, clock);
            var attacker = CreateActor("Attacker", 1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f), 50);
            var target = CreateActor("Target", 2, new LayerPosition(MapLayerId.DungeonFloor(1), 6f, 5f), 50);
            worldState.RegisterActor(attacker);
            worldState.RegisterActor(target);
            combatService.GetOrCreateCombatState(attacker.Id).SetTarget(target.Id);

            var first = useCase.ExecuteAsync(worldState, 0f).GetAwaiter().GetResult();
            var hpAfterFirstAttack = target.Hp;
            clock.ElapsedGameTimeSeconds = 0.1f;
            var second = useCase.ExecuteAsync(worldState, 0f).GetAwaiter().GetResult();

            Assert.That(first.Attacks.Count, Is.EqualTo(1));
            Assert.That(second.Attacks.Count, Is.EqualTo(0));
            Assert.That(target.Hp, Is.EqualTo(hpAfterFirstAttack));
        }

        [Test]
        public void DefeatedTargetIsRemovedFromWorldAndCombatTargetsAreCleared()
        {
            var clock = new FakeGameClock { ElapsedGameTimeSeconds = 0f };
            var worldState = new GameWorldState();
            var combatService = new ActorCombatService();
            var useCase = new AdvanceCombatUseCase(combatService, clock);
            var attacker = CreateActor("Attacker", 1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f), 50);
            var target = CreateActor("Target", 2, new LayerPosition(MapLayerId.DungeonFloor(1), 6f, 5f), 1);
            worldState.RegisterActor(attacker);
            worldState.RegisterActor(target);
            combatService.GetOrCreateCombatState(attacker.Id).SetTarget(target.Id);
            combatService.GetOrCreateCombatState(target.Id).SetTarget(attacker.Id);

            var result = useCase.ExecuteAsync(worldState, 0f).GetAwaiter().GetResult();

            Assert.That(result.Deaths.Count, Is.EqualTo(1));
            Assert.That(result.Deaths[0].ActorId, Is.EqualTo(target.Id));
            Assert.That(worldState.Actors.Any(x => x.Id.Equals(target.Id)), Is.False);
            Assert.That(combatService.HasTarget(attacker.Id), Is.False);
        }

        sealed class FakeGameClock : IGameClock
        {
            public int CurrentScheduleTick => 0;
            public int CurrentDay => 0;
            public float ElapsedRealTimeSeconds => ElapsedGameTimeSeconds;
            public float ElapsedGameTimeSeconds { get; set; }
            public float TimeScale => 1f;
            public void SetTimeScale(float timeScale) { }
            public GameClockAdvanceResult Advance(float unscaledDeltaTimeSeconds)
                => new GameClockAdvanceResult(0, false);
        }

        static Actor CreateActor(string name, int factionId, LayerPosition position, int hp)
        {
            return new Actor(
                Guid.NewGuid(),
                name,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(),
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
