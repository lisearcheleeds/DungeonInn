using System;
using System.Collections.Generic;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Character
{
    public sealed class Character
    {
        public Guid Id { get; }
        public string Name { get; }
        public CharacterRole Role { get; private set; }
        public CharacterStats Stats { get; }
        public Inventory Inventory { get; }
        public int Level { get; private set; }
        public int Experience { get; private set; }
        public int Hp { get; private set; }
        public int Mp { get; private set; }
        public int Fatigue { get; private set; }
        public int Stress { get; private set; }
        public int InjurySeverity { get; private set; }
        public int PreferenceSeed { get; }
        public ScoutCost ScoutCost { get; }
        public IReadOnlyList<ItemStack> Salary { get; }

        public Character(
            Guid id,
            string name,
            CharacterRole role,
            CharacterStats stats,
            Inventory inventory,
            int level,
            int experience,
            int hp,
            int mp,
            int fatigue,
            int stress,
            int injurySeverity,
            int preferenceSeed,
            ScoutCost scoutCost,
            IReadOnlyList<ItemStack> salary)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Character name is required.", nameof(name));
            }

            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            Id = id;
            Name = name;
            Role = role;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            Level = level;
            Experience = Math.Max(0, experience);
            Hp = Math.Max(0, hp);
            Mp = Math.Max(0, mp);
            Fatigue = Math.Max(0, fatigue);
            Stress = Math.Max(0, stress);
            InjurySeverity = Math.Max(0, injurySeverity);
            PreferenceSeed = preferenceSeed;
            ScoutCost = scoutCost ?? throw new ArgumentNullException(nameof(scoutCost));
            Salary = salary ?? throw new ArgumentNullException(nameof(salary));
        }

        public bool CanBeScouted => Role == CharacterRole.Adventurer || Role == CharacterRole.RecruitCandidate;
        public bool IsGuildStaff => Role == CharacterRole.GuildStaff;

        public int CalculateMaxHp()
        {
            return Stats.Constitution * 10 + Stats.Strength * 2 + Level * 5;
        }

        public int CalculateMaxMp()
        {
            return Stats.Intelligence * 5 + Stats.Wisdom * 5 + Level * 2;
        }

        public int CalculateMoveSpeed()
        {
            return 100 + Stats.Dexterity * 2;
        }

        public int CalculateInjuryResistance()
        {
            return Stats.Constitution * 3 + Stats.Wisdom;
        }

        public int CalculateStressResistance()
        {
            return Stats.Wisdom * 3 + Stats.Charisma;
        }

        public int CalculateSwordAttack()
        {
            return Stats.Strength * 3 + Stats.Dexterity + Level;
        }

        public int CalculateBowAttack()
        {
            return Stats.Dexterity * 3 + Stats.Strength + Level;
        }

        public int CalculateExplorationPower()
        {
            return Stats.Dexterity * 2 + Stats.Wisdom * 2 + Stats.Intelligence + Level;
        }

        public int CalculateEquipmentAptitude()
        {
            return Stats.Strength + Stats.Dexterity + Stats.Intelligence;
        }

        public int CalculateFacilityPoint(FacilityType facilityType)
        {
            switch (facilityType)
            {
                case FacilityType.Inn:
                    return Stats.Constitution * 3 + Stats.Wisdom * 2 + Stats.Charisma;
                case FacilityType.Tavern:
                    return Stats.Charisma * 4 + Stats.Wisdom * 2 + Stats.Dexterity;
                case FacilityType.GeneralStore:
                    return Stats.Intelligence * 3 + Stats.Charisma * 2 + Stats.Wisdom;
                case FacilityType.EquipmentShop:
                    return Stats.Strength * 2 + Stats.Dexterity * 2 + Stats.Intelligence * 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(facilityType));
            }
        }

        public void Recover(int hpAmount, int mpAmount, int fatigueReduction, int stressReduction, int injuryReduction)
        {
            Hp = DomainMath.Clamp(Hp + Math.Max(0, hpAmount), 0, CalculateMaxHp());
            Mp = DomainMath.Clamp(Mp + Math.Max(0, mpAmount), 0, CalculateMaxMp());
            Fatigue = Math.Max(0, Fatigue - Math.Max(0, fatigueReduction));
            Stress = Math.Max(0, Stress - Math.Max(0, stressReduction));
            InjurySeverity = Math.Max(0, InjurySeverity - Math.Max(0, injuryReduction));
        }

        public void GainExperience(int amount)
        {
            Experience += Math.Max(0, amount);
        }

        public void RecruitAsStaff()
        {
            if (!CanBeScouted)
            {
                throw new InvalidOperationException("Character cannot be scouted.");
            }

            Role = CharacterRole.GuildStaff;
        }
    }
}
