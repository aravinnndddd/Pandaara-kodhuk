using System;
using System.Windows;

namespace DigitalMosquito
{
    public enum SpiderBehaviorState
    {
        Crawling,
        Paused,
        DescendingWeb,
        HangingOnWeb,
        ClimbingWeb
    }

    public class Spider : Creature
    {
        protected readonly Random _random = new();
        protected readonly SpiderRenderer _renderer;

        public override CreatureType CreatureType => CreatureType.Spider;
        public override string Name => "Spider";
        public override double HitRadius => 160.0;
        public override BloodConfig BloodConfig => BloodConfig.SpiderPreset;
        public override int XpReward => 20;
        public override FrameworkElement VisualElement => _renderer.VisualElement;

        public SpiderBehaviorState CurrentState { get; protected set; } = SpiderBehaviorState.Crawling;
        public double LegCycle { get; protected set; } = 0.0;
        public bool IsHangingOnWeb => CurrentState == SpiderBehaviorState.HangingOnWeb ||
                                      CurrentState == SpiderBehaviorState.DescendingWeb ||
                                      CurrentState == SpiderBehaviorState.ClimbingWeb;

        public override bool HasWebThread => IsHangingOnWeb;

        protected double _vx = 0;
        protected double _vy = 0;
        protected double _crawlSpeed = 2.4;
        protected double _stateTimer = 0;
        protected double _targetDropY = 300;
        protected double _swayTime = 0;

