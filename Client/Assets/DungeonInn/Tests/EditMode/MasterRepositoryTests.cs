using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class MasterRepositoryTests
    {
        [Test]
        public void HardcodedMasterRepositoryInitializesAndValidatesReferences()
        {
            var repository = new HardcodedMasterRepository();

            Assert.That(repository.ItemMasters, Is.Not.Empty);
            Assert.That(repository.EquipmentMasters, Is.Not.Empty);
            Assert.That(repository.WeaponMasters, Is.Not.Empty);
            Assert.That(repository.WeaponTypeCombatMasters, Is.Not.Empty);
            Assert.That(repository.ActorArchetypeMasters, Is.Not.Empty);
            Assert.That(repository.AdventurerSpawnMasters, Is.Not.Empty);
            Assert.That(repository.ActorEffectMasters, Is.Not.Empty);
            Assert.That(repository.SpeciesMasters, Is.Not.Empty);
            Assert.That(repository.SpawnTableMasters, Is.Not.Empty);
        }

        [Test]
        public void PotionReferencesHealActorEffectMaster()
        {
            var repository = new HardcodedMasterRepository();
            var itemMaster = repository.GetItemMaster(2001);
            var actorEffectMaster = repository.GetActorEffectMaster(itemMaster.ActorEffectMasterId);

            Assert.That(itemMaster.Category, Is.EqualTo(ItemCategory.Consumable));
            Assert.That(actorEffectMaster.ReapplyPolicy, Is.EqualTo(ActorEffectReapplyPolicy.AppendDuration));
            Assert.That(actorEffectMaster.StatusEffectSpecs[0].Type, Is.EqualTo(StatusEffectType.HealHpOverTime));
            Assert.That(actorEffectMaster.StatusEffectSpecs[0].Amount, Is.EqualTo(30));
            Assert.That(actorEffectMaster.StatusEffectSpecs[0].DurationSeconds, Is.EqualTo(10f));
        }

        [Test]
        public void WeaponMasterUsesItemMasterPrimaryKey()
        {
            var repository = new HardcodedMasterRepository();
            var weaponMaster = repository.GetWeaponMaster(3001);
            var equipmentMaster = repository.GetEquipmentMaster(3001);
            var itemMaster = repository.GetItemMaster(3001);

            Assert.That(weaponMaster.ItemId, Is.EqualTo(itemMaster.Id));
            Assert.That(equipmentMaster.ItemId, Is.EqualTo(itemMaster.Id));
            Assert.That(equipmentMaster.Slot, Is.EqualTo(EquipmentSlot.Weapon));
        }

        [Test]
        public void SpawnTableEntriesResolveToTargetMasters()
        {
            var repository = new HardcodedMasterRepository();
            var adventurerSpawnTable = repository.GetSpawnTableMaster(1);
            var monsterSpawnTable = repository.GetSpawnTableMaster(2);
            var adventurerSpawnMaster = repository.GetAdventurerSpawnMaster(adventurerSpawnTable.Entries[0].TargetMasterId);

            Assert.That(repository.GetActorArchetypeMaster(adventurerSpawnMaster.ActorArchetypeId), Is.Not.Null);
            Assert.That(repository.GetActorArchetypeMaster(monsterSpawnTable.Entries[0].TargetMasterId), Is.Not.Null);
        }

        [Test]
        public void AdventurerSpawnMasterReferencesAdventurerArchetype()
        {
            var repository = new HardcodedMasterRepository();
            var adventurerSpawnMaster = repository.GetAdventurerSpawnMaster(1);
            var archetypeMaster = repository.GetActorArchetypeMaster(adventurerSpawnMaster.ActorArchetypeId);

            Assert.That(adventurerSpawnMaster.DisplayName, Is.EqualTo("アリス"));
            Assert.That(archetypeMaster.BehaviorType, Is.EqualTo(ActorBehaviorType.Adventurer));
        }

        [Test]
        public void MonsterArchetypeHasDefaultNaturalWeaponType()
        {
            var repository = new HardcodedMasterRepository();
            var monsterArchetypeMaster = repository.GetActorArchetypeMaster(2);

            Assert.That(monsterArchetypeMaster.DefaultWeaponType, Is.EqualTo(WeaponType.Claws));
            Assert.That(repository.GetWeaponTypeCombatMaster(monsterArchetypeMaster.DefaultWeaponType), Is.Not.Null);
        }

        [Test]
        public void ActorArchetypeReferencesSpeciesMaster()
        {
            var repository = new HardcodedMasterRepository();
            var monsterArchetypeMaster = repository.GetActorArchetypeMaster(2);
            var speciesMaster = repository.GetSpeciesMaster(monsterArchetypeMaster.SpeciesId);

            Assert.That(speciesMaster.Name, Is.EqualTo("Goblin"));
            Assert.That(speciesMaster.SpeciesDrops, Is.Not.Empty);
        }

        [Test]
        public void WeaponTypeCombatMasterDefinesNaturalWeaponCombatBaseValues()
        {
            var repository = new HardcodedMasterRepository();
            var claws = repository.GetWeaponTypeCombatMaster(WeaponType.Claws);

            Assert.That(claws.BaseRangeMeters, Is.EqualTo(1.5f));
            Assert.That(claws.BaseAttackIntervalSeconds, Is.EqualTo(0.9f));
        }
    }
}
