using System;
using System.Windows.Media;

namespace DigitalMosquito
{
    public enum CreatureType
    {
        Mosquito,
        Spider,
        GiantSpider,
        BossGiantMosquito,
        BossGiantSpider,
        BossMutantSpider
    }

    public enum CreatureSelectionMode
    {
        Auto,
        Mosquito,
        Spider,
        GiantSpider,
        Boss
    }

    public class BloodConfig
    {
        public CreatureType CreatureType { get; set; } = CreatureType.Mosquito;
        public int MinParticles { get; set; } = 35;
        public int MaxParticles { get; set; } = 60;
        public double SplatterRadius { get; set; } = 120.0;
        public double MinDropletRadius { get; set; } = 2.5;
        public double MaxDropletRadius { get; set; } = 9.5;
        public double WipeResistance { get; set; } = 1.0;
        public bool FadeOverTime { get; set; } = false;

        public static BloodConfig MosquitoPreset => new()
        {
            CreatureType = CreatureType.Mosquito,
            MinParticles = 35,
            MaxParticles = 60,
            SplatterRadius = 120.0,
            MinDropletRadius = 2.5,
            MaxDropletRadius = 9.5,
            WipeResistance = 1.0,
            FadeOverTime = false
        };

        public static BloodConfig SpiderPreset => new()
        {
            CreatureType = CreatureType.Spider,
            MinParticles = 85,
            MaxParticles = 145,
            SplatterRadius = 200.0,
            MinDropletRadius = 4.0,
            MaxDropletRadius = 18.0,
            WipeResistance = 1.25,
            FadeOverTime = false
        };

        public static BloodConfig GiantSpiderPreset => new()
        {
            CreatureType = CreatureType.GiantSpider,
            MinParticles = 170,
            MaxParticles = 270,
            SplatterRadius = 300.0,
            MinDropletRadius = 5.5,
            MaxDropletRadius = 28.0,
            WipeResistance = 1.45,
            FadeOverTime = false
        };

        public static BloodConfig BossPreset => new()
        {
            CreatureType = CreatureType.BossGiantSpider,
            MinParticles = 240,
            MaxParticles = 380,
            SplatterRadius = 380.0,
            MinDropletRadius = 6.0,
            MaxDropletRadius = 35.0,
            WipeResistance = 1.6,
            FadeOverTime = false
        };

        public BloodConfig WithWeaponModifier(double particleMultiplier, double radiusMultiplier)
        {
            return new BloodConfig
            {
                CreatureType = this.CreatureType,
                MinParticles = Math.Max(15, (int)(this.MinParticles * particleMultiplier)),
                MaxParticles = Math.Max(25, (int)(this.MaxParticles * particleMultiplier)),
                SplatterRadius = Math.Max(60.0, this.SplatterRadius * radiusMultiplier),
                MinDropletRadius = this.MinDropletRadius * Math.Sqrt(Math.Max(0.5, radiusMultiplier)),
                MaxDropletRadius = this.MaxDropletRadius * Math.Sqrt(Math.Max(0.5, radiusMultiplier)),
                WipeResistance = this.WipeResistance,
                FadeOverTime = this.FadeOverTime
            };
        }
    }
}
