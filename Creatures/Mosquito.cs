using System;
using System.Windows;

namespace DigitalMosquito
{
    public class Mosquito : Creature
    {
        private readonly Random _random = new();
        private readonly MosquitoRenderer _renderer;

        public override CreatureType CreatureType => CreatureType.Mosquito;
        public override string Name => "Mosquito";
        public override double HitRadius => 130.0;
        public override BloodConfig BloodConfig => BloodConfig.MosquitoPreset;
        public override FrameworkElement VisualElement => _renderer.VisualElement;

        public double WingPhase { get; private set; }

        public override double SpeedMultiplier
        {
            get => base.SpeedMultiplier;
            set
            {
                double old = base.SpeedMultiplier;
                base.SpeedMultiplier = value;
                if (old > 0.001)
                {
                    _currentSpeed = (_currentSpeed / old) * base.SpeedMultiplier;
                    _targetSpeed = (_targetSpeed / old) * base.SpeedMultiplier;
                }
            }
        }

        private double _vx = 4.0;
        private double _vy = 2.0;

        private double _targetSpeed = 4.5;
        private double _currentSpeed = 4.5;
        private double _speedChangeTimer = 0;

        private double _turnTimer = 0;
        private double _turnDuration = 0;
        private double _angularVelocity = 0;

        private double _jitterTime = 0;

