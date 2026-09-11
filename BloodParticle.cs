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

        public BloodParticle(double x, double y, double radius, Color color, double opacity, Point impactCenter, bool animateSplash = true)
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

            if (animateSplash)
            {
                // Splash burst animation from impact center towards target offset
                double startX = impactCenter.X + (x - impactCenter.X) * 0.25;
                double startY = impactCenter.Y + (y - impactCenter.Y) * 0.25;

                Canvas.SetLeft(VisualShape, startX - radius);
                Canvas.SetTop(VisualShape, startY - radius);

                var xAnim = new DoubleAnimation(startX - radius, x - radius, TimeSpan.FromMilliseconds(180))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var yAnim = new DoubleAnimation(startY - radius, y - radius, TimeSpan.FromMilliseconds(180))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                VisualShape.BeginAnimation(Canvas.LeftProperty, xAnim);
                VisualShape.BeginAnimation(Canvas.TopProperty, yAnim);
            }
            else
            {
                Canvas.SetLeft(VisualShape, X - Radius);
                Canvas.SetTop(VisualShape, Y - Radius);
            }
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

        public void Fade(double amount)
        {
            if (IsCleaned) return;
            Health -= amount;
            if (Health <= 0.05)
            {
                Health = 0;
                VisualShape.Visibility = Visibility.Collapsed;
            }
            else
            {
                VisualShape.Opacity = Health * 0.9;
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
        public BloodConfig CurrentConfig { get; private set; } = BloodConfig.MosquitoPreset;

        private (double width, double height) GetScreenBounds()
        {
            double w = _containerCanvas.ActualWidth > 100 ? _containerCanvas.ActualWidth : SystemParameters.PrimaryScreenWidth;
            double h = _containerCanvas.ActualHeight > 100 ? _containerCanvas.ActualHeight : SystemParameters.PrimaryScreenHeight;
            if (w <= 100) w = 1920;
            if (h <= 100) h = 1080;
            return (w, h);
        }

        private static void ClampParticleCoord(ref double px, ref double py, double radius, double screenW, double screenH)
        {
            double minX = radius + 6.0;
            double maxX = screenW - radius - 6.0;
            double minY = radius + 6.0;
            double maxY = screenH - radius - 6.0;

            // Soft-bounce splatter droplets if they would spray beyond physical monitor edge
            if (px < minX) px = minX + (minX - px) * 0.35;
            else if (px > maxX) px = maxX - (px - maxX) * 0.35;

            if (py < minY) py = minY + (minY - py) * 0.35;
            else if (py > maxY) py = maxY - (py - maxY) * 0.35;

            // Strict clamp to ensure 100% of particles are reachable by mouse cursor
            px = Math.Clamp(px, minX, maxX);
            py = Math.Clamp(py, minY, maxY);
        }

        public int RemainingUncleanedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _particles.Count; i++)
                {
                    if (!_particles[i].IsCleaned) count++;
                }
                return count;
            }
        }

        public double CleanProgress
        {
            get
            {
                if (_particles.Count == 0) return 1.0;
                var (screenW, screenH) = GetScreenBounds();
                int totalValid = 0;
                int cleaned = 0;

                for (int i = 0; i < _particles.Count; i++)
                {
                    var p = _particles[i];

                    // Failsafe: If particle somehow ended up off-screen, auto-mark cleaned
                    if (p.X < 0 || p.X > screenW || p.Y < 0 || p.Y > screenH)
                    {
                        p.Health = 0;
                        p.VisualShape.Visibility = Visibility.Collapsed;
                        cleaned++;
                        totalValid++;
                        continue;
                    }

                    totalValid++;
                    if (p.IsCleaned) cleaned++;
                }

                if (totalValid == 0) return 1.0;
                return (double)cleaned / totalValid;
            }
        }

        public BloodSplatterSystem(Canvas containerCanvas)
        {
            _containerCanvas = containerCanvas;
        }

        public void SpawnSplatter(Point location, BloodConfig? config = null)
        {
            Clear();

            var (screenW, screenH) = GetScreenBounds();

            CurrentConfig = config ?? BloodConfig.MosquitoPreset;
            Center = new Point(
                Math.Clamp(location.X, 20, screenW - 20),
                Math.Clamp(location.Y, 20, screenH - 20)
            );
            SplatterRadius = CurrentConfig.SplatterRadius;
            IsActive = true;

            // Crimson / dark blood palette
            Color darkClot = Color.FromRgb(95, 0, 0);
            Color deepRed = Color.FromRgb(140, 8, 8);
            Color scarletRed = Color.FromRgb(185, 14, 14);
            Color brightRed = Color.FromRgb(225, 25, 25);
            Color specularWhite = Color.FromArgb(200, 255, 235, 235);

            int totalParticles = _random.Next(CurrentConfig.MinParticles, CurrentConfig.MaxParticles + 1);

            // 1. Central thick impact clots (heavy overlapping pooling blood)
            int clotCount = CurrentConfig.CreatureType == CreatureType.Mosquito ? 10 :
                           (CurrentConfig.CreatureType == CreatureType.Spider ? 24 : 45);

            for (int i = 0; i < clotCount; i++)
            {
                double offsetAngle = _random.NextDouble() * Math.PI * 2;
                double maxOffset = CurrentConfig.CreatureType == CreatureType.Mosquito ? 14.0 :
                                   (CurrentConfig.CreatureType == CreatureType.Spider ? 32.0 : 55.0);
                double offsetDist = _random.NextDouble() * maxOffset;

                double px = Center.X + Math.Cos(offsetAngle) * offsetDist;
                double py = Center.Y + Math.Sin(offsetAngle) * offsetDist;

                double minRad = CurrentConfig.MinDropletRadius * 2.2;
                double maxRad = CurrentConfig.MaxDropletRadius * 2.0;
                double rad = minRad + _random.NextDouble() * (maxRad - minRad);

                ClampParticleCoord(ref px, ref py, rad, screenW, screenH);

                var p = new BloodParticle(px, py, rad, (i % 2 == 0) ? darkClot : deepRed, 0.95, Center);
                _particles.Add(p);
                _containerCanvas.Children.Add(p.VisualShape);
            }

            // Arterial splatter burst streaks / spurts (juicy arcade gore trails)
            int spurtCount = CurrentConfig.CreatureType == CreatureType.Mosquito ? 5 :
                            (CurrentConfig.CreatureType == CreatureType.Spider ? 10 : 18);
            for (int s = 0; s < spurtCount; s++)
            {
                double spurtAngle = _random.NextDouble() * Math.PI * 2;
                double maxDist = CurrentConfig.SplatterRadius * (0.4 + _random.NextDouble() * 0.6);
                int trailDots = 3 + _random.Next(4);
                for (int t = 1; t <= trailDots; t++)
                {
                    double frac = (double)t / trailDots;
                    double px = Center.X + Math.Cos(spurtAngle) * (maxDist * frac);
                    double py = Center.Y + Math.Sin(spurtAngle) * (maxDist * frac);
                    double rad = Math.Max(1.5, CurrentConfig.MinDropletRadius * (1.3 - frac * 0.4));

                    ClampParticleCoord(ref px, ref py, rad, screenW, screenH);

                    var p = new BloodParticle(px, py, rad, (t % 2 == 0) ? scarletRed : deepRed, 0.90, Center);
                    _particles.Add(p);
                    _containerCanvas.Children.Add(p.VisualShape);
                }
            }

            // 2. Creature-specific squashed corpse remains in center
            if (CurrentConfig.CreatureType == CreatureType.Mosquito)
            {
                AddSquashedMosquito(Center, screenW, screenH);
            }
            else
            {
                AddSquashedSpider(Center, CurrentConfig.CreatureType == CreatureType.GiantSpider, screenW, screenH);
            }

            // 3. Radial splatter droplets (medium & large droplets)
            int dropletCount = (int)(totalParticles * 0.55);
            for (int i = 0; i < dropletCount; i++)
            {
                double angle = _random.NextDouble() * Math.PI * 2;
                double dist = 12.0 + Math.Pow(_random.NextDouble(), 1.4) * (CurrentConfig.SplatterRadius * 0.75);
                double px = Center.X + Math.Cos(angle) * dist;
                double py = Center.Y + Math.Sin(angle) * dist;

                double rad = CurrentConfig.MinDropletRadius + _random.NextDouble() * (CurrentConfig.MaxDropletRadius - CurrentConfig.MinDropletRadius);

                ClampParticleCoord(ref px, ref py, rad, screenW, screenH);

                Color c = (_random.Next(3) == 0) ? scarletRed : deepRed;
                var p = new BloodParticle(px, py, rad, c, 0.88, Center);
                _particles.Add(p);
                _containerCanvas.Children.Add(p.VisualShape);
            }

            // 4. Fine spray specks and satellite perimeter dots
            int fineCount = totalParticles - clotCount - dropletCount;
            for (int i = 0; i < fineCount; i++)
            {
                double angle = _random.NextDouble() * Math.PI * 2;
                double dist = 18.0 + _random.NextDouble() * CurrentConfig.SplatterRadius;
                double px = Center.X + Math.Cos(angle) * dist;
                double py = Center.Y + Math.Sin(angle) * dist;

                double rad = 1.2 + _random.NextDouble() * (CurrentConfig.MinDropletRadius * 1.1);

                ClampParticleCoord(ref px, ref py, rad, screenW, screenH);

                Color c = (_random.Next(2) == 0) ? brightRed : scarletRed;
                var p = new BloodParticle(px, py, rad, c, 0.78, Center);
                _particles.Add(p);
                _containerCanvas.Children.Add(p.VisualShape);
            }

            // 5. Specular liquid shine highlights
            int specularCount = CurrentConfig.CreatureType == CreatureType.Mosquito ? 3 : 7;
            for (int i = 0; i < specularCount; i++)
            {
                double angle = _random.NextDouble() * Math.PI * 2;
                double dist = 3.0 + _random.NextDouble() * (CurrentConfig.SplatterRadius * 0.25);
                double px = Center.X + Math.Cos(angle) * dist;
                double py = Center.Y + Math.Sin(angle) * dist;
                double rad = 1.8 + _random.NextDouble() * 2.8;

                ClampParticleCoord(ref px, ref py, rad, screenW, screenH);

                var p = new BloodParticle(px, py, rad, specularWhite, 0.68, Center);
                _particles.Add(p);
                _containerCanvas.Children.Add(p.VisualShape);
            }
        }

        private void AddSquashedMosquito(Point location, double screenW, double screenH)
        {
            var squashedGroup = new Path
            {
                Data = Geometry.Parse("M -4,-2 L -12,-8 M -3,0 L -14,2 M -2,4 L -10,12 M 4,-2 L 12,-8 M 3,0 L 14,2 M 2,4 L 10,12 M -2,-6 L 2,-6 L 2,6 L -2,6 Z"),
                Stroke = new SolidColorBrush(Color.FromArgb(195, 20, 10, 10)),
                StrokeThickness = 1.5,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(squashedGroup, Math.Clamp(location.X, 20, screenW - 20));
            Canvas.SetTop(squashedGroup, Math.Clamp(location.Y, 20, screenH - 20));
            _containerCanvas.Children.Add(squashedGroup);
            _staticEffects.Add(squashedGroup);
        }

        private void AddSquashedSpider(Point location, bool isGiant, double screenW, double screenH)
        {
            double scale = isGiant ? 1.9 : 1.2;
            var squashedGroup = new Path
            {
                // Curled crumpled 8 spider legs & crushed cephalothorax
                Data = Geometry.Parse("M -3,-3 L -14,-12 M -5,0 L -18,-2 M -4,4 L -16,10 M -2,7 L -12,18 " +
                                      "M 3,-3 L 14,-12 M 5,0 L 18,-2 M 4,4 L 16,10 M 2,7 L 12,18 " +
                                      "M -5,-5 Q 0,-8 5,-5 Q 8,4 0,7 Q -8,4 -5,-5 Z"),
                Stroke = new SolidColorBrush(Color.FromArgb(210, 15, 8, 8)),
                Fill = new SolidColorBrush(Color.FromArgb(180, 25, 10, 10)),
                StrokeThickness = scale * 1.5,
                RenderTransform = new ScaleTransform(scale, scale),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(squashedGroup, Math.Clamp(location.X, 25, screenW - 25));
            Canvas.SetTop(squashedGroup, Math.Clamp(location.Y, 25, screenH - 25));
            _containerCanvas.Children.Add(squashedGroup);
            _staticEffects.Add(squashedGroup);
        }

        public bool IsOverSplatter(Point point)
        {
            if (!IsActive) return false;
            double dx = point.X - Center.X;
            double dy = point.Y - Center.Y;
            if ((dx * dx + dy * dy) <= (SplatterRadius * SplatterRadius))
                return true;

            // Proximity check to any active uncleaned particle
            for (int i = 0; i < _particles.Count; i++)
            {
                if (!_particles[i].IsCleaned)
                {
                    double px = _particles[i].X - point.X;
                    double py = _particles[i].Y - point.Y;
                    if ((px * px + py * py) <= (75.0 * 75.0))
                        return true;
                }
            }
            return false;
        }

        public bool WipeAt(Point cursorPoint, double wipeRadius = 38.0, double power = 0.28)
        {
            if (!IsActive) return false;

            var (screenW, screenH) = GetScreenBounds();

            // When user rubs cursor along screen edges, expand reach so edge particles clean easily
            bool isNearEdge = cursorPoint.X <= 30 || cursorPoint.X >= screenW - 30 ||
                              cursorPoint.Y <= 30 || cursorPoint.Y >= screenH - 30;
            double effectiveRadius = isNearEdge ? wipeRadius * 1.5 : wipeRadius;

            // Apply resistance modifier from creature blood config
            double effectivePower = power / Math.Max(0.5, CurrentConfig.WipeResistance);

            bool wipedAny = false;
            double wipeRadiusSq = effectiveRadius * effectiveRadius;

            for (int i = 0; i < _particles.Count; i++)
            {
                var p = _particles[i];
                if (p.IsCleaned) continue;

                double dx = p.X - cursorPoint.X;
                double dy = p.Y - cursorPoint.Y;
                double distSq = dx * dx + dy * dy;

                if (distSq <= wipeRadiusSq)
                {
                    double falloff = 1.0 - (Math.Sqrt(distSq) / effectiveRadius);
                    p.Wipe(effectivePower * (0.5 + falloff * 0.5));
                    wipedAny = true;
                }
            }

            // Diminish static crushed remnants proportionally
            double progress = CleanProgress;
            for (int i = 0; i < _staticEffects.Count; i++)
            {
                _staticEffects[i].Opacity = Math.Max(0, 1.0 - progress * 1.15);
            }

            return wipedAny;
        }

        public void UpdateFade(double deltaTime)
        {
            if (!IsActive || !CurrentConfig.FadeOverTime) return;

            double fadeRate = 0.02 * deltaTime; // Slow subtle fade
            for (int i = 0; i < _particles.Count; i++)
            {
                _particles[i].Fade(fadeRate);
            }
        }

        public void DissolveRemaining()
        {
            if (!IsActive) return;

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
