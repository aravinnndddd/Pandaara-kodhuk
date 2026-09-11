using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace DigitalMosquito
{
    public class WeaponAnimationManager
    {
        private readonly Canvas _weaponCanvas;
        private readonly Random _random = new();

        public WeaponAnimationManager(Canvas weaponCanvas)
        {
            _weaponCanvas = weaponCanvas;
        }

        public void PlayAttack(
            string weaponId,
            Point targetPoint,
            Creature? targetCreature,
            Action onImpact,
            Action onFinished)
        {
            switch (weaponId.ToLowerInvariant())
            {
                case "hand":
                    PlayHandSlap(targetPoint, onImpact, onFinished);
                    break;
                case "swatter":
                    PlayFlySwatter(targetPoint, onImpact, onFinished);
                    break;
                case "newspaper":
                    PlayNewspaper(targetPoint, onImpact, onFinished);
                    break;
                case "slipper":
                    PlaySlipper(targetPoint, false, onImpact, onFinished);
                    break;
                case "bat":
                    PlayBat(targetPoint, onImpact, onFinished);
                    break;
                case "hammer":
                    PlayHammer(targetPoint, onImpact, onFinished);
                    break;
                case "vacuum":
                    PlayVacuum(targetPoint, targetCreature, onImpact, onFinished);
                    break;
                case "washer":
                    PlayWaterWasher(targetPoint, onImpact, onFinished);
                    break;
                case "electric":
                    PlayElectricSwatter(targetPoint, onImpact, onFinished);
                    break;
                case "giant_slipper":
                    PlaySlipper(targetPoint, true, onImpact, onFinished);
                    break;
                case "boss_mode":
                default:
                    PlayBossScepter(targetPoint, onImpact, onFinished);
                    break;
            }
        }

        #region Weapon 1: Hand 👋

        private void PlayHandSlap(Point target, Action onImpact, Action onFinished)
        {
            var handCanvas = new Canvas { Width = 70, Height = 70, IsHitTestVisible = false };

            // Vector hand with palm and fingers
            var palm = new Path
            {
                Data = Geometry.Parse("M 25,60 C 15,55 10,40 12,28 C 15,16 28,12 38,12 C 48,12 58,18 60,30 C 62,42 55,56 45,62 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(255, 218, 185)),
                Stroke = new SolidColorBrush(Color.FromRgb(210, 140, 100)),
                StrokeThickness = 2
            };
            // 4 fingers + thumb
            var fingers = new Path
            {
                Data = Geometry.Parse("M 20,25 C 18,10 24,4 28,4 C 32,4 34,10 32,24 M 32,22 C 32,6 38,2 42,2 C 46,2 48,8 46,22 M 46,24 C 47,8 52,5 56,5 C 60,5 61,12 58,25 M 58,28 C 60,18 64,15 67,15 C 70,15 70,22 66,32 M 14,36 C 4,32 0,38 4,44 C 8,48 16,46 16,40"),
                Stroke = new SolidColorBrush(Color.FromRgb(210, 140, 100)),
                StrokeThickness = 2.5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            handCanvas.Children.Add(palm);
            handCanvas.Children.Add(fingers);

            var transformGroup = new TransformGroup();
            var scale = new ScaleTransform(1.4, 1.4, 35, 35);
            var rotate = new RotateTransform(-35, 35, 35);
            var translate = new TranslateTransform(-20, -25);
            transformGroup.Children.Add(scale);
            transformGroup.Children.Add(rotate);
            transformGroup.Children.Add(translate);
            handCanvas.RenderTransform = transformGroup;

            Canvas.SetLeft(handCanvas, target.X - 35);
            Canvas.SetTop(handCanvas, target.Y - 35);
            _weaponCanvas.Children.Add(handCanvas);

            // Fast slap animation: rotate and snap in 85ms
            var slapAnim = new DoubleAnimation(-35, 12, TimeSpan.FromMilliseconds(85))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            slapAnim.Completed += (s, e) =>
            {
                onImpact?.Invoke();
                SpawnImpactSparks(target, 6, Color.FromRgb(255, 230, 100));

                var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(70));
                fade.Completed += (fs, fe) =>
                {
                    _weaponCanvas.Children.Remove(handCanvas);
                    onFinished?.Invoke();
                };
                handCanvas.BeginAnimation(UIElement.OpacityProperty, fade);
            };

            rotate.BeginAnimation(RotateTransform.AngleProperty, slapAnim);
        }

        #endregion

        #region Weapon 2: Fly Swatter 🪰

        private void PlayFlySwatter(Point target, Action onImpact, Action onFinished)
        {
            var swatterCanvas = new Canvas { Width = 90, Height = 130, IsHitTestVisible = false };

            // Swatter handle
            var handle = new Rectangle
            {
                Width = 6,
                Height = 85,
                Fill = new SolidColorBrush(Color.FromRgb(40, 120, 220)),
                RadiusX = 3,
                RadiusY = 3
            };
            Canvas.SetLeft(handle, 42);
            Canvas.SetTop(handle, 45);
            swatterCanvas.Children.Add(handle);

            // Mesh grid head
            var head = new Rectangle
            {
                Width = 50,
                Height = 45,
                Fill = new SolidColorBrush(Color.FromArgb(140, 60, 160, 255)),
                Stroke = new SolidColorBrush(Color.FromRgb(30, 100, 220)),
                StrokeThickness = 3,
                RadiusX = 6,
                RadiusY = 6
            };
            Canvas.SetLeft(head, 20);
            Canvas.SetTop(head, 0);
            swatterCanvas.Children.Add(head);

            // Grid lines
            var gridLines = new Path
            {
                Data = Geometry.Parse("M 32,2 L 32,43 M 45,2 L 45,43 M 58,2 L 58,43 M 22,12 L 68,12 M 22,23 L 68,23 M 22,34 L 68,34"),
                Stroke = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                StrokeThickness = 1.5
            };
            swatterCanvas.Children.Add(gridLines);

            var rotate = new RotateTransform(-55, 45, 120);
            swatterCanvas.RenderTransform = rotate;

            Canvas.SetLeft(swatterCanvas, target.X - 45);
            Canvas.SetTop(swatterCanvas, target.Y - 110);
            _weaponCanvas.Children.Add(swatterCanvas);

            // Ultra snappy 55ms snap
            var snapAnim = new DoubleAnimation(-55, 10, TimeSpan.FromMilliseconds(55))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            snapAnim.Completed += (s, e) =>
            {
                onImpact?.Invoke();
                SpawnImpactSparks(target, 8, Color.FromRgb(100, 200, 255));

                var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(65));
                fade.Completed += (fs, fe) =>
                {
                    _weaponCanvas.Children.Remove(swatterCanvas);
                    onFinished?.Invoke();
                };
                swatterCanvas.BeginAnimation(UIElement.OpacityProperty, fade);
            };

            rotate.BeginAnimation(RotateTransform.AngleProperty, snapAnim);
        }

        #endregion

        #region Weapon 3: Newspaper 📰

        private void PlayNewspaper(Point target, Action onImpact, Action onFinished)
        {
            var paperCanvas = new Canvas { Width = 110, Height = 140, IsHitTestVisible = false };

            // Rolled paper cylinder
            var roll = new Rectangle
            {
                Width = 24,
                Height = 110,
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(240, 240, 235), 0.0),
                        new GradientStop(Color.FromRgb(210, 210, 200), 0.5),
                        new GradientStop(Color.FromRgb(160, 160, 150), 1.0)
                    }
                },
                Stroke = new SolidColorBrush(Color.FromRgb(120, 120, 110)),
                StrokeThickness = 1.5,
                RadiusX = 5,
                RadiusY = 5
            };
            Canvas.SetLeft(roll, 43);
            Canvas.SetTop(roll, 10);
            paperCanvas.Children.Add(roll);

            // Print text stripes
            var stripes = new Path
            {
                Data = Geometry.Parse("M 46,25 L 64,25 M 46,38 L 64,38 M 46,55 L 64,55 M 46,70 L 64,70 M 46,85 L 64,85"),
                Stroke = new SolidColorBrush(Color.FromArgb(140, 40, 40, 40)),
                StrokeThickness = 2.5
            };
            paperCanvas.Children.Add(stripes);

            // Rubber band
            var band = new Rectangle { Width = 26, Height = 6, Fill = Brushes.Red };
            Canvas.SetLeft(band, 42);
            Canvas.SetTop(band, 60);
            paperCanvas.Children.Add(band);

            var rotate = new RotateTransform(-65, 55, 120);
            paperCanvas.RenderTransform = rotate;

            Canvas.SetLeft(paperCanvas, target.X - 55);
            Canvas.SetTop(paperCanvas, target.Y - 110);
            _weaponCanvas.Children.Add(paperCanvas);

            var swingAnim = new DoubleAnimation(-65, 15, TimeSpan.FromMilliseconds(75))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            swingAnim.Completed += (s, e) =>
            {
                onImpact?.Invoke();
                SpawnImpactSparks(target, 10, Color.FromRgb(240, 240, 200));

                var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(70));
                fade.Completed += (fs, fe) =>
                {
                    _weaponCanvas.Children.Remove(paperCanvas);
                    onFinished?.Invoke();
                };
                paperCanvas.BeginAnimation(UIElement.OpacityProperty, fade);
            };

            rotate.BeginAnimation(RotateTransform.AngleProperty, swingAnim);
        }

        #endregion

        #region Weapon 4 & 9: Slipper 🩴 & Giant Slipper

        private void PlaySlipper(Point target, bool isGiant, Action onImpact, Action onFinished)
        {
            double scale = isGiant ? 3.0 : 1.25;
            var slipperCanvas = new Canvas { Width = 80 * scale, Height = 140 * scale, IsHitTestVisible = false };

            // Flip-flop chancla sole
            var sole = new Path
            {
                Data = Geometry.Parse("M 25,130 C 10,120 5,70 12,35 C 18,10 40,2 52,2 C 68,2 75,15 72,45 C 70,80 62,120 48,130 Z"),
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(230, 40, 70), 0.0), // Classic chancla pink/red
                        new GradientStop(Color.FromRgb(160, 20, 50), 1.0)
                    }
                },
                Stroke = new SolidColorBrush(Color.FromRgb(255, 240, 100)),
                StrokeThickness = 3
            };
            sole.LayoutTransform = new ScaleTransform(scale, scale);
            slipperCanvas.Children.Add(sole);

            // Strap V-band
            var strap = new Path
            {
                Data = Geometry.Parse("M 20,45 Q 42,20 64,45 M 42,20 L 42,12"),
                Stroke = new SolidColorBrush(Color.FromRgb(255, 230, 40)),
                StrokeThickness = 4 * scale,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            strap.LayoutTransform = new ScaleTransform(scale, scale);
            slipperCanvas.Children.Add(strap);

            var transformGroup = new TransformGroup();
            var scaleAnim = new ScaleTransform(1.0, 1.0, (80 * scale) / 2.0, (140 * scale) / 2.0);
            var translate = new TranslateTransform(0, -120 * (isGiant ? 1.8 : 1.0));
            transformGroup.Children.Add(scaleAnim);
            transformGroup.Children.Add(translate);
            slipperCanvas.RenderTransform = transformGroup;

            Canvas.SetLeft(slipperCanvas, target.X - (40 * scale));
            Canvas.SetTop(slipperCanvas, target.Y - (70 * scale));
            _weaponCanvas.Children.Add(slipperCanvas);

            // Fast vertical slam down
            var dropAnim = new DoubleAnimation(-120 * (isGiant ? 1.8 : 1.0), 0, TimeSpan.FromMilliseconds(isGiant ? 110 : 80))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            dropAnim.Completed += (s, e) =>
            {
                onImpact?.Invoke();
                SpawnImpactSparks(target, isGiant ? 24 : 12, Color.FromRgb(255, 80, 80));

                var squashX = new DoubleAnimation(1.0, 1.3, TimeSpan.FromMilliseconds(50)) { AutoReverse = true };
                var squashY = new DoubleAnimation(1.0, 0.7, TimeSpan.FromMilliseconds(50)) { AutoReverse = true };
                scaleAnim.BeginAnimation(ScaleTransform.ScaleXProperty, squashX);
                scaleAnim.BeginAnimation(ScaleTransform.ScaleYProperty, squashY);

                var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(isGiant ? 160 : 90));
                fade.Completed += (fs, fe) =>
                {
                    _weaponCanvas.Children.Remove(slipperCanvas);
                    onFinished?.Invoke();
                };
                slipperCanvas.BeginAnimation(UIElement.OpacityProperty, fade);
            };

            translate.BeginAnimation(TranslateTransform.YProperty, dropAnim);
        }

        #endregion

        #region Weapon 5: Bat 🏏

        private void PlayBat(Point target, Action onImpact, Action onFinished)
        {
            var batCanvas = new Canvas { Width = 140, Height = 140, IsHitTestVisible = false };

            // Wooden bat
            var bat = new Path
            {
                Data = Geometry.Parse("M 20,120 L 30,115 L 95,20 C 102,10 115,16 110,28 L 38,125 Z"),
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(210, 160, 100), 0.0),
                        new GradientStop(Color.FromRgb(160, 110, 60), 1.0)
                    }
                },
                Stroke = new SolidColorBrush(Color.FromRgb(100, 60, 20)),
                StrokeThickness = 2
            };
            batCanvas.Children.Add(bat);

            // Grip wrap
            var grip = new Path
            {
                Data = Geometry.Parse("M 22,118 L 32,104 L 38,107 L 28,121 Z"),
                Fill = Brushes.Black
            };
            batCanvas.Children.Add(grip);

            var rotate = new RotateTransform(-75, 25, 120);
            batCanvas.RenderTransform = rotate;

            Canvas.SetLeft(batCanvas, target.X - 60);
            Canvas.SetTop(batCanvas, target.Y - 110);
            _weaponCanvas.Children.Add(batCanvas);

            // Powerful swing arc across 110ms
            var swingAnim = new DoubleAnimation(-75, 45, TimeSpan.FromMilliseconds(110))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            swingAnim.Completed += (s, e) =>
            {
                onImpact?.Invoke();
                SpawnImpactSparks(target, 16, Color.FromRgb(255, 220, 120));

                var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(90));
                fade.Completed += (fs, fe) =>
                {
                    _weaponCanvas.Children.Remove(batCanvas);
                    onFinished?.Invoke();
                };
                batCanvas.BeginAnimation(UIElement.OpacityProperty, fade);
            };

            rotate.BeginAnimation(RotateTransform.AngleProperty, swingAnim);
        }

        #endregion

        #region Weapon 6: Hammer 🔨

        private void PlayHammer(Point target, Action onImpact, Action onFinished)
        {
            var hammerCanvas = new Canvas { Width = 110, Height = 140, IsHitTestVisible = false };

            // Handle
            var handle = new Rectangle
            {
                Width = 10,
                Height = 90,
                Fill = new SolidColorBrush(Color.FromRgb(180, 120, 60)),
                RadiusX = 3,
                RadiusY = 3
            };
            Canvas.SetLeft(handle, 45);
            Canvas.SetTop(handle, 35);
            hammerCanvas.Children.Add(handle);

            // Heavy Steel Hammer Head
            var head = new Path
            {
                Data = Geometry.Parse("M 20,20 L 75,20 C 85,20 88,14 85,8 L 65,8 C 65,4 35,4 35,8 L 15,8 C 12,14 15,20 20,20 Z"),
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(220, 225, 230), 0.0),
                        new GradientStop(Color.FromRgb(120, 130, 140), 0.5),
                        new GradientStop(Color.FromRgb(60, 70, 80), 1.0)
                    }
                },
                Stroke = new SolidColorBrush(Color.FromRgb(40, 45, 50)),
                StrokeThickness = 2
            };
            Canvas.SetLeft(head, 25);
            Canvas.SetTop(head, 15);
            hammerCanvas.Children.Add(head);

            var rotate = new RotateTransform(-70, 50, 120);
            hammerCanvas.RenderTransform = rotate;

            Canvas.SetLeft(hammerCanvas, target.X - 50);
            Canvas.SetTop(hammerCanvas, target.Y - 110);
            _weaponCanvas.Children.Add(hammerCanvas);

            var smashAnim = new DoubleAnimation(-70, 10, TimeSpan.FromMilliseconds(95))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            smashAnim.Completed += (s, e) =>
            {
                onImpact?.Invoke();
                SpawnImpactSparks(target, 20, Color.FromRgb(255, 160, 40));

                var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(90));
                fade.Completed += (fs, fe) =>
                {
                    _weaponCanvas.Children.Remove(hammerCanvas);
                    onFinished?.Invoke();
                };
                hammerCanvas.BeginAnimation(UIElement.OpacityProperty, fade);
            };

            rotate.BeginAnimation(RotateTransform.AngleProperty, smashAnim);
        }

        #endregion

        #region Weapon 7: Vacuum 🌀

        private void PlayVacuum(Point target, Creature? creature, Action onImpact, Action onFinished)
        {
            var vacuumCanvas = new Canvas { Width = 120, Height = 120, IsHitTestVisible = false };

            // Vacuum nozzle tube
            var nozzle = new Path
            {
                Data = Geometry.Parse("M 70,10 L 110,40 L 95,58 L 55,28 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(200, 40, 40)),
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            var head = new Ellipse
            {
                Width = 26,
                Height = 36,
                Fill = new SolidColorBrush(Color.FromRgb(30, 30, 35)),
                Stroke = Brushes.Silver,
                StrokeThickness = 2
            };
            Canvas.SetLeft(head, 45);
            Canvas.SetTop(head, 18);
            vacuumCanvas.Children.Add(nozzle);
            vacuumCanvas.Children.Add(head);

            // Swirling suction vortex lines
            var spiral = new Path
            {
                Data = Geometry.Parse("M 50,30 Q 30,50 15,35 Q 0,15 25,0 Q 60,0 55,25"),
                Stroke = new SolidColorBrush(Color.FromArgb(180, 100, 220, 255)),
                StrokeThickness = 3
            };
            vacuumCanvas.Children.Add(spiral);

            Canvas.SetLeft(vacuumCanvas, target.X + 20);
            Canvas.SetTop(vacuumCanvas, target.Y - 40);
            _weaponCanvas.Children.Add(vacuumCanvas);

            // Suction animation: pull creature into the vacuum nozzle over 320ms
            if (creature != null && creature.VisualElement != null)
            {
                var originalTransform = creature.VisualElement.RenderTransform;
                var pullGroup = new TransformGroup();
                var pullTranslate = new TranslateTransform(0, 0);
                var pullScale = new ScaleTransform(1.0, 1.0);
                pullGroup.Children.Add(pullScale);
                pullGroup.Children.Add(pullTranslate);
                creature.VisualElement.RenderTransform = pullGroup;

                var pullX = new DoubleAnimation(0, 35, TimeSpan.FromMilliseconds(300)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
                var pullY = new DoubleAnimation(0, -25, TimeSpan.FromMilliseconds(300)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
                var shrink = new DoubleAnimation(1.0, 0.1, TimeSpan.FromMilliseconds(300));

                shrink.Completed += (s, e) =>
                {
                    creature.VisualElement.RenderTransform = originalTransform;
                    onImpact?.Invoke();

                    var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(100));
                    fade.Completed += (fs, fe) =>
                    {
                        _weaponCanvas.Children.Remove(vacuumCanvas);
                        onFinished?.Invoke();
                    };
                    vacuumCanvas.BeginAnimation(UIElement.OpacityProperty, fade);
                };

                pullTranslate.BeginAnimation(TranslateTransform.XProperty, pullX);
                pullTranslate.BeginAnimation(TranslateTransform.YProperty, pullY);
                pullScale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
                pullScale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
            }
            else
            {
                onImpact?.Invoke();
                _weaponCanvas.Children.Remove(vacuumCanvas);
                onFinished?.Invoke();
            }
        }

        #endregion

        #region Weapon 8: Electric Swatter ⚡

        private void PlayElectricSwatter(Point target, Action onImpact, Action onFinished)
        {
            var electricCanvas = new Canvas { Width = 110, Height = 150, IsHitTestVisible = false };

            // High-tech yellow racket
            var head = new Ellipse
            {
                Width = 70,
                Height = 85,
                Fill = new SolidColorBrush(Color.FromArgb(90, 255, 255, 50)),
                Stroke = new SolidColorBrush(Color.FromRgb(255, 220, 0)),
                StrokeThickness = 4
            };
            head.Effect = new DropShadowEffect { BlurRadius = 16, Color = Colors.Yellow, Opacity = 0.9, ShadowDepth = 0 };
            Canvas.SetLeft(head, 20);
            Canvas.SetTop(head, 5);
            electricCanvas.Children.Add(head);

            var handle = new Rectangle
            {
                Width = 10,
                Height = 65,
                Fill = new SolidColorBrush(Color.FromRgb(40, 40, 45)),
                RadiusX = 4,
                RadiusY = 4
            };
            Canvas.SetLeft(handle, 50);
            Canvas.SetTop(handle, 85);
            electricCanvas.Children.Add(handle);

            // Dynamic lightning arcs
            for (int i = 0; i < 4; i++)
            {
                var bolt = new Polyline
                {
                    Stroke = Brushes.Cyan,
                    StrokeThickness = 2.5
                };
                bolt.Points.Add(new Point(35 + _random.Next(40), 20));
                bolt.Points.Add(new Point(30 + _random.Next(50), 45));
                bolt.Points.Add(new Point(25 + _random.Next(60), 70));
                electricCanvas.Children.Add(bolt);
            }

            var rotate = new RotateTransform(-60, 55, 120);
            electricCanvas.RenderTransform = rotate;

            Canvas.SetLeft(electricCanvas, target.X - 55);
            Canvas.SetTop(electricCanvas, target.Y - 100);
            _weaponCanvas.Children.Add(electricCanvas);

            var swatAnim = new DoubleAnimation(-60, 10, TimeSpan.FromMilliseconds(70))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            swatAnim.Completed += (s, e) =>
            {
                onImpact?.Invoke();
                SpawnImpactSparks(target, 28, Colors.Cyan);
                SpawnImpactSparks(target, 16, Colors.Yellow);

                var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(80));
                fade.Completed += (fs, fe) =>
                {
                    _weaponCanvas.Children.Remove(electricCanvas);
                    onFinished?.Invoke();
                };
                electricCanvas.BeginAnimation(UIElement.OpacityProperty, fade);
            };

            rotate.BeginAnimation(RotateTransform.AngleProperty, swatAnim);
        }

        #endregion

        #region Weapon 10: Boss Scepter / Royal Strike 👑

        private void PlayBossScepter(Point target, Action onImpact, Action onFinished)
        {
            var scepterCanvas = new Canvas { Width = 120, Height = 160, IsHitTestVisible = false };

            // Gold Scepter shaft
            var shaft = new Rectangle
            {
                Width = 10,
                Height = 110,
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(255, 245, 120), 0.0),
                        new GradientStop(Color.FromRgb(215, 175, 20), 0.7),
                        new GradientStop(Color.FromRgb(140, 100, 10), 1.0)
                    }
                },
                RadiusX = 4,
                RadiusY = 4
            };
            Canvas.SetLeft(shaft, 55);
            Canvas.SetTop(shaft, 40);
            scepterCanvas.Children.Add(shaft);

            // Crown Headpiece
            var crown = new TextBlock
            {
                Text = "👑",
                FontSize = 38
            };
            crown.Effect = new DropShadowEffect { BlurRadius = 20, Color = Color.FromRgb(255, 215, 0), Opacity = 0.95, ShadowDepth = 0 };
            Canvas.SetLeft(crown, 40);
            Canvas.SetTop(crown, 5);
            scepterCanvas.Children.Add(crown);

            var rotate = new RotateTransform(-65, 60, 140);
            scepterCanvas.RenderTransform = rotate;

            Canvas.SetLeft(scepterCanvas, target.X - 60);
            Canvas.SetTop(scepterCanvas, target.Y - 120);
            _weaponCanvas.Children.Add(scepterCanvas);

            var strikeAnim = new DoubleAnimation(-65, 12, TimeSpan.FromMilliseconds(90))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            strikeAnim.Completed += (s, e) =>
            {
                onImpact?.Invoke();
                SpawnImpactSparks(target, 32, Color.FromRgb(255, 215, 0));
                SpawnImpactSparks(target, 16, Color.FromRgb(255, 50, 80));

                var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(100));
                fade.Completed += (fs, fe) =>
                {
                    _weaponCanvas.Children.Remove(scepterCanvas);
                    onFinished?.Invoke();
                };
                scepterCanvas.BeginAnimation(UIElement.OpacityProperty, fade);
            };

            rotate.BeginAnimation(RotateTransform.AngleProperty, strikeAnim);
        }

        #endregion

        #region Weapon: Water Washer / Water Wiper 🌊

        private void PlayWaterWasher(Point target, Action onImpact, Action onFinished)
        {
            var washerCanvas = new Canvas { Width = 150, Height = 150, IsHitTestVisible = false };

            // High-pressure nozzle / sprayer pipe
            var handle = new Rectangle
            {
                Width = 8,
                Height = 85,
                Fill = new SolidColorBrush(Color.FromRgb(30, 144, 255)), // Dodger blue
                RadiusX = 4,
                RadiusY = 4
            };
            Canvas.SetLeft(handle, 71);
            Canvas.SetTop(handle, 50);
            washerCanvas.Children.Add(handle);

            // Wiper / Squeegee blade bar
            var bladeBar = new Rectangle
            {
                Width = 100,
                Height = 10,
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(0, 180, 255), 0.0),
                        new GradientStop(Color.FromRgb(180, 240, 255), 0.5),
                        new GradientStop(Color.FromRgb(0, 180, 255), 1.0)
                    }
                },
                RadiusX = 3,
                RadiusY = 3
            };
            Canvas.SetLeft(bladeBar, 25);
            Canvas.SetTop(bladeBar, 42);
            washerCanvas.Children.Add(bladeBar);

            // Squeegee rubber wiper edge
            var rubberEdge = new Rectangle
            {
                Width = 96,
                Height = 5,
                Fill = new SolidColorBrush(Color.FromRgb(20, 40, 70)),
                RadiusX = 2,
                RadiusY = 2
            };
            Canvas.SetLeft(rubberEdge, 27);
            Canvas.SetTop(rubberEdge, 38);
            washerCanvas.Children.Add(rubberEdge);

            // Water spray jets from washer nozzle
            for (int i = 0; i < 5; i++)
            {
                var sprayJet = new Path
                {
                    Data = Geometry.Parse($"M 75,45 Q {45 + i * 15},{15 - i * 3} {35 + i * 20},0"),
                    Stroke = new SolidColorBrush(Color.FromArgb(200, 120, 220, 255)),
                    StrokeThickness = 2.5
                };
                washerCanvas.Children.Add(sprayJet);
            }

            var rotate = new RotateTransform(-65, 75, 130);
            washerCanvas.RenderTransform = rotate;

            Canvas.SetLeft(washerCanvas, target.X - 75);
            Canvas.SetTop(washerCanvas, target.Y - 110);
            _weaponCanvas.Children.Add(washerCanvas);

            // Rapid water wipe swing arc
            var sweepAnim = new DoubleAnimation(-65, 30, TimeSpan.FromMilliseconds(85))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            sweepAnim.Completed += (s, e) =>
            {
                onImpact?.Invoke();
                SpawnWaterSplash(target, 28);

                var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(95));
                fade.Completed += (fs, fe) =>
                {
                    _weaponCanvas.Children.Remove(washerCanvas);
                    onFinished?.Invoke();
                };
                washerCanvas.BeginAnimation(UIElement.OpacityProperty, fade);
            };

            rotate.BeginAnimation(RotateTransform.AngleProperty, sweepAnim);
        }

        private void SpawnWaterSplash(Point center, int count)
        {
            for (int i = 0; i < count; i++)
            {
                double angle = _random.NextDouble() * Math.PI * 2.0;
                double dist = 20.0 + _random.NextDouble() * 70.0;
                double size = 4.0 + _random.NextDouble() * 8.0;

                var drop = new Ellipse
                {
                    Width = size,
                    Height = size * 1.3,
                    Fill = new RadialGradientBrush
                    {
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(240, 220, 245, 255), 0.0),
                            new GradientStop(Color.FromArgb(190, 60, 180, 255), 0.7),
                            new GradientStop(Color.FromArgb(120, 0, 120, 230), 1.0)
                        }
                    },
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(drop, center.X);
                Canvas.SetTop(drop, center.Y);
                _weaponCanvas.Children.Add(drop);

                var moveX = new DoubleAnimation(center.X, center.X + Math.Cos(angle) * dist, TimeSpan.FromMilliseconds(180));
                var moveY = new DoubleAnimation(center.Y, center.Y + Math.Sin(angle) * dist, TimeSpan.FromMilliseconds(180));
                var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(180));

                fade.Completed += (s, e) =>
                {
                    _weaponCanvas.Children.Remove(drop);
                };

                drop.BeginAnimation(Canvas.LeftProperty, moveX);
                drop.BeginAnimation(Canvas.TopProperty, moveY);
                drop.BeginAnimation(UIElement.OpacityProperty, fade);
            }
        }

        #endregion

        private void SpawnImpactSparks(Point center, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                double angle = _random.NextDouble() * Math.PI * 2.0;
                double dist = 15.0 + _random.NextDouble() * 35.0;
                double size = 3.0 + _random.NextDouble() * 4.5;

                var spark = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(color),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(spark, center.X);
                Canvas.SetTop(spark, center.Y);
                _weaponCanvas.Children.Add(spark);

                var moveX = new DoubleAnimation(center.X, center.X + Math.Cos(angle) * dist, TimeSpan.FromMilliseconds(140));
                var moveY = new DoubleAnimation(center.Y, center.Y + Math.Sin(angle) * dist, TimeSpan.FromMilliseconds(140));
                var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(140));

                fade.Completed += (s, e) =>
                {
                    _weaponCanvas.Children.Remove(spark);
                };

                spark.BeginAnimation(Canvas.LeftProperty, moveX);
                spark.BeginAnimation(Canvas.TopProperty, moveY);
                spark.BeginAnimation(UIElement.OpacityProperty, fade);
            }
        }
    }
}
