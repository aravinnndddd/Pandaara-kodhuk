using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace DigitalMosquito
{
    public abstract class BossCreature : Creature
    {
        public override bool IsBoss => true;
        public override int XpReward => 200;
        public override BloodConfig BloodConfig => BloodConfig.BossPreset;

        protected Canvas? _bossHudCanvas;
        protected ProgressBar? _healthBar;
        protected TextBlock? _crownBadge;

        protected void AttachBossHud(Canvas rootCanvas, double width, double yOffset)
        {
            _bossHudCanvas = new Canvas
            {
                Width = 140,
                Height = 32,
                IsHitTestVisible = false
            };

            // Floating Golden Crown badge
            _crownBadge = new TextBlock
            {
                Text = "👑 BOSS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                FontFamily = new FontFamily("Segoe UI, sans-serif"),
                TextAlignment = TextAlignment.Center,
                Width = 140
            };
            _crownBadge.Effect = new DropShadowEffect
            {
                BlurRadius = 8,
                Color = Color.FromRgb(255, 215, 0),
                Opacity = 0.9,
                ShadowDepth = 0
            };
            Canvas.SetTop(_crownBadge, 0);
            _bossHudCanvas.Children.Add(_crownBadge);

            // Boss Health Bar
            _healthBar = new ProgressBar
            {
                Width = 110,
                Height = 8,
                Minimum = 0,
                Maximum = MaxHealth,
                Value = CurrentHealth,
                Background = new SolidColorBrush(Color.FromArgb(180, 20, 20, 25)),
                Foreground = new SolidColorBrush(Color.FromRgb(255, 30, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                BorderThickness = new Thickness(1)
            };
            Canvas.SetLeft(_healthBar, 15);
            Canvas.SetTop(_healthBar, 18);
            _bossHudCanvas.Children.Add(_healthBar);

            Canvas.SetLeft(_bossHudCanvas, (width - 140) / 2.0);
            Canvas.SetTop(_bossHudCanvas, yOffset);
            rootCanvas.Children.Add(_bossHudCanvas);
        }

        public override bool TakeDamage(int damage)
        {
            bool dead = base.TakeDamage(damage);
            if (_healthBar != null)
            {
                _healthBar.Value = Math.Max(0, CurrentHealth);
            }
            return dead;
        }

        public override void ResetHealth()
        {
            base.ResetHealth();
            if (_healthBar != null)
            {
                _healthBar.Value = CurrentHealth;
            }
        }
    }

    public class BossGiantMosquito : BossCreature
    {
        private readonly MosquitoRenderer _renderer;
        private double _vx = 5.5;
        private double _vy = 3.2;
        private double _targetSpeed = 5.5;
        private double _currentSpeed = 5.5;
        private double _turnTimer = 0;
        private double _turnDuration = 0;
        private double _angularVelocity = 0;
        private double _jitterTime = 0;
        private readonly Random _random = new();

        public override CreatureType CreatureType => CreatureType.BossGiantMosquito;
        public override string Name => "👑 Giant Mosquito";
        public override double HitRadius => 180.0;
        public override int MaxHealth => 3;
        public override FrameworkElement VisualElement => _renderer.VisualElement;

        public BossGiantMosquito()
        {
            CurrentHealth = MaxHealth;
            _renderer = new MosquitoRenderer();
            var canvas = _renderer.VisualElement as Canvas;
            if (canvas != null)
            {
                // Scale vector mosquito up by 1.7x for boss presence
                var scale = new ScaleTransform(1.7, 1.7, 50, 50);
                canvas.LayoutTransform = scale;
                AttachBossHud(canvas, 100, -38);
            }
        }

        public override void ResetToRandomPosition(double screenWidth, double screenHeight)
        {
            if (screenWidth <= 100) screenWidth = 1920;
            if (screenHeight <= 100) screenHeight = 1080;

            X = _random.NextDouble() * (screenWidth - 260) + 130;
            Y = _random.NextDouble() * (screenHeight - 260) + 130;
            DisplayX = X;
            DisplayY = Y;
            ResetHealth();
        }

        public override void Update(double deltaTime, double screenWidth, double screenHeight)
        {
            if (screenWidth <= 100) screenWidth = 1920;
            if (screenHeight <= 100) screenHeight = 1080;

            _jitterTime += deltaTime * 12.0;

            // Erratic aggressive boss movement
            _turnTimer -= deltaTime;
            if (_turnTimer <= 0)
            {
                _turnDuration = 0.3 + _random.NextDouble() * 0.7;
                _turnTimer = _turnDuration;
                _angularVelocity = (_random.NextDouble() * 2.0 - 1.0) * 8.5;
                _targetSpeed = (4.5 + _random.NextDouble() * 4.0) * SpeedMultiplier;
            }

            double currentAngle = Math.Atan2(_vy, _vx);
            currentAngle += _angularVelocity * deltaTime;

            _currentSpeed += (_targetSpeed - _currentSpeed) * (deltaTime * 3.5);
            _vx = Math.Cos(currentAngle) * _currentSpeed;
            _vy = Math.Sin(currentAngle) * _currentSpeed;

            X += _vx;
            Y += _vy;

            // Bounds bounce
            const double margin = 90.0;
            if (X < margin) { X = margin; _vx = Math.Abs(_vx); }
            else if (X > screenWidth - margin) { X = screenWidth - margin; _vx = -Math.Abs(_vx); }
            if (Y < margin) { Y = margin; _vy = Math.Abs(_vy); }
            else if (Y > screenHeight - margin) { Y = screenHeight - margin; _vy = -Math.Abs(_vy); }

            DisplayX = X;
            DisplayY = Y;
            AngleDegrees = Math.Atan2(_vy, _vx) * (180.0 / Math.PI);

            RecordPosition(DisplayX, DisplayY);
            if (_renderer.VisualElement is Canvas c)
            {
                Canvas.SetLeft(c, DisplayX - 50);
                Canvas.SetTop(c, DisplayY - 50);
            }
        }
    }

    public class BossGiantSpider : BossCreature
    {
        private readonly SpiderRenderer _renderer;
        private double _crawlSpeed = 2.4;
        private double _vx = 2.0;
        private double _vy = 1.2;
        private double _stateTimer = 2.0;
        private readonly Random _random = new();

        public override CreatureType CreatureType => CreatureType.BossGiantSpider;
        public override string Name => "👑 Giant Spider";
        public override double HitRadius => 240.0;
        public override int MaxHealth => 4;
        public override FrameworkElement VisualElement => _renderer.VisualElement;

        public BossGiantSpider()
        {
            CurrentHealth = MaxHealth;
            _renderer = new SpiderRenderer(scale: 2.2, isGiant: true);
            if (_renderer.VisualElement is Canvas canvas)
            {
                AttachBossHud(canvas, 210, -32);
            }
        }

        public override void ResetToRandomPosition(double screenWidth, double screenHeight)
        {
            if (screenWidth <= 100) screenWidth = 1920;
            if (screenHeight <= 100) screenHeight = 1080;

            X = _random.NextDouble() * (screenWidth - 300) + 150;
            Y = _random.NextDouble() * (screenHeight - 300) + 150;
            DisplayX = X;
            DisplayY = Y;
            ResetHealth();
        }

        public override void Update(double deltaTime, double screenWidth, double screenHeight)
        {
            if (screenWidth <= 100) screenWidth = 1920;
            if (screenHeight <= 100) screenHeight = 1080;

            _stateTimer -= deltaTime;
            if (_stateTimer <= 0)
            {
                double angle = _random.NextDouble() * Math.PI * 2.0;
                _crawlSpeed = (2.6 + _random.NextDouble() * 2.2) * SpeedMultiplier;
                _vx = Math.Cos(angle) * _crawlSpeed;
                _vy = Math.Sin(angle) * _crawlSpeed;
                AngleDegrees = angle * (180.0 / Math.PI);
                _stateTimer = 1.2 + _random.NextDouble() * 2.5;
            }

            X += _vx;
            Y += _vy;

            const double margin = 100.0;
            if (X < margin) { X = margin; _vx = Math.Abs(_vx); }
            else if (X > screenWidth - margin) { X = screenWidth - margin; _vx = -Math.Abs(_vx); }
            if (Y < margin) { Y = margin; _vy = Math.Abs(_vy); }
            else if (Y > screenHeight - margin) { Y = screenHeight - margin; _vy = -Math.Abs(_vy); }

            DisplayX = X;
            DisplayY = Y;
            RecordPosition(DisplayX, DisplayY);

            if (_renderer.VisualElement is Canvas c)
            {
                Canvas.SetLeft(c, DisplayX - 105);
                Canvas.SetTop(c, DisplayY - 105);
            }
        }
    }

    public class BossMutantSpider : BossCreature
    {
        private readonly SpiderRenderer _renderer;
        private double _vx = 2.5;
        private double _vy = 1.8;
        private double _stateTimer = 1.8;
        private readonly Random _random = new();

        public override CreatureType CreatureType => CreatureType.BossMutantSpider;
        public override string Name => "👑 Mutant Spider";
        public override double HitRadius => 260.0;
        public override int MaxHealth => 5;
        public override int XpReward => 250;
        public override FrameworkElement VisualElement => _renderer.VisualElement;

        public BossMutantSpider()
        {
            CurrentHealth = MaxHealth;
            _renderer = new SpiderRenderer(scale: 2.5, isGiant: true);
            if (_renderer.VisualElement is Canvas canvas)
            {
                AttachBossHud(canvas, 240, -34);
            }
        }

        public override void ResetToRandomPosition(double screenWidth, double screenHeight)
        {
            if (screenWidth <= 100) screenWidth = 1920;
            if (screenHeight <= 100) screenHeight = 1080;

            X = _random.NextDouble() * (screenWidth - 340) + 170;
            Y = _random.NextDouble() * (screenHeight - 340) + 170;
            DisplayX = X;
            DisplayY = Y;
            ResetHealth();
        }

        public override void Update(double deltaTime, double screenWidth, double screenHeight)
        {
            if (screenWidth <= 100) screenWidth = 1920;
            if (screenHeight <= 100) screenHeight = 1080;

            _stateTimer -= deltaTime;
            if (_stateTimer <= 0)
            {
                double angle = _random.NextDouble() * Math.PI * 2.0;
                // Mutant burst speed
                double speed = (3.2 + _random.NextDouble() * 3.0) * SpeedMultiplier;
                _vx = Math.Cos(angle) * speed;
                _vy = Math.Sin(angle) * speed;
                AngleDegrees = angle * (180.0 / Math.PI);
                _stateTimer = 0.8 + _random.NextDouble() * 2.0;
            }

            X += _vx;
            Y += _vy;

            const double margin = 110.0;
            if (X < margin) { X = margin; _vx = Math.Abs(_vx); }
            else if (X > screenWidth - margin) { X = screenWidth - margin; _vx = -Math.Abs(_vx); }
            if (Y < margin) { Y = margin; _vy = Math.Abs(_vy); }
            else if (Y > screenHeight - margin) { Y = screenHeight - margin; _vy = -Math.Abs(_vy); }

            DisplayX = X;
            DisplayY = Y;
            RecordPosition(DisplayX, DisplayY);

            if (_renderer.VisualElement is Canvas c)
            {
                Canvas.SetLeft(c, DisplayX - 120);
                Canvas.SetTop(c, DisplayY - 120);
            }
        }
    }
}
