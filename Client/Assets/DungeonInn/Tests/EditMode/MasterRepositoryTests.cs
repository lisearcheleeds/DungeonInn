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
            Assert.That(repository.ActorArchetypeMasters, Is.Not.Empty);
            Assert.That(repository.MonsterSpeciesMasters, Is.Not.Empty);
            Assert.That(repository.SpawnTableMasters, Is.Not.Empty);
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

            Assert.That(repository.GetActorArchetypeMaster(adventurerSpawnTable.Entries[0].TargetId), Is.Not.Null);
            Assert.That(repository.GetMonsterSpeciesMaster(monsterSpawnTable.Entries[0].TargetId), Is.Not.Null);
        }

        [Test]
        public void MonsterSpeciesHasDefaultNaturalWeaponType()
        {
            var repository = new HardcodedMasterRepository();
            var monsterSpeciesMaster = repository.GetMonsterSpeciesMaster(1);

            Assert.That(monsterSpeciesMaster.DefaultWeaponType, Is.EqualTo(WeaponType.Claws));
        }
    }
}
