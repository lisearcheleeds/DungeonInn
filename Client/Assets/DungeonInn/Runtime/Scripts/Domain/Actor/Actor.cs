using System;
using System.Collections.Generic;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;

namespace DungeonInn.Domain.Actor
{
    public sealed class Actor : DungeonInn.Domain.Combat.IWeaponCombatSource
    {
        public Guid Id { get; }
        public int ArchetypeId { get; }
        public ActorStats Stats { get; private set; }
        public ActorParams Params { get; private set; }
        public ActorEquipment Equipment { get; }
        public Inventory Inventory { get; }
        public int Level { get; private set; }
        public int Experience { get; private set; }
        public int Hp { get; private set; }
        public int Mp { get; private set; }
        public int Fatigue { get; private set; }
        public int InjurySeverity { get; private set; }
        public int PreferenceSeed { get; }
        public LayerPosition Position { get; private set; }
        public ActorFaction Faction { get; private set; }
        public IActorBehavior Behavior { get; private set; }
        public IWeaponCalculator WeaponCalculator { get; private set; }
        public IWeaponCombatCalculator WeaponCombatCalculator { get; private set; }
        public WeaponType NaturalWeaponType { get; private set; }
        public WeaponTypeCombatMaster NaturalWeaponTypeCombatMaster { get; private set; }
        public int WeaponAttack { get; private set; }
        public WeaponCombatParams WeaponCombatParams { get; private set; }
        public ActorGoal CurrentGoal { get; private set; }
        public ActorPlan CurrentPlan { get; private set; }
        public ActorAction CurrentAction { get; private set; }

        public Actor(
            Guid id,
            int archetypeId,
            ActorStats stats,
            Inventory inventory,
            int level,
            int experience,
            int hp,
            int mp,
            int fatigue,
            int injurySeverity,
            int preferenceSeed,
            LayerPosition position,
            ActorFaction faction,
            IActorBehavior behavior)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            Id = id;
            ArchetypeId = archetypeId;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            Equipment = new ActorEquipment();
            Level = level;
            Experience = Math.Max(0, experience);
            Hp = Math.Max(0, hp);
            Mp = Math.Max(0, mp);
            Fatigue = Math.Max(0, fatigue);
            InjurySeverity = Math.Max(0, injurySeverity);
            PreferenceSeed = preferenceSeed;
            Position = position;
            Faction = faction ?? throw new ArgumentNullException(nameof(faction));
            Behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            NaturalWeaponType = WeaponType.Fist;
            NaturalWeaponTypeCombatMaster = WeaponTypeCombatMasterCatalog.Get(NaturalWeaponType);
            CurrentGoal = ActorGoal.None();
            CurrentPlan = ActorPlan.None();
            CurrentAction = ActorAction.None();
            RefreshWeaponCalculator();
            RefreshParams();
        }

        public void Recover(int hpAmount, int mpAmount, int fatigueReduction, int stressReduction, int injuryReduction)
        {
            Hp = DomainMath.Clamp(Hp + Math.Max(0, hpAmount), 0, Params.MaxHp);
            Mp = DomainMath.Clamp(Mp + Math.Max(0, mpAmount), 0, Params.MaxMp);
            Fatigue = Math.Max(0, Fatigue - Math.Max(0, fatigueReduction));
            InjurySeverity = Math.Max(0, InjurySeverity - Math.Max(0, injuryReduction));

            if (Behavior is AdventurerBehavior adventurerBehavior)
            {
                adventurerBehavior.ReduceStress(stressReduction);
            }
        }

        public void ReceiveDamage(int amount)
        {
            Hp = DomainMath.Clamp(Hp - Math.Max(0, amount), 0, Params.MaxHp);
        }

        public void GainExperience(int amount)
        {
            Experience += Math.Max(0, amount);
        }

        public void RecalculateLevel(LevelTable levelTable)
        {
            Level = Math.Max(1, levelTable.GetLevel(Experience));
            RefreshParams();
        }

        public void IncreaseStats(
            int strength,
            int dexterity,
            int constitution,
            int intelligence,
            int wisdom,
            int charisma)
        {
            Stats = Stats.Increase(strength, dexterity, constitution, intelligence, wisdom, charisma);
            RefreshParams();
        }

