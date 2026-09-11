using System;
using System.Windows;

namespace DigitalMosquito
{
    public class GiantSpider : Spider
    {
        public override CreatureType CreatureType => CreatureType.GiantSpider;
        public override string Name => "Giant Spider";
        public override double HitRadius => 220.0;
        public override BloodConfig BloodConfig => BloodConfig.GiantSpiderPreset;
        public override int XpReward => 50;

        public GiantSpider() : base(scale: 1.85, isGiant: true)
        {
        }

        protected override void StartCrawlingFromEdge(double screenWidth, double screenHeight)
        {
            base.StartCrawlingFromEdge(screenWidth, screenHeight);
            // Slower, heavier crawl speed
            _crawlSpeed = (1.4 + _random.NextDouble() * 1.0) * SpeedMultiplier;
            _vx = Math.Cos(AngleDegrees * Math.PI / 180.0) * _crawlSpeed;
            _vy = Math.Sin(AngleDegrees * Math.PI / 180.0) * _crawlSpeed;
        }

        protected override void StartWebDescent(double screenWidth, double screenHeight)
        {
            base.StartWebDescent(screenWidth, screenHeight);
            // Heavier drop
            _crawlSpeed = (1.2 + _random.NextDouble() * 0.8) * SpeedMultiplier;
        }
    }
}
