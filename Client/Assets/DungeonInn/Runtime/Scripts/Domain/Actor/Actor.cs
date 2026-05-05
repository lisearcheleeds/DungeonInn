using System;
using System.Collections.Generic;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Actor
{
    public sealed class Actor
    {
        public Guid Id { get; }
        public string Name { get; }
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
        public int WeaponAttack { get; private set; }
        public WeaponCombatParams WeaponCombatParams { get; private set; }
        public ActorGoal CurrentGoal { get; private set; }
        public ActorPlan CurrentPlan { get; private set; }
        public ActorAction CurrentAction { get; private set; }

        public Actor(
            Guid id,
            string name,
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
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Actor name is required.", nameof(name));
            }

            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            Id = id;
            Name = name;
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

        public void GainExperience(int amount)
        {
            Experience += Math.Max(0, amount);
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

        public void Equip(EquipmentSpec equipmentSpec)
        {
            Equipment.Equip(equipmentSpec);

            if (equipmentSpec.Slot == EquipmentSlot.Weapon)
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
            var weaponType = Equipment.Weapon == null ? WeaponType.Fist : Equipment.Weapon.WeaponType;
            WeaponCalculator = WeaponCalculatorFactory.Create(weaponType);
            WeaponCombatCalculator = WeaponCombatCalculatorFactory.Create(weaponType);
            RefreshWeaponAttack();
            RefreshWeaponCombatParams();
        }

        public void RefreshParams()
        {
            Params = new ActorParamCalculator().Calculate(Stats, Equipment.All, Behavior, Level);
            RefreshWeaponAttack();
            RefreshWeaponCombatParams();
        }

        void RefreshWeaponAttack()
        {
            WeaponAttack = WeaponCalculator.CalculateAttack(Stats, Equipment.Weapon, Behavior, Level);
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
