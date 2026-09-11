using System;

namespace DigitalMosquito
{
    public class GameConfig
    {
        public static GameConfig Instance { get; set; } = new GameConfig();

        public CreatureSelectionMode SelectionMode { get; set; } = CreatureSelectionMode.Auto;

        public double MosquitoSpawnProbability { get; set; } = 0.70;
        public double SpiderSpawnProbability { get; set; } = 0.30;
        public double GiantSpiderSpawnProbability { get; set; } = 0.15;

        public int GiantSpiderUnlockKills { get; set; } = 3;

        public bool SoundEnabled { get; set; } = false; // Default muted as requested
        public bool BloodFadeOverTime { get; set; } = false;

        public bool JumpscaresEnabled { get; set; } = true;
        public double JumpscareChance { get; set; } = 0.25; // 25% chance on swat attempt

        public double GetSpeedScaling(int kills)
        {
            return 1.0 + Math.Min(1.2, kills * 0.07);
        }

        public TimeSpan GetRespawnInterval(int kills)
        {
            double ms = Math.Max(380, 750 - kills * 35);
            return TimeSpan.FromMilliseconds(ms);
        }

        public CreatureType ChooseNextCreatureType(int kills, Random random)
        {
            if (SelectionMode == CreatureSelectionMode.Boss)
            {
                double bRoll = random.NextDouble();
                if (bRoll < 0.35) return CreatureType.BossGiantMosquito;
                if (bRoll < 0.70) return CreatureType.BossGiantSpider;
                return CreatureType.BossMutantSpider;
            }

            if (SelectionMode == CreatureSelectionMode.Mosquito)
                return CreatureType.Mosquito;
            if (SelectionMode == CreatureSelectionMode.Spider)
                return CreatureType.Spider;
            if (SelectionMode == CreatureSelectionMode.GiantSpider)
                return CreatureType.GiantSpider;

            // Auto mode with level-based progression:
            int level = ProgressionManager.GetLevelForKills(kills).Level;

            // Level 10 (250+ kills) - Boss Mode unlocked!
            if (level >= 10 && random.NextDouble() < 0.40)
            {
                double bRoll = random.NextDouble();
                if (bRoll < 0.35) return CreatureType.BossGiantMosquito;
                if (bRoll < 0.70) return CreatureType.BossGiantSpider;
                return CreatureType.BossMutantSpider;
            }

            // Level 8-9 (100-249 kills) - Rare boss encounters
            if (level >= 8 && random.NextDouble() < 0.18)
            {
                return random.NextDouble() < 0.5 ? CreatureType.BossGiantMosquito : CreatureType.BossGiantSpider;
            }

            // Giant Spiders appear from Level 3+
            if (level >= 3 && random.NextDouble() < (0.15 + (level - 3) * 0.05))
            {
                return CreatureType.GiantSpider;
            }

            double roll = random.NextDouble();
            double totalProb = MosquitoSpawnProbability + SpiderSpawnProbability;
            double mosquitoThreshold = MosquitoSpawnProbability / totalProb;

            return roll < mosquitoThreshold ? CreatureType.Mosquito : CreatureType.Spider;
        }
    }
}
