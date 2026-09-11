using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DigitalMosquito
{
    public class SpiderRenderer
    {
        public Canvas VisualElement { get; }

        private readonly Canvas _rootCanvas;
        private readonly Canvas _bodyCanvas;
        private readonly Canvas _shadowCanvas;
        private readonly Line _webLine;

        private readonly RotateTransform _bodyRotate;
        private readonly RotateTransform _shadowRotate;
        private readonly ScaleTransform _scaleTransform;

        // Dynamic leg paths for 8 legs
        private readonly Path[] _leftLegPaths = new Path[4];
        private readonly Path[] _rightLegPaths = new Path[4];
        private readonly Path[] _leftLegShadows = new Path[4];
        private readonly Path[] _rightLegShadows = new Path[4];

        private readonly double _scale;
        private readonly bool _isGiant;

        public SpiderRenderer(double scale = 1.0, bool isGiant = false)
        {
            _scale = scale;
            _isGiant = isGiant;

            _rootCanvas = new Canvas
            {
                Width = 140 * scale,
                Height = 140 * scale,
                IsHitTestVisible = false
            };
            VisualElement = _rootCanvas;

            // Web line attached directly to visual element or handled at window level
            _webLine = new Line
            {
                Stroke = new SolidColorBrush(Color.FromArgb(170, 240, 245, 255)),
                StrokeThickness = Math.Max(1.0, 1.3 * scale),
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            _rootCanvas.Children.Add(_webLine);

            _scaleTransform = new ScaleTransform(scale, scale, 70, 70);

            // Shadow layer
            _shadowCanvas = CreateSpiderGraphic(isShadow: true, out _leftLegShadows, out _rightLegShadows);
            _shadowRotate = new RotateTransform(0, 70, 70);
            var shadowGroup = new TransformGroup();
            shadowGroup.Children.Add(_scaleTransform);
            shadowGroup.Children.Add(_shadowRotate);
            _shadowCanvas.RenderTransform = shadowGroup;
            Canvas.SetLeft(_shadowCanvas, 6 * scale);
            Canvas.SetTop(_shadowCanvas, 10 * scale);
            _shadowCanvas.Opacity = 0.32;

            // Body layer
            _bodyCanvas = CreateSpiderGraphic(isShadow: false, out _leftLegPaths, out _rightLegPaths);
            _bodyRotate = new RotateTransform(0, 70, 70);
            var bodyGroup = new TransformGroup();
            bodyGroup.Children.Add(_scaleTransform);
            bodyGroup.Children.Add(_bodyRotate);
            _bodyCanvas.RenderTransform = bodyGroup;

            _rootCanvas.Children.Add(_shadowCanvas);
            _rootCanvas.Children.Add(_bodyCanvas);
        }

        private Canvas CreateSpiderGraphic(bool isShadow, out Path[] leftLegs, out Path[] rightLegs)
        {
            var canvas = new Canvas
            {
                Width = 140,
                Height = 140
            };

            leftLegs = new Path[4];
            rightLegs = new Path[4];

            var chitinDark = isShadow
                ? new SolidColorBrush(Colors.Black)
                : (_isGiant ? new SolidColorBrush(Color.FromRgb(16, 12, 14)) : new SolidColorBrush(Color.FromRgb(24, 24, 28)));

            var chitinHighlight = isShadow
                ? new SolidColorBrush(Colors.Black)
                : (_isGiant ? new SolidColorBrush(Color.FromRgb(45, 25, 30)) : new SolidColorBrush(Color.FromRgb(50, 50, 58)));

            var markingsBrush = isShadow
                ? new SolidColorBrush(Colors.Black)
                : (_isGiant ? new SolidColorBrush(Color.FromRgb(235, 35, 35)) : new SolidColorBrush(Color.FromRgb(200, 160, 90)));

            var legBrush = isShadow
                ? new SolidColorBrush(Colors.Black)
                : (_isGiant ? new SolidColorBrush(Color.FromRgb(20, 15, 18)) : new SolidColorBrush(Color.FromRgb(32, 32, 38)));

            double legThickness = _isGiant ? 2.8 : 2.2;

            // 1. Create 4 pairs of jointed legs
            for (int i = 0; i < 4; i++)
            {
                var leftLeg = new Path
                {
                    Stroke = legBrush,
                    StrokeThickness = legThickness,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                var rightLeg = new Path
                {
                    Stroke = legBrush,
                    StrokeThickness = legThickness,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };

                leftLegs[i] = leftLeg;
                rightLegs[i] = rightLeg;

                canvas.Children.Add(leftLeg);
                canvas.Children.Add(rightLeg);
            }

            // 2. Pedipalps (front sensory feelers)
            var palpL = new Path
            {
                Data = Geometry.Parse("M 66,54 Q 61,46 64,40"),
                Stroke = legBrush,
                StrokeThickness = legThickness * 0.75,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            var palpR = new Path
            {
                Data = Geometry.Parse("M 74,54 Q 79,46 76,40"),
                Stroke = legBrush,
                StrokeThickness = legThickness * 0.75,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            canvas.Children.Add(palpL);
            canvas.Children.Add(palpR);

            // 3. Chelicerae / Fangs
            if (!isShadow)
            {
                var fangL = new Path
                {
                    Data = Geometry.Parse("M 67,52 L 67,46 L 69,47 Z"),
                    Fill = new SolidColorBrush(Color.FromRgb(70, 20, 20))
                };
                var fangR = new Path
                {
                    Data = Geometry.Parse("M 73,52 L 73,46 L 71,47 Z"),
                    Fill = new SolidColorBrush(Color.FromRgb(70, 20, 20))
                };
                canvas.Children.Add(fangL);
                canvas.Children.Add(fangR);
            }

            // 4. Abdomen (large bulbous posterior segment)
            var abdomen = new Path
            {
                Data = Geometry.Parse("M 70,68 C 55,68 50,86 54,98 C 58,107 82,107 86,98 C 90,86 85,68 70,68 Z"),
                Fill = chitinDark,
                Stroke = chitinHighlight,
                StrokeThickness = 1.0
            };
            canvas.Children.Add(abdomen);

            // 5. Abdomen markings (venomous/arcade pattern)
            if (!isShadow)
            {
                if (_isGiant)
                {
                    // Menacing red skull/hourglass crest
                    var mark = new Path
                    {
                        Data = Geometry.Parse("M 70,74 L 64,80 L 68,85 L 64,95 L 70,92 L 76,95 L 72,85 L 76,80 Z"),
                        Fill = markingsBrush
                    };
                    canvas.Children.Add(mark);
                }
                else
                {
                    // Golden/amber dorsal chevron spots
                    for (int y = 76; y <= 94; y += 6)
                    {
                        var dotL = new Ellipse { Width = 3, Height = 3, Fill = markingsBrush };
                        Canvas.SetLeft(dotL, 66);
                        Canvas.SetTop(dotL, y);
                        canvas.Children.Add(dotL);

                        var dotR = new Ellipse { Width = 3, Height = 3, Fill = markingsBrush };
                        Canvas.SetLeft(dotR, 71);
                        Canvas.SetTop(dotR, y);
                        canvas.Children.Add(dotR);
                    }
                }
            }

            // 6. Cephalothorax (head/midsection)
            var cephalothorax = new Ellipse
            {
                Width = 18,
                Height = 20,
                Fill = chitinDark,
                Stroke = chitinHighlight,
                StrokeThickness = 1.0
            };
            Canvas.SetLeft(cephalothorax, 61);
            Canvas.SetTop(cephalothorax, 52);
            canvas.Children.Add(cephalothorax);

            // 7. Cluster of arachnid eyes
            if (!isShadow)
            {
                Color eyeGlow = _isGiant ? Color.FromRgb(255, 30, 30) : Color.FromRgb(240, 60, 40);
                var eyeBrush = new SolidColorBrush(eyeGlow);

                // Main forward eyes
                var eye1 = new Ellipse { Width = 2.8, Height = 2.8, Fill = eyeBrush };
                Canvas.SetLeft(eye1, 67.2);
                Canvas.SetTop(eye1, 53.5);
                canvas.Children.Add(eye1);

                var eye2 = new Ellipse { Width = 2.8, Height = 2.8, Fill = eyeBrush };
                Canvas.SetLeft(eye2, 70.0);
                Canvas.SetTop(eye2, 53.5);
                canvas.Children.Add(eye2);

                // Secondary lateral eyes
                var eye3 = new Ellipse { Width = 2.0, Height = 2.0, Fill = eyeBrush };
                Canvas.SetLeft(eye3, 64.5);
                Canvas.SetTop(eye3, 55.0);
                canvas.Children.Add(eye3);

                var eye4 = new Ellipse { Width = 2.0, Height = 2.0, Fill = eyeBrush };
                Canvas.SetLeft(eye4, 73.5);
                Canvas.SetTop(eye4, 55.0);
                canvas.Children.Add(eye4);
            }

            return canvas;
        }

        public void Render(Spider spider)
        {
            double centerOffset = 70 * _scale;
            Canvas.SetLeft(_rootCanvas, spider.DisplayX - centerOffset);
            Canvas.SetTop(_rootCanvas, spider.DisplayY - centerOffset);

            // Angle alignment: local spider graphic faces -Y (up) with head at Y=52, abdomen at Y=90.
            // WPF AngleDegrees: 0 is +X (right). Adding 90 makes forward match AngleDegrees.
            double visualAngle = spider.AngleDegrees + 90;
            _bodyRotate.Angle = visualAngle;
            _shadowRotate.Angle = visualAngle;

            // Animate articulated legs
            UpdateLegs(spider.LegCycle, spider.IsHangingOnWeb);

            // Render web thread if hanging or climbing
            if (spider.HasWebThread)
            {
                _webLine.Visibility = Visibility.Visible;
                // Web thread extends from ceiling down to spider's spinneret (approx center of abdomen)
                // In local coordinates of _rootCanvas:
                // Abdomen tip is at (70, 95) in local coords when facing up
                double localSpinneretX = 70 * _scale;
                double localSpinneretY = 70 * _scale;

                _webLine.X1 = localSpinneretX;
                _webLine.Y1 = -spider.DisplayY + centerOffset; // Ceiling at Y=0
                _webLine.X2 = localSpinneretX;
                _webLine.Y2 = localSpinneretY;
            }
            else
            {
                _webLine.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateLegs(double cycle, bool isHanging)
        {
            // Calculate leg joint kinematics
            // In crawling, alternating tripod gait: pairs (0L, 2L, 1R, 3R) vs (1L, 3L, 0R, 2R)
            double stepAmplitude = isHanging ? 2.5 : 8.0;

            for (int i = 0; i < 4; i++)
            {
                double phaseOffset = (i % 2 == 0) ? 0 : Math.PI;
                if (isHanging) phaseOffset = i * 0.7; // Gentle twitch when hanging

                double wave = Math.Sin(cycle + phaseOffset);
                double sweep = wave * stepAmplitude;
                double reach = Math.Abs(Math.Cos(cycle + phaseOffset)) * (isHanging ? 1.5 : 4.0);

                // Leg 0: Front leg (reaching forward and out)
                // Leg 1: Second leg (spreading sideways-forward)
                // Leg 2: Third leg (spreading sideways-backward)
                // Leg 3: Hind leg (reaching backward and out)
                string leftPathData;
                string rightPathData;

                switch (i)
                {
                    case 0: // Front
                        leftPathData = $"M 64,57 Q {40 - reach},{38 + sweep} {20 - sweep},{18 - reach}";
                        rightPathData = $"M 76,57 Q {100 + reach},{38 - sweep} {120 + sweep},{18 - reach}";
                        break;
                    case 1: // Mid-front
                        leftPathData = $"M 62,61 Q {34 - reach},{55 + sweep} {12 - sweep},{46 + reach}";
                        rightPathData = $"M 78,61 Q {106 + reach},{55 - sweep} {128 + sweep},{46 + reach}";
                        break;
                    case 2: // Mid-back
                        leftPathData = $"M 63,65 Q {36 - reach},{76 - sweep} {15 - sweep},{82 + reach}";
                        rightPathData = $"M 77,65 Q {104 + reach},{76 + sweep} {125 + sweep},{82 + reach}";
                        break;
                    default: // Hind
                        leftPathData = $"M 65,68 Q {42 - reach},{96 - sweep} {24 - sweep},{122 + reach}";
                        rightPathData = $"M 75,68 Q {98 + reach},{96 + sweep} {116 + sweep},{122 + reach}";
                        break;
                }

                var leftGeom = Geometry.Parse(leftPathData);
                var rightGeom = Geometry.Parse(rightPathData);

                _leftLegPaths[i].Data = leftGeom;
                _rightLegPaths[i].Data = rightGeom;
                _leftLegShadows[i].Data = leftGeom;
                _rightLegShadows[i].Data = rightGeom;
            }
        }

        public void SetVisibility(bool visible)
        {
            _rootCanvas.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
