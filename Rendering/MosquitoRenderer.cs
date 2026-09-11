using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DigitalMosquito
{
    public class MosquitoRenderer
    {
        public Canvas VisualElement { get; }

        private readonly Canvas _rootCanvas;
        private readonly Canvas _bodyCanvas;
        private readonly Canvas _shadowCanvas;

        private readonly RotateTransform _leftWingRotate;
        private readonly RotateTransform _rightWingRotate;
        private readonly RotateTransform _bodyRotate;
        private readonly RotateTransform _shadowRotate;

        private readonly ScaleTransform _leftWingScale;
        private readonly ScaleTransform _rightWingScale;

        public MosquitoRenderer()
        {
            _rootCanvas = new Canvas
            {
                Width = 100,
                Height = 100,
                IsHitTestVisible = false
            };
            VisualElement = _rootCanvas;

            // Shadow layer for floating depth illusion
            _shadowCanvas = CreateMosquitoGraphic(isShadow: true);
            _shadowRotate = new RotateTransform(0, 50, 50);
            _shadowCanvas.RenderTransform = _shadowRotate;
            Canvas.SetLeft(_shadowCanvas, 8);
            Canvas.SetTop(_shadowCanvas, 14);
            _shadowCanvas.Opacity = 0.28;

            // Main vector body layer
            _bodyCanvas = CreateMosquitoGraphic(isShadow: false);
            _bodyRotate = new RotateTransform(0, 50, 50);
            _bodyCanvas.RenderTransform = _bodyRotate;

            // Wing animation transforms
            _leftWingRotate = new RotateTransform(0, 48, 48);
            _leftWingScale = new ScaleTransform(1.0, 1.0, 48, 48);
            var leftWingGroup = new TransformGroup();
            leftWingGroup.Children.Add(_leftWingScale);
            leftWingGroup.Children.Add(_leftWingRotate);

            _rightWingRotate = new RotateTransform(0, 52, 48);
            _rightWingScale = new ScaleTransform(1.0, 1.0, 52, 48);
            var rightWingGroup = new TransformGroup();
            rightWingGroup.Children.Add(_rightWingScale);
            rightWingGroup.Children.Add(_rightWingRotate);

            if (_bodyCanvas.FindName("LeftWing") is FrameworkElement leftWing)
            {
                leftWing.RenderTransform = leftWingGroup;
            }
            if (_bodyCanvas.FindName("RightWing") is FrameworkElement rightWing)
            {
                rightWing.RenderTransform = rightWingGroup;
            }

            _rootCanvas.Children.Add(_shadowCanvas);
            _rootCanvas.Children.Add(_bodyCanvas);
        }

        private Canvas CreateMosquitoGraphic(bool isShadow)
        {
            var canvas = new Canvas
            {
                Width = 100,
                Height = 100
            };

            var bodyBrush = isShadow
                ? new SolidColorBrush(Colors.Black)
                : new SolidColorBrush(Color.FromRgb(38, 38, 38));

            var abdomenStripeLight = isShadow
                ? new SolidColorBrush(Colors.Black)
                : new SolidColorBrush(Color.FromRgb(210, 185, 140));

            var legBrush = isShadow
                ? new SolidColorBrush(Colors.Black)
                : new SolidColorBrush(Color.FromRgb(45, 45, 45));

            var proboscisBrush = isShadow
                ? new SolidColorBrush(Colors.Black)
                : new SolidColorBrush(Color.FromRgb(20, 20, 20));

            var wingBrush = isShadow
                ? new SolidColorBrush(Color.FromArgb(120, 0, 0, 0))
                : new SolidColorBrush(Color.FromArgb(150, 215, 235, 255));

            var wingStroke = isShadow
                ? new SolidColorBrush(Color.FromArgb(80, 0, 0, 0))
                : new SolidColorBrush(Color.FromArgb(180, 160, 190, 220));

            // 1. Jointed 6 legs
            // Back legs (long, curved backward)
            canvas.Children.Add(CreateLegPath("M 46,55 L 26,72 L 10,88 L 4,96", legBrush, 1.6));
            canvas.Children.Add(CreateLegPath("M 54,55 L 74,72 L 90,88 L 96,96", legBrush, 1.6));

            // Middle legs (spreading out)
            canvas.Children.Add(CreateLegPath("M 46,50 L 22,50 L 8,62 L 2,68", legBrush, 1.5));
            canvas.Children.Add(CreateLegPath("M 54,50 L 78,50 L 92,62 L 98,68", legBrush, 1.5));

            // Front legs (reaching forward)
            canvas.Children.Add(CreateLegPath("M 47,44 L 28,34 L 16,18 L 12,12", legBrush, 1.4));
            canvas.Children.Add(CreateLegPath("M 53,44 L 72,34 L 84,18 L 88,12", legBrush, 1.4));

            // 2. Needle proboscis (forward-pointing needle)
            var proboscis = new Line
            {
                X1 = 50,
                Y1 = 35,
                X2 = 50,
                Y2 = 14,
                Stroke = proboscisBrush,
                StrokeThickness = 1.8,
                StrokeEndLineCap = PenLineCap.Round
            };
            canvas.Children.Add(proboscis);

            // Antennae
            canvas.Children.Add(CreateLegPath("M 49,36 Q 44,28 41,20", proboscisBrush, 1.0));
            canvas.Children.Add(CreateLegPath("M 51,36 Q 56,28 59,20", proboscisBrush, 1.0));

            // 3. Head
            var head = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = bodyBrush
            };
            Canvas.SetLeft(head, 46);
            Canvas.SetTop(head, 34);
            canvas.Children.Add(head);

            // Eyes (small dark glossy red/black dots)
            if (!isShadow)
            {
                var eyeL = new Ellipse { Width = 2.5, Height = 2.5, Fill = new SolidColorBrush(Color.FromRgb(80, 10, 10)) };
                Canvas.SetLeft(eyeL, 46);
                Canvas.SetTop(eyeL, 36);
                canvas.Children.Add(eyeL);

                var eyeR = new Ellipse { Width = 2.5, Height = 2.5, Fill = new SolidColorBrush(Color.FromRgb(80, 10, 10)) };
                Canvas.SetLeft(eyeR, 51.5);
                Canvas.SetTop(eyeR, 36);
                canvas.Children.Add(eyeR);
            }

            // 4. Thorax (broad, rounded)
            var thorax = new Ellipse
            {
                Width = 11,
                Height = 15,
                Fill = bodyBrush
            };
            Canvas.SetLeft(thorax, 44.5);
            Canvas.SetTop(thorax, 40);
            canvas.Children.Add(thorax);

            // 5. Segmented Abdomen (elongated, striped)
            var abdomen = new Path
            {
                Data = Geometry.Parse("M 47,53 Q 46,65 48,78 Q 50,82 52,78 Q 54,65 53,53 Z"),
                Fill = bodyBrush
            };
            canvas.Children.Add(abdomen);

            if (!isShadow)
            {
                // Alternating mosquito abdomen stripes
                for (int y = 57; y <= 73; y += 4)
                {
                    var stripe = new Line
                    {
                        X1 = 47.5,
                        Y1 = y,
                        X2 = 52.5,
                        Y2 = y,
                        Stroke = abdomenStripeLight,
                        StrokeThickness = 1.3
                    };
                    canvas.Children.Add(stripe);
                }
            }

            // 6. Translucent Wings
            var leftWing = new Path
            {
                Name = isShadow ? "LeftWingShadow" : "LeftWing",
                Data = Geometry.Parse("M 48,46 C 40,38 28,34 16,36 C 24,45 36,49 48,48 Z"),
                Fill = wingBrush,
                Stroke = wingStroke,
                StrokeThickness = 0.8
            };
            canvas.Children.Add(leftWing);

            var rightWing = new Path
            {
                Name = isShadow ? "RightWingShadow" : "RightWing",
                Data = Geometry.Parse("M 52,46 C 60,38 72,34 84,36 C 76,45 64,49 52,48 Z"),
                Fill = wingBrush,
                Stroke = wingStroke,
                StrokeThickness = 0.8
            };
            canvas.Children.Add(rightWing);

            return canvas;
        }

        private static Path CreateLegPath(string data, Brush stroke, double thickness)
        {
            return new Path
            {
                Data = Geometry.Parse(data),
                Stroke = stroke,
                StrokeThickness = thickness,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
        }

        public void Render(Mosquito mosquito)
        {
            // Position root canvas so (50, 50) aligns with mosquito position
            Canvas.SetLeft(_rootCanvas, mosquito.DisplayX - 50);
            Canvas.SetTop(_rootCanvas, mosquito.DisplayY - 50);

            // Vector mosquito points forward towards -Y (top) in local coords (proboscis is at Y=14).
            // WPF AngleDegrees: 0 is +X (right), 90 is +Y (down).
            // To point forward along AngleDegrees, we offset by +90 degrees.
            double visualAngle = mosquito.AngleDegrees + 90;
            _bodyRotate.Angle = visualAngle;
            _shadowRotate.Angle = visualAngle;

            // Wing flutter oscillation (high frequency flapping)
            double flapAngle = Math.Sin(mosquito.WingPhase) * 32.0;
            double flapScale = 0.75 + Math.Abs(Math.Cos(mosquito.WingPhase)) * 0.45;

            _leftWingRotate.Angle = flapAngle;
            _leftWingScale.ScaleY = flapScale;

            _rightWingRotate.Angle = -flapAngle;
            _rightWingScale.ScaleY = flapScale;
        }

        public void SetVisibility(bool visible)
        {
            _rootCanvas.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