        public void MoveTo(LayerPosition position)
        {
            Position = position;
        }

        public void ChangeFaction(ActorFaction faction)
        {
            Faction = faction ?? throw new ArgumentNullException(nameof(faction));
        }

        public void ChangeBehavior(IActorBehavior behavior)
        {
            Behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            RefreshWeaponCalculator();
            RefreshParams();
        }

        public void ChangeNaturalWeaponType(WeaponType weaponType)
        {
            ChangeNaturalWeaponType(WeaponTypeCombatMasterCatalog.Get(weaponType));
        }

        public void ChangeNaturalWeaponType(WeaponTypeCombatMaster weaponTypeCombatMaster)
        {
            if (weaponTypeCombatMaster == null)
            {
                throw new ArgumentNullException(nameof(weaponTypeCombatMaster));
            }

            NaturalWeaponType = weaponTypeCombatMaster.WeaponType;
            NaturalWeaponTypeCombatMaster = weaponTypeCombatMaster;
            RefreshWeaponCalculator();
            RefreshParams();
        }

        public void ChangeGoal(ActorGoal goal)
        {
            CurrentGoal = goal ?? throw new ArgumentNullException(nameof(goal));
        }

        public void ChangePlan(ActorPlan plan)
        {
            CurrentPlan = plan ?? throw new ArgumentNullException(nameof(plan));
        }

        public void ChangeAction(ActorAction action)
        {
            CurrentAction = action ?? throw new ArgumentNullException(nameof(action));
        }

        public void StartCurrentAction()
        {
            CurrentAction.Start();
        }

        public void CompleteCurrentAction()
        {
            CurrentAction.Complete();
        }

        public void FailCurrentAction()
        {
            CurrentAction.Fail();
        }

        public void CancelCurrentAction()
        {
            CurrentAction.Cancel();
        }

        public void Equip(EquipmentMaster equipmentMaster)
        {
            Equipment.Equip(equipmentMaster);

            if (equipmentMaster.Slot == EquipmentSlot.Weapon)
            {
                RefreshWeaponCalculator();
            }

            RefreshParams();
        }

        public void Equip(EquipmentMaster equipmentMaster, WeaponMaster weaponMaster)
        {
            Equipment.Equip(equipmentMaster, weaponMaster);

            if (equipmentMaster.Slot == EquipmentSlot.Weapon)
            {
                RefreshWeaponCalculator();
            }

            RefreshParams();
        }

        public void Unequip(EquipmentSlot slot)
        {
            Equipment.Unequip(slot);

            if (slot == EquipmentSlot.Weapon)
            {
                RefreshWeaponCalculator();
            }

            RefreshParams();
        }

        void RefreshWeaponCalculator()
        {
            var weaponType = Equipment.Weapon == null ? NaturalWeaponType : Equipment.Weapon.WeaponType;
            var weaponTypeCombatMaster = Equipment.Weapon == null
                ? NaturalWeaponTypeCombatMaster
                : Equipment.Weapon.WeaponTypeCombatMaster;
            WeaponCalculator = WeaponCalculatorFactory.Create(weaponType);
            WeaponCombatCalculator = WeaponCombatCalculatorFactory.Create(weaponTypeCombatMaster);
            RefreshWeaponAttack();
            RefreshWeaponCombatParams();
        }

        // Intentionally public: UseCase can trigger cache refresh when needed (e.g., after applying buffs/debuffs externally).
        public void RefreshParams()
        {
            Params = new ActorParamCalculator().Calculate(Stats, Equipment.All, Behavior, Level);
            RefreshWeaponAttack();
            RefreshWeaponCombatParams();
        }

        void RefreshWeaponAttack()
        {
            WeaponAttack = WeaponCalculator.CalculateAttack(Stats, Equipment.Weapon, Equipment.WeaponEquipment, Behavior, Level, Equipment.AllStatBonuses);
        }

        void RefreshWeaponCombatParams()
        {
            WeaponCombatParams = WeaponCombatCalculator.Calculate(this, Equipment.Weapon);
        }

        public TBehavior RequireBehavior<TBehavior>() where TBehavior : class, IActorBehavior
        {
            if (Behavior is TBehavior behavior)
            {
                return behavior;
            }

            throw new InvalidOperationException("Actor behavior type does not match.");
        }
    }
}