        public Spider(double scale = 1.0, bool isGiant = false)
        {
            _renderer = new SpiderRenderer(scale, isGiant);
            ResetToRandomPosition(SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
        }

        public override void ResetToRandomPosition(double screenWidth, double screenHeight)
        {
            if (screenWidth <= 100) screenWidth = 1920;
            if (screenHeight <= 100) screenHeight = 1080;

            // 40% chance to start by descending from top of screen on a web thread
            if (_random.NextDouble() < 0.40)
            {
                StartWebDescent(screenWidth, screenHeight);
            }
            else
            {
                StartCrawlingFromEdge(screenWidth, screenHeight);
            }

            _renderer.Render(this);
        }

        protected virtual void StartWebDescent(double screenWidth, double screenHeight)
        {
            CurrentState = SpiderBehaviorState.DescendingWeb;
            X = 80 + _random.NextDouble() * (screenWidth - 160);
            Y = 10;
            DisplayX = X;
            DisplayY = Y;
            AngleDegrees = 90; // Facing down

            _targetDropY = 120 + _random.NextDouble() * (screenHeight * 0.65);
            _crawlSpeed = (1.8 + _random.NextDouble() * 1.2) * SpeedMultiplier;
        }

        protected virtual void StartCrawlingFromEdge(double screenWidth, double screenHeight)
        {
            CurrentState = SpiderBehaviorState.Crawling;
            int edge = _random.Next(4);
            switch (edge)
            {
                case 0: // Left
                    X = 40;
                    Y = _random.NextDouble() * (screenHeight - 120) + 60;
                    SetCrawlHeading(_random.NextDouble() * 1.2 - 0.6); // Rightwards
                    break;
                case 1: // Right
                    X = screenWidth - 40;
                    Y = _random.NextDouble() * (screenHeight - 120) + 60;
                    SetCrawlHeading(Math.PI + (_random.NextDouble() * 1.2 - 0.6)); // Leftwards
                    break;
                case 2: // Top
                    X = _random.NextDouble() * (screenWidth - 120) + 60;
                    Y = 40;
                    SetCrawlHeading(Math.PI / 2 + (_random.NextDouble() * 1.2 - 0.6)); // Downwards
                    break;
                default: // Bottom
                    X = _random.NextDouble() * (screenWidth - 120) + 60;
                    Y = screenHeight - 40;
                    SetCrawlHeading(-Math.PI / 2 + (_random.NextDouble() * 1.2 - 0.6)); // Upwards
                    break;
            }

            DisplayX = X;
            DisplayY = Y;
            _stateTimer = 1.5 + _random.NextDouble() * 3.0;
        }

        protected void SetCrawlHeading(double radians)
        {
            AngleDegrees = radians * (180.0 / Math.PI);
            _crawlSpeed = (2.2 + _random.NextDouble() * 1.8) * SpeedMultiplier;
            _vx = Math.Cos(radians) * _crawlSpeed;
            _vy = Math.Sin(radians) * _crawlSpeed;
        }

        public override void Update(double deltaTime, double screenWidth, double screenHeight)
        {
            if (screenWidth <= 100) screenWidth = 1920;
            if (screenHeight <= 100) screenHeight = 1080;

            switch (CurrentState)
            {
                case SpiderBehaviorState.DescendingWeb:
                    UpdateDescendingWeb(deltaTime);
                    break;

                case SpiderBehaviorState.HangingOnWeb:
                    UpdateHangingOnWeb(deltaTime, screenWidth, screenHeight);
                    break;

                case SpiderBehaviorState.ClimbingWeb:
                    UpdateClimbingWeb(deltaTime, screenWidth, screenHeight);
                    break;

                case SpiderBehaviorState.Paused:
                    UpdatePaused(deltaTime, screenWidth, screenHeight);
                    break;

                case SpiderBehaviorState.Crawling:
                default:
                    UpdateCrawling(deltaTime, screenWidth, screenHeight);
                    break;
            }

            RecordPosition(DisplayX, DisplayY);
            _renderer.Render(this);
        }

        private void UpdateDescendingWeb(double deltaTime)
        {
            AngleDegrees = 90; // Point downward
            double dropStep = (85.0 + 45.0 * SpeedMultiplier) * deltaTime;
            Y += dropStep;

            // Leg dangling twitch
            LegCycle += deltaTime * 4.0;

            _swayTime += deltaTime * 2.5;
            DisplayX = X + Math.Sin(_swayTime) * 3.5;
            DisplayY = Y;

            if (Y >= _targetDropY)
            {
                Y = _targetDropY;
                CurrentState = SpiderBehaviorState.HangingOnWeb;
                _stateTimer = 2.0 + _random.NextDouble() * 3.5;
            }
        }

        private void UpdateHangingOnWeb(double deltaTime, double screenWidth, double screenHeight)
        {
            _stateTimer -= deltaTime;
            _swayTime += deltaTime * 2.0;

            // Gentle pendulum sway
            DisplayX = X + Math.Sin(_swayTime) * 7.0;
            DisplayY = Y + Math.Cos(_swayTime * 1.4) * 2.5;

            // Subtle leg twitches while hanging
            LegCycle += deltaTime * 2.0;

            if (_stateTimer <= 0)
            {
                // Randomly choose: climb back up or drop to crawl
                if (_random.NextDouble() < 0.45)
                {
                    CurrentState = SpiderBehaviorState.ClimbingWeb;
                }
                else
                {
                    // Cut silk and begin crawling
                    CurrentState = SpiderBehaviorState.Crawling;
                    X = DisplayX;
                    Y = DisplayY;
                    double randomAngle = _random.NextDouble() * Math.PI * 2;
                    SetCrawlHeading(randomAngle);
                    _stateTimer = 2.0 + _random.NextDouble() * 3.0;
                }
            }
        }

        private void UpdateClimbingWeb(double deltaTime, double screenWidth, double screenHeight)
        {
            AngleDegrees = -90; // Facing upward
            double climbStep = (70.0 + 40.0 * SpeedMultiplier) * deltaTime;
            Y -= climbStep;

            LegCycle += deltaTime * 7.0; // Rapid leg climbing motion

            _swayTime += deltaTime * 3.0;
            DisplayX = X + Math.Sin(_swayTime) * 3.0;
            DisplayY = Y;

            if (Y <= 30)
            {
                // Reached ceiling! Start crawling horizontally or spawn elsewhere
                Y = 30;
                X = DisplayX;
                CurrentState = SpiderBehaviorState.Crawling;
                SetCrawlHeading(_random.Next(2) == 0 ? 0 : Math.PI); // Crawl left or right along ceiling
                _stateTimer = 1.5 + _random.NextDouble() * 2.5;
            }
        }

        private void UpdateCrawling(double deltaTime, double screenWidth, double screenHeight)
        {
            _stateTimer -= deltaTime;

            // Advance leg cycle proportionally to actual movement
            LegCycle += deltaTime * _crawlSpeed * 3.8;

            X += _vx;
            Y += _vy;
            DisplayX = X;
            DisplayY = Y;

            // Edge bounds reflection
            const double margin = 40.0;
            bool bounced = false;

            if (X < margin)
            {
                X = margin;
                _vx = Math.Abs(_vx);
                bounced = true;
            }
            else if (X > screenWidth - margin)
            {
                X = screenWidth - margin;
                _vx = -Math.Abs(_vx);
                bounced = true;
            }

            if (Y < margin)
            {
                Y = margin;
                _vy = Math.Abs(_vy);
                bounced = true;
            }
            else if (Y > screenHeight - margin)
            {
                Y = screenHeight - margin;
                _vy = -Math.Abs(_vy);
                bounced = true;
            }

            if (bounced)
            {
                AngleDegrees = Math.Atan2(_vy, _vx) * (180.0 / Math.PI);
            }

            // State changes
            if (_stateTimer <= 0)
            {
                double roll = _random.NextDouble();
                if (roll < 0.35)
                {
                    // Pause / freeze in place
                    CurrentState = SpiderBehaviorState.Paused;
                    _stateTimer = 0.6 + _random.NextDouble() * 1.5;
                }
                else if (roll < 0.50 && Y < screenHeight * 0.35)
                {
                    // Near top - climb up silk or drop down
                    StartWebDescent(screenWidth, screenHeight);
                }
                else
                {
                    // Change crawl direction: horizontal, vertical, or diagonal
                    PickNewCrawlDirection();
                    _stateTimer = 1.5 + _random.NextDouble() * 3.5;
                }
            }
        }

        private void UpdatePaused(double deltaTime, double screenWidth, double screenHeight)
        {
            _stateTimer -= deltaTime;
            // Idle subtle leg twitching
            LegCycle += deltaTime * 0.8;

            if (_stateTimer <= 0)
            {
                CurrentState = SpiderBehaviorState.Crawling;
                PickNewCrawlDirection();
                _stateTimer = 1.8 + _random.NextDouble() * 3.0;
            }
        }

        protected void PickNewCrawlDirection()
        {
            // Pick unpredictable angle (horizontal, vertical, diagonal)
            double[] angles = { 0, 45, 90, 135, 180, 225, 270, 315 };
            double baseAngle = angles[_random.Next(angles.Length)] * (Math.PI / 180.0);
            double wander = (_random.NextDouble() - 0.5) * 0.5;
            SetCrawlHeading(baseAngle + wander);
        }

        public override void SetVisibility(bool visible)
        {
            base.SetVisibility(visible);
            _renderer.SetVisibility(visible);
        }
    }
}
