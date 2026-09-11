using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalMosquito
{
    public class CleanerDefinition
    {
        public int Tier { get; set; }
        public int UnlockLevel { get; set; }
        public int RequiredKills { get; set; }
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double WipeRadius { get; set; } = 38.0;
        public double WipePower { get; set; } = 0.28;
        public int BonusCleanXp { get; set; } = 10;
        public bool SpawnsBubbles { get; set; } = false;
        public bool SpawnsLaserGleam { get; set; } = false;
    }

    public static class CleanerProgression
    {
        public static readonly IReadOnlyList<CleanerDefinition> Cleaners = new List<CleanerDefinition>
        {
            new()
            {
                Tier = 1,
                UnlockLevel = 1,
                RequiredKills = 0,
                Id = "tissue",
                Name = "Tissue Paper",
                Icon = "🧻",
                Description = "Basic paper napkin. Gets the job done slowly.",
                WipeRadius = 38.0,
                WipePower = 0.28,
                BonusCleanXp = 10,
                SpawnsBubbles = false
            },
            new()
            {
                Tier = 2,
                UnlockLevel = 3,
                RequiredKills = 10,
                Id = "sponge",
                Name = "Microfiber Sponge",
                Icon = "🧽",
                Description = "Soapy wet sponge. Leaves foam bubbles and cleans faster.",
                WipeRadius = 55.0,
                WipePower = 0.45,
                BonusCleanXp = 15,
                SpawnsBubbles = true
            },
            new()
            {
                Tier = 3,
                UnlockLevel = 5,
                RequiredKills = 25,
                Id = "squeegee",
                Name = "Glass Squeegee",
                Icon = "🪟",
                Description = "Rubber blade squeegee. Wide wiping swipe with squeaky shine.",
                WipeRadius = 78.0,
                WipePower = 0.65,
                BonusCleanXp = 25,
                SpawnsBubbles = true
            },
            new()
            {
                Tier = 4,
                UnlockLevel = 8,
                RequiredKills = 80,
                Id = "hydro_scrubber",
                Name = "Turbo Hydro-Scrubber",
                Icon = "🌀",
                Description = "High-pressure motorized scrubber. Rapidly melts blood with water torrents.",
                WipeRadius = 110.0,
                WipePower = 0.95,
                BonusCleanXp = 40,
                SpawnsBubbles = true
            },
            new()
            {
                Tier = 5,
                UnlockLevel = 11,
                RequiredKills = 250,
                Id = "sonic_sanitizer",
                Name = "Sonic Laser Sanitizer",
                Icon = "⚡",
                Description = "Ultimate screen purifier. Vaporizes splatters instantly with radiant gleam!",
                WipeRadius = 160.0,
                WipePower = 1.40,
                BonusCleanXp = 75,
                SpawnsBubbles = true,
                SpawnsLaserGleam = true
            }
        };

        public static CleanerDefinition GetCleanerForKills(int totalKills)
        {
            for (int i = Cleaners.Count - 1; i >= 0; i--)
            {
                if (totalKills >= Cleaners[i].RequiredKills)
                {
                    return Cleaners[i];
                }
            }
            return Cleaners[0];
        }

        public static CleanerDefinition? GetNextCleaner(int currentTier)
        {
            return Cleaners.FirstOrDefault(c => c.Tier == currentTier + 1);
        }
    }
}
