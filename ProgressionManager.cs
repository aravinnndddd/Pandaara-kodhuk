using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalMosquito
{
    public class LevelDefinition
    {
        public int Level { get; set; }
        public int RequiredKills { get; set; }
        public string WeaponId { get; set; } = string.Empty;
        public string WeaponName { get; set; } = string.Empty;
        public string WeaponIcon { get; set; } = string.Empty;
        public string UnlockDescription { get; set; } = string.Empty;
        public double BloodParticleMultiplier { get; set; } = 1.0;
        public double BloodRadiusMultiplier { get; set; } = 1.0;
        public int AttackDamage { get; set; } = 1;
        public double OverlayShakeMagnitude { get; set; } = 0.0;
    }

    public static class ProgressionManager
    {
        public static readonly IReadOnlyList<LevelDefinition> Levels = new List<LevelDefinition>
        {
            new()
            {
                Level = 1,
                RequiredKills = 0,
                WeaponId = "hand",
                WeaponName = "Hand",
                WeaponIcon = "👋",
                UnlockDescription = "Starting weapon. Quick slap!",
                BloodParticleMultiplier = 1.0,
                BloodRadiusMultiplier = 1.0,
                AttackDamage = 1,
                OverlayShakeMagnitude = 0.0
            },
            new()
            {
                Level = 2,
                RequiredKills = 5,
                WeaponId = "swatter",
                WeaponName = "Fly Swatter",
                WeaponIcon = "🪰",
                UnlockDescription = "Noticeably faster swatting!",
                BloodParticleMultiplier = 1.2,
                BloodRadiusMultiplier = 1.1,
                AttackDamage = 1,
                OverlayShakeMagnitude = 0.0
            },
            new()
            {
                Level = 3,
                RequiredKills = 10,
                WeaponId = "newspaper",
                WeaponName = "Newspaper",
                WeaponIcon = "📰",
                UnlockDescription = "Rolled newspaper swing!",
                BloodParticleMultiplier = 1.45,
                BloodRadiusMultiplier = 1.25,
                AttackDamage = 1,
                OverlayShakeMagnitude = 0.0
            },
            new()
            {
                Level = 4,
                RequiredKills = 15,
                WeaponId = "slipper",
                WeaponName = "Slipper",
                WeaponIcon = "🩴",
                UnlockDescription = "Heavy chancla slap with loud thwack!",
                BloodParticleMultiplier = 1.8,
                BloodRadiusMultiplier = 1.4,
                AttackDamage = 1,
                OverlayShakeMagnitude = 6.0
            },
            new()
            {
                Level = 5,
                RequiredKills = 25,
                WeaponId = "bat",
                WeaponName = "Bat",
                WeaponIcon = "🏏",
                UnlockDescription = "Major upgrade! Whoosh swing & loud crack!",
                BloodParticleMultiplier = 2.3,
                BloodRadiusMultiplier = 1.65,
                AttackDamage = 1,
                OverlayShakeMagnitude = 14.0
            },
            new()
            {
                Level = 6,
                RequiredKills = 40,
                WeaponId = "hammer",
                WeaponName = "Hammer",
                WeaponIcon = "🔨",
                UnlockDescription = "Heavy crushing smash with spark shockwaves!",
                BloodParticleMultiplier = 2.8,
                BloodRadiusMultiplier = 1.85,
                AttackDamage = 2,
                OverlayShakeMagnitude = 18.0
            },
            new()
            {
                Level = 7,
                RequiredKills = 60,
                WeaponId = "vacuum",
                WeaponName = "Vacuum",
                WeaponIcon = "🌀",
                UnlockDescription = "Vortex suction pulls creatures into the nozzle!",
                BloodParticleMultiplier = 2.5,
                BloodRadiusMultiplier = 1.6,
                AttackDamage = 2,
                OverlayShakeMagnitude = 8.0
            },
            new()
            {
                Level = 8,
                RequiredKills = 80,
                WeaponId = "washer",
                WeaponName = "Water Wiper",
                WeaponIcon = "🌊",
                UnlockDescription = "High-pressure water jet & squeegee wiper sweeps and cleans the screen!",
                BloodParticleMultiplier = 2.0,
                BloodRadiusMultiplier = 2.2,
                AttackDamage = 2,
                OverlayShakeMagnitude = 12.0
            },
            new()
            {
                Level = 9,
                RequiredKills = 100,
                WeaponId = "electric",
                WeaponName = "Electric Swatter",
                WeaponIcon = "⚡",
                UnlockDescription = "High-voltage electric lightning arcs and zaps!",
                BloodParticleMultiplier = 3.2,
                BloodRadiusMultiplier = 2.0,
                AttackDamage = 2,
                OverlayShakeMagnitude = 16.0
            },
            new()
            {
                Level = 10,
                RequiredKills = 150,
                WeaponId = "giant_slipper",
                WeaponName = "Giant Slipper",
                WeaponIcon = "🩴",
                UnlockDescription = "Ridiculously colossal chancla slam!",
                BloodParticleMultiplier = 4.2,
                BloodRadiusMultiplier = 2.5,
                AttackDamage = 3,
                OverlayShakeMagnitude = 28.0
            },
            new()
            {
                Level = 11,
                RequiredKills = 250,
                WeaponId = "boss_mode",
                WeaponName = "Boss Mode",
                WeaponIcon = "👑",
                UnlockDescription = "Unlocks Legendary Boss creatures & massive rewards!",
                BloodParticleMultiplier = 5.0,
                BloodRadiusMultiplier = 3.0,
                AttackDamage = 3,
                OverlayShakeMagnitude = 32.0
            }
        };

        public static LevelDefinition GetLevelForKills(int totalKills)
        {
            for (int i = Levels.Count - 1; i >= 0; i--)
            {
                if (totalKills >= Levels[i].RequiredKills)
                {
                    return Levels[i];
                }
            }
            return Levels[0];
        }

        public static LevelDefinition GetDefinitionForLevel(int level)
        {
            var match = Levels.FirstOrDefault(l => l.Level == level);
            return match ?? Levels[0];
        }

        public static LevelDefinition? GetNextLevelDefinition(int currentLevel)
        {
            return Levels.FirstOrDefault(l => l.Level == currentLevel + 1);
        }

        public static LevelDefinition GetDefinitionForWeapon(string weaponId)
        {
            var match = Levels.FirstOrDefault(l => string.Equals(l.WeaponId, weaponId, StringComparison.OrdinalIgnoreCase));
            return match ?? Levels[0];
        }

        public static void GetProgressToNextLevel(
            int totalKills,
            out LevelDefinition currentLevelDef,
            out LevelDefinition? nextLevelDef,
            out int currentKillsInLevel,
            out int requiredKillsForNext,
            out double progressPercent)
        {
            currentLevelDef = GetLevelForKills(totalKills);
            nextLevelDef = GetNextLevelDefinition(currentLevelDef.Level);

            if (nextLevelDef == null)
            {
                // Max level reached
                currentKillsInLevel = totalKills;
                requiredKillsForNext = currentLevelDef.RequiredKills;
                progressPercent = 1.0;
                return;
            }

            int startKills = currentLevelDef.RequiredKills;
            int endKills = nextLevelDef.RequiredKills;
            int span = endKills - startKills;

            currentKillsInLevel = totalKills - startKills;
            requiredKillsForNext = span;
            progressPercent = Math.Clamp((double)currentKillsInLevel / span, 0.0, 1.0);
        }
    }
}
