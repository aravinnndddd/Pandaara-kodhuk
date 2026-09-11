using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace DigitalMosquito
{
    public class GlassCrackRenderer
    {
        private readonly Canvas _container;
        private readonly Random _random = new();

        public GlassCrackRenderer(Canvas container)
        {
            _container = container;
        }

        public void SpawnCrackAt(Point center, double radius = 120.0)
        {
            var crackGroup = new Canvas
            {
                Width = radius * 2,
                Height = radius * 2,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(crackGroup, center.X - radius);
            Canvas.SetTop(crackGroup, center.Y - radius);

            var whiteBrush = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255));
            var iceBlueBrush = new SolidColorBrush(Color.FromArgb(160, 200, 235, 255));

            // 1. Central impact shattered core
            var coreHole = new Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
                Stroke = new SolidColorBrush(Color.FromArgb(250, 180, 220, 255)),
                StrokeThickness = 2
            };
            Canvas.SetLeft(coreHole, radius - 7);
            Canvas.SetTop(coreHole, radius - 7);
            crackGroup.Children.Add(coreHole);

            // 2. Radial jagged fracture fissures radiating outward
            int numSpokes = 8 + _random.Next(5);
            double angleStep = (Math.PI * 2) / numSpokes;

            for (int i = 0; i < numSpokes; i++)
            {
                double baseAngle = i * angleStep + (_random.NextDouble() - 0.5) * 0.35;
                double spokeLength = radius * (0.65 + _random.NextDouble() * 0.35);

                // Build a jagged zigzag path from center outwards
                var pathGeom = new PathGeometry();
                var figure = new PathFigure { StartPoint = new Point(radius, radius) };

                int segments = 4 + _random.Next(3);
                for (int s = 1; s <= segments; s++)
                {
                    double frac = (double)s / segments;
                    double currentDist = spokeLength * frac;
                    double jitterAngle = baseAngle + (_random.NextDouble() - 0.5) * 0.28;
                    double px = radius + Math.Cos(jitterAngle) * currentDist;
                    double py = radius + Math.Sin(jitterAngle) * currentDist;
                    figure.Segments.Add(new LineSegment(new Point(px, py), true));
                }

                pathGeom.Figures.Add(figure);

                var spokePath = new Path
                {
                    Data = pathGeom,
                    Stroke = (i % 2 == 0) ? whiteBrush : iceBlueBrush,
                    StrokeThickness = (i % 3 == 0) ? 2.2 : 1.4,
                    StrokeLineJoin = PenLineJoin.Miter,
                    StrokeEndLineCap = PenLineCap.Round
                };
                crackGroup.Children.Add(spokePath);
            }

            // 3. Concentric circular/web fracture rings
            for (int r = 1; r <= 3; r++)
            {
                double ringRadius = radius * (0.28 * r + (_random.NextDouble() - 0.5) * 0.08);
                var ringGeom = new PathGeometry();
                var ringFig = new PathFigure();

                bool isFirst = true;
                for (int i = 0; i <= numSpokes; i++)
                {
                    double a = i * angleStep;
                    double jRad = ringRadius + (_random.NextDouble() - 0.5) * 8.0;
                    double px = radius + Math.Cos(a) * jRad;
                    double py = radius + Math.Sin(a) * jRad;

                    if (isFirst)
                    {
                        ringFig.StartPoint = new Point(px, py);
                        isFirst = false;
                    }
                    else
                    {
                        ringFig.Segments.Add(new LineSegment(new Point(px, py), true));
                    }
                }

                ringGeom.Figures.Add(ringFig);

                var ringPath = new Path
                {
                    Data = ringGeom,
                    Stroke = iceBlueBrush,
                    StrokeThickness = 1.2,
                    StrokeLineJoin = PenLineJoin.Round
                };
                crackGroup.Children.Add(ringPath);
            }

            // Attach to container
            _container.Children.Add(crackGroup);

            // Animate opacity fade out over 2.2 seconds
            var fadeAnim = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(2200))
            {
                BeginTime = TimeSpan.FromMilliseconds(600)
            };
            fadeAnim.Completed += (s, e) =>
            {
                _container.Children.Remove(crackGroup);
            };

            crackGroup.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
        }
    }
}
