using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace DigitalMosquito
{
    public class BloodParticle
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Radius { get; set; }
        public double OriginalRadius { get; set; }
        public double Health { get; set; } = 1.0;
        public bool IsCleaned => Health <= 0.05;

        public Shape VisualShape { get; }

        public BloodParticle(double x, double y, double radius, Color color, double opacity)
        {
            X = x;
            Y = y;
            Radius = radius;
            OriginalRadius = radius;

            VisualShape = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = new SolidColorBrush(color),
                Opacity = opacity,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(VisualShape, X - Radius);
            Canvas.SetTop(VisualShape, Y - Radius);
        }

        public void Wipe(double power)
        {
            if (IsCleaned) return;

            Health -= power;
            if (Health < 0) Health = 0;

            VisualShape.Opacity = Math.Max(0, Health * 0.9);
            double currentRadius = Math.Max(1.0, OriginalRadius * Math.Sqrt(Health));
            VisualShape.Width = currentRadius * 2;
            VisualShape.Height = currentRadius * 2;
            Canvas.SetLeft(VisualShape, X - currentRadius);
            Canvas.SetTop(VisualShape, Y - currentRadius);

            if (IsCleaned)
            {
                VisualShape.Visibility = Visibility.Collapsed;
            }
        }
    }

    public class BloodSplatterSystem
    {
        private readonly Canvas _containerCanvas;
        private readonly Random _random = new();

        private readonly List<BloodParticle> _particles = new();
        private readonly List<Shape> _staticEffects = new();

        public Point Center { get; private set; }
        public double SplatterRadius { get; private set; } = 110.0;
        public bool IsActive { get; private set; } = false;

        public double CleanProgress
        {
            get
            {
                if (_particles.Count == 0) return 1.0;
                int cleaned = 0;
                for (int i = 0; i < _particles.Count; i++)
                {
                    if (_particles[i].IsCleaned) cleaned++;
                }
                return (double)cleaned / _particles.Count;
            }
        }

        public BloodSplatterSystem(Canvas containerCanvas)
        {
            _containerCanvas = containerCanvas;
        }

        public void SpawnSplatter(Point location)
        {
            Clear();

            Center = location;
            IsActive = true;

            // Crimson / blood color palette
            Color darkClot = Color.FromRgb(100, 0, 0);
            Color deepRed = Color.FromRgb(140, 8, 8);
            Color scarletRed = Color.FromRgb(180, 15, 15);
            Color brightRed = Color.FromRgb(220, 25, 25);
            Color specularWhite = Color.FromArgb(200, 255, 235, 235);

            // 1. Central thick impact clots (overlapping pooling blood)
            for (int i = 0; i < 7; i++)
            {
                double offsetAngle = _random.NextDouble() * Math.PI * 2;
                double offsetDist = _random.NextDouble() * 12.0;
                double px = location.X + Math.Cos(offsetAngle) * offsetDist;
                double py = location.Y + Math.Sin(offsetAngle) * offsetDist;
                double rad = 10.0 + _random.NextDouble() * 14.0;

                var p = new BloodParticle(px, py, rad, (i % 2 == 0) ? darkClot : deepRed, 0.92);
                _particles.Add(p);
                _containerCanvas.Children.Add(p.VisualShape);
            }

            // 2. Squashed mosquito squib / remains in center
            AddSquashedMosquito(location);

            // 3. Medium radial splash droplets
            int mediumCount = 28;
            for (int i = 0; i < mediumCount; i++)
            {
                double angle = _random.NextDouble() * Math.PI * 2;
                double dist = 14.0 + Math.Pow(_random.NextDouble(), 1.5) * 65.0;
                double px = location.X + Math.Cos(angle) * dist;
                double py = location.Y + Math.Sin(angle) * dist;
                double rad = 3.5 + _random.NextDouble() * 5.5;

                Color c = (_random.Next(3) == 0) ? scarletRed : deepRed;
                var p = new BloodParticle(px, py, rad, c, 0.85);
                _particles.Add(p);
                _containerCanvas.Children.Add(p.VisualShape);
            }

            // 4. Fine spray specks and satellite dots
            int fineCount = 35;
            for (int i = 0; i < fineCount; i++)
            {
                double angle = _random.NextDouble() * Math.PI * 2;
                double dist = 20.0 + _random.NextDouble() * 85.0;
                double px = location.X + Math.Cos(angle) * dist;
                double py = location.Y + Math.Sin(angle) * dist;
                double rad = 1.2 + _random.NextDouble() * 2.5;

                Color c = (_random.Next(2) == 0) ? brightRed : scarletRed;
                var p = new BloodParticle(px, py, rad, c, 0.75);
                _particles.Add(p);
                _containerCanvas.Children.Add(p.VisualShape);
            }

            // 5. Specular liquid shine highlights
            for (int i = 0; i < 4; i++)
            {
                double angle = _random.NextDouble() * Math.PI * 2;
                double dist = 3.0 + _random.NextDouble() * 10.0;
                double px = location.X + Math.Cos(angle) * dist;
                double py = location.Y + Math.Sin(angle) * dist;
                double rad = 2.0 + _random.NextDouble() * 2.5;

                var p = new BloodParticle(px, py, rad, specularWhite, 0.65);
                _particles.Add(p);
                _containerCanvas.Children.Add(p.VisualShape);
            }
        }

        private void AddSquashedMosquito(Point location)
        {
            // Tiny crushed black legs and body silhouette in the clot
            var squashedGroup = new Path
            {
                Data = Geometry.Parse("M -4,-2 L -12,-8 M -3,0 L -14,2 M -2,4 L -10,12 M 4,-2 L 12,-8 M 3,0 L 14,2 M 2,4 L 10,12 M -2,-6 L 2,-6 L 2,6 L -2,6 Z"),
                Stroke = new SolidColorBrush(Color.FromArgb(190, 20, 10, 10)),
                StrokeThickness = 1.5,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(squashedGroup, location.X);
            Canvas.SetTop(squashedGroup, location.Y);
            _containerCanvas.Children.Add(squashedGroup);
            _staticEffects.Add(squashedGroup);
        }

        public bool IsOverSplatter(Point point)
        {
            if (!IsActive) return false;
            double dx = point.X - Center.X;
            double dy = point.Y - Center.Y;
            return (dx * dx + dy * dy) <= (SplatterRadius * SplatterRadius);
        }

        public bool WipeAt(Point cursorPoint, double wipeRadius = 32.0, double power = 0.28)
        {
            if (!IsActive) return false;

            bool wipedAny = false;
            double wipeRadiusSq = wipeRadius * wipeRadius;

            for (int i = 0; i < _particles.Count; i++)
            {
                var p = _particles[i];
                if (p.IsCleaned) continue;

                double dx = p.X - cursorPoint.X;
                double dy = p.Y - cursorPoint.Y;
                double distSq = dx * dx + dy * dy;

                if (distSq <= wipeRadiusSq)
                {
                    // Closer particles get wiped faster
                    double falloff = 1.0 - (Math.Sqrt(distSq) / wipeRadius);
                    p.Wipe(power * (0.5 + falloff * 0.5));
                    wipedAny = true;
                }
            }

            // Diminish static crushed remnants proportionally
            double progress = CleanProgress;
            for (int i = 0; i < _staticEffects.Count; i++)
            {
                _staticEffects[i].Opacity = Math.Max(0, 1.0 - progress * 1.1);
            }

            return wipedAny;
        }

        public void DissolveRemaining()
        {
            if (!IsActive) return;

            // Fade out everything smoothly
            for (int i = 0; i < _particles.Count; i++)
            {
                _particles[i].VisualShape.Visibility = Visibility.Collapsed;
            }
            for (int i = 0; i < _staticEffects.Count; i++)
            {
                _staticEffects[i].Visibility = Visibility.Collapsed;
            }

            Clear();
        }

        public void Clear()
        {
            for (int i = 0; i < _particles.Count; i++)
            {
                _containerCanvas.Children.Remove(_particles[i].VisualShape);
            }
            _particles.Clear();

            for (int i = 0; i < _staticEffects.Count; i++)
            {
                _containerCanvas.Children.Remove(_staticEffects[i]);
            }
            _staticEffects.Clear();

            IsActive = false;
        }
    }
}