        public Mosquito()
        {
            _renderer = new MosquitoRenderer();
            ResetToRandomPosition(SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
        }

        public override void ResetToRandomPosition(double screenWidth, double screenHeight)
        {
            if (screenWidth <= 100) screenWidth = 1920;
            if (screenHeight <= 100) screenHeight = 1080;

            // Spawn near an edge for realism
            int edge = _random.Next(4);
            switch (edge)
            {
                case 0: // Left
                    X = 30;
                    Y = _random.NextDouble() * (screenHeight - 100) + 50;
                    _vx = (3 + _random.NextDouble() * 3) * SpeedMultiplier;
                    _vy = (_random.NextDouble() - 0.5) * 4 * SpeedMultiplier;
                    break;
                case 1: // Right
                    X = screenWidth - 50;
                    Y = _random.NextDouble() * (screenHeight - 100) + 50;
                    _vx = -(3 + _random.NextDouble() * 3) * SpeedMultiplier;
                    _vy = (_random.NextDouble() - 0.5) * 4 * SpeedMultiplier;
                    break;
                case 2: // Top
                    X = _random.NextDouble() * (screenWidth - 100) + 50;
                    Y = 30;
                    _vx = (_random.NextDouble() - 0.5) * 4 * SpeedMultiplier;
                    _vy = (3 + _random.NextDouble() * 3) * SpeedMultiplier;
                    break;
                default: // Bottom
                    X = _random.NextDouble() * (screenWidth - 100) + 50;
                    Y = screenHeight - 50;
                    _vx = (_random.NextDouble() - 0.5) * 4 * SpeedMultiplier;
                    _vy = -(3 + _random.NextDouble() * 3) * SpeedMultiplier;
                    break;
            }

            DisplayX = X;
            DisplayY = Y;
            UpdateAngle();
            _renderer.Render(this);
        }

        public override void Update(double deltaTime, double screenWidth, double screenHeight)
        {
            if (screenWidth <= 100) screenWidth = 1920;
            if (screenHeight <= 100) screenHeight = 1080;

            // 1. Dynamic speed mode transitions (cruising, darting, hovering)
            _speedChangeTimer -= deltaTime;
            if (_speedChangeTimer <= 0)
            {
                int mode = _random.Next(10);
                if (mode < 2)
                {
                    // Hovering pause (slow buzzing in place)
                    _targetSpeed = (1.0 + _random.NextDouble() * 1.5) * SpeedMultiplier;
                    _speedChangeTimer = 0.4 + _random.NextDouble() * 0.8;
                }
                else if (mode < 5)
                {
                    // Sudden dart burst
                    _targetSpeed = (8.5 + _random.NextDouble() * 5.0) * SpeedMultiplier;
                    _speedChangeTimer = (0.3 + _random.NextDouble() * 0.5) / Math.Max(0.5, SpeedMultiplier);
                }
                else
                {
                    // Normal cruising
                    _targetSpeed = (3.5 + _random.NextDouble() * 2.5) * SpeedMultiplier;
                    _speedChangeTimer = (0.8 + _random.NextDouble() * 1.5) / Math.Max(0.5, SpeedMultiplier);
                }
            }

            // Smoothly interpolate speed
            _currentSpeed += (_targetSpeed - _currentSpeed) * Math.Min(1.0, deltaTime * 6.0);

            // 2. Insect flight steering & direction changes
            _turnTimer -= deltaTime;
            if (_turnTimer <= 0)
            {
                _turnDuration = 0.2 + _random.NextDouble() * 0.6;
                _turnTimer = _turnDuration + _random.NextDouble() * 1.2;
                // Sudden turn angular impulse (-3 to +3 radians per second)
                _angularVelocity = (_random.NextDouble() - 0.5) * 6.0;
            }

            // Apply steering rotation to velocity vector
            double heading = Math.Atan2(_vy, _vx);
            heading += _angularVelocity * deltaTime;

            // Small continuous wander jitter
            heading += (_random.NextDouble() - 0.5) * 0.4;

            _vx = Math.Cos(heading) * _currentSpeed;
            _vy = Math.Sin(heading) * _currentSpeed;

            // 3. Boundary avoidance & smooth repulsion from screen edges
            const double margin = 80.0;
            double pushX = 0;
            double pushY = 0;

            if (X < margin)
                pushX += (margin - X) * 0.15;
            else if (X > screenWidth - margin)
                pushX -= (X - (screenWidth - margin)) * 0.15;

            if (Y < margin)
                pushY += (margin - Y) * 0.15;
            else if (Y > screenHeight - margin)
                pushY -= (Y - (screenHeight - margin)) * 0.15;

            _vx += pushX;
            _vy += pushY;

            // Hard clamp & bounce if it actually passes the screen border
            if (X < 15)
            {
                X = 15;
                _vx = Math.Abs(_vx) + 2.0;
            }
            else if (X > screenWidth - 45)
            {
                X = screenWidth - 45;
                _vx = -Math.Abs(_vx) - 2.0;
            }

            if (Y < 15)
            {
                Y = 15;
                _vy = Math.Abs(_vy) + 2.0;
            }
            else if (Y > screenHeight - 45)
            {
                Y = screenHeight - 45;
                _vy = -Math.Abs(_vy) - 2.0;
            }

            // Move base position
            X += _vx;
            Y += _vy;

            // 4. Insect buzzing wobble (perpendicular to flight direction)
            _jitterTime += deltaTime * (35.0 + 20.0 * SpeedMultiplier);
            double normalX = -_vy;
            double normalY = _vx;
            double normalLen = Math.Sqrt(normalX * normalX + normalY * normalY);
            if (normalLen > 0.001)
            {
                normalX /= normalLen;
                normalY /= normalLen;
            }

            double wobble = Math.Sin(_jitterTime) * 3.5 + Math.Sin(_jitterTime * 2.3) * 1.5;
            DisplayX = X + normalX * wobble;
            DisplayY = Y + normalY * wobble;
            RecordPosition(DisplayX, DisplayY);

            // 5. Update heading angle and wing flutter phase
            UpdateAngle();
            WingPhase = (WingPhase + deltaTime * (45.0 + 35.0 * SpeedMultiplier)) % (Math.PI * 2);

            // 6. Update visual rendering
            _renderer.Render(this);
        }

        private void UpdateAngle()
        {
            double radians = Math.Atan2(_vy, _vx);
            AngleDegrees = radians * (180.0 / Math.PI);
        }

        public override void SetVisibility(bool visible)
        {
            base.SetVisibility(visible);
            _renderer.SetVisibility(visible);
        }
    }
}
