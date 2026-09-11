using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace DigitalMosquito
{
    public class NightmareMonsterRenderer
    {
        private readonly Canvas _container;
        private readonly Random _random = new();

        public Canvas MonsterRoot { get; private set; } = new();
        public RotateTransform? FangLeftTransform { get; private set; }
        public RotateTransform? FangRightTransform { get; private set; }
        public ScaleTransform? MonsterScaleTransform { get; private set; }
        public TranslateTransform? MonsterJitterTransform { get; private set; }

        public NightmareMonsterRenderer(Canvas container)
        {
            _container = container;
        }

        public void Clear()
        {
            _container.Children.Clear();
            MonsterRoot = new Canvas();
        }

        /// <summary>
        /// Builds the colossal Demonic Arachnid horror face with 10 glowing eyes, snapping fangs, and screen-gripping legs.
        /// </summary>
        public void BuildDemonArachnid(double screenWidth, double screenHeight)
        {
            Clear();

            MonsterRoot = new Canvas
            {
                Width = 840,
                Height = 840,
                IsHitTestVisible = false
            };

            var transformGroup = new TransformGroup();
            MonsterScaleTransform = new ScaleTransform(1.0, 1.0, 420, 420);
            MonsterJitterTransform = new TranslateTransform(0, 0);
            transformGroup.Children.Add(MonsterScaleTransform);
            transformGroup.Children.Add(MonsterJitterTransform);
            MonsterRoot.RenderTransform = transformGroup;

            double cx = 420;
            double cy = 400;

            // 1. Giant Spiny Raptor Legs reaching outwards to screen borders
            var legsCanvas = new Canvas();
            BuildArachnidLegs(legsCanvas, cx, cy);
            MonsterRoot.Children.Add(legsCanvas);

            // 2. Colossal Armored Cephalothorax (Demonic Carapace)
            var carapace = new Path
            {
                Fill = new RadialGradientBrush
                {
                    Center = new Point(0.5, 0.4),
                    GradientOrigin = new Point(0.5, 0.35),
                    RadiusX = 0.55,
                    RadiusY = 0.55,
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(45, 12, 18), 0.0),   // Inner bloody dark highlight
                        new GradientStop(Color.FromRgb(22, 6, 9), 0.45),
                        new GradientStop(Color.FromRgb(8, 6, 8), 0.8),
                        new GradientStop(Color.FromRgb(2, 2, 2), 1.0)       // Pitch obsidian edge
                    }
                },
                Stroke = new SolidColorBrush(Color.FromArgb(160, 180, 20, 30)),
                StrokeThickness = 3,
                Data = Geometry.Parse("M 260,330 C 240,240 320,180 420,170 C 520,180 600,240 580,330 C 600,430 540,510 420,530 C 300,510 240,430 260,330 Z")
            };
            carapace.Effect = new DropShadowEffect
            {
                BlurRadius = 30,
                Color = Color.FromRgb(180, 0, 20),
                Opacity = 0.6,
                ShadowDepth = 0
            };
            MonsterRoot.Children.Add(carapace);

            // Carapace chitin armor ridges
            var chitinRidge1 = new Path
            {
                Stroke = new SolidColorBrush(Color.FromArgb(90, 255, 60, 60)),
                StrokeThickness = 2.5,
                Data = Geometry.Parse("M 320,290 C 370,330 470,330 520,290")
            };
            var chitinRidge2 = new Path
            {
                Stroke = new SolidColorBrush(Color.FromArgb(70, 255, 40, 40)),
                StrokeThickness = 2,
                Data = Geometry.Parse("M 340,360 C 380,390 460,390 500,360")
            };
            MonsterRoot.Children.Add(chitinRidge1);
            MonsterRoot.Children.Add(chitinRidge2);

            // 3. Ten Evil Glowing Demonic Eyes (2 colossal central, 4 amber medium, 4 green sulfur outer)
            BuildDemonEyes(MonsterRoot, cx, cy - 80);

            // 4. Spiny Mouthparts & Pedipalps
            BuildPedipalps(MonsterRoot, cx, cy + 40);

            // 5. Massive Snapping Curved Hollow Fangs
            BuildSnappingFangs(MonsterRoot, cx, cy + 70);

            // Center MonsterRoot in canvas
            Canvas.SetLeft(MonsterRoot, (screenWidth - 840) / 2.0);
            Canvas.SetTop(MonsterRoot, (screenHeight - 840) / 2.0);

            _container.Children.Add(MonsterRoot);
        }

        private void BuildArachnidLegs(Canvas parent, double cx, double cy)
        {
            // 8 massive jointed spiny raptor legs spanning across the screen
            var legPaths = new (string pathData, double thickness)[]
            {
                // Top-left & Top-right (reaching to top screen corners)
                ("M 300,260 Q 150,110 20,-40 Q -30,-80 -60,-120", 14),
                ("M 540,260 Q 690,110 820,-40 Q 870,-80 900,-120", 14),

                // Mid-upper left & right (reaching wide to left & right borders)
                ("M 270,330 Q 80,240 -120,200 Q -180,180 -240,160", 16),
                ("M 570,330 Q 760,240 960,200 Q 1020,180 1080,160", 16),

                // Mid-lower left & right
                ("M 280,410 Q 70,440 -140,510 Q -200,540 -260,600", 15),
                ("M 560,410 Q 770,440 980,510 Q 1040,540 1100,600", 15),

                // Bottom left & right (clawing down towards viewer)
                ("M 320,480 Q 170,680 40,880 Q -10,950 -50,1020", 13),
                ("M 520,480 Q 670,680 800,880 Q 850,950 890,1020", 13),
            };

            var legBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(10, 8, 10), 0.0),
                    new GradientStop(Color.FromRgb(35, 12, 16), 0.5),
                    new GradientStop(Color.FromRgb(6, 4, 6), 1.0)
                }
            };

            foreach (var (pathData, thickness) in legPaths)
            {
                var leg = new Path
                {
                    Data = Geometry.Parse(pathData),
                    Stroke = legBrush,
                    StrokeThickness = thickness,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Triangle
                };
                leg.Effect = new DropShadowEffect
                {
                    BlurRadius = 14,
                    Color = Colors.Black,
                    Opacity = 0.85,
                    ShadowDepth = 4
                };
                parent.Children.Add(leg);
            }
        }

        private void BuildDemonEyes(Canvas parent, double cx, double cy)
        {
            var eyeGlowEffect = new DropShadowEffect
            {
                BlurRadius = 24,
                Color = Color.FromRgb(255, 0, 30),
                Opacity = 0.95,
                ShadowDepth = 0
            };

            // 1. Two Colossal Primary Center Eyes (Slit-pupil demon orbs)
            double[] centerEyeOffsets = { -36, 36 };
            foreach (double ox in centerEyeOffsets)
            {
                var outerGlow = new Ellipse
                {
                    Width = 48,
                    Height = 54,
                    Fill = new RadialGradientBrush
                    {
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(255, 20, 40), 0.0),
                            new GradientStop(Color.FromRgb(200, 0, 20), 0.65),
                            new GradientStop(Color.FromRgb(60, 0, 10), 1.0)
                        }
                    },
                    Effect = eyeGlowEffect
                };
                Canvas.SetLeft(outerGlow, cx + ox - 24);
                Canvas.SetTop(outerGlow, cy - 27);
                parent.Children.Add(outerGlow);

                // Burning Iris Ring
                var iris = new Ellipse
                {
                    Width = 28,
                    Height = 36,
                    Fill = new RadialGradientBrush
                    {
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(255, 230, 80), 0.0),
                            new GradientStop(Color.FromRgb(255, 60, 0), 0.6),
                            new GradientStop(Color.FromRgb(180, 0, 0), 1.0)
                        }
                    }
                };
                Canvas.SetLeft(iris, cx + ox - 14);
                Canvas.SetTop(iris, cy - 18);
                parent.Children.Add(iris);

                // Black Vertical Slit Demon Pupil
                var pupil = new Path
                {
                    Fill = Brushes.Black,
                    Data = Geometry.Parse("M 0,-14 C 5,-7 5,7 0,14 C -5,7 -5,-7 0,-14 Z")
                };
                Canvas.SetLeft(pupil, cx + ox);
                Canvas.SetTop(pupil, cy);
                parent.Children.Add(pupil);

                // Specular glint
                var glint = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.White
                };
                Canvas.SetLeft(glint, cx + ox - 8);
                Canvas.SetTop(glint, cy - 12);
                parent.Children.Add(glint);
            }

            // 2. Four Intermediate Amber Secondary Eyes
            (double x, double y)[] amberEyes = { (-75, -28), (75, -28), (-55, 24), (55, 24) };
            foreach (var (ex, ey) in amberEyes)
            {
                var eye = new Ellipse
                {
                    Width = 22,
                    Height = 22,
                    Fill = new RadialGradientBrush
                    {
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(255, 210, 40), 0.0),
                            new GradientStop(Color.FromRgb(255, 90, 0), 0.7),
                            new GradientStop(Color.FromRgb(50, 10, 0), 1.0)
                        }
                    },
                    Effect = new DropShadowEffect
                    {
                        BlurRadius = 16,
                        Color = Color.FromRgb(255, 120, 0),
                        Opacity = 0.9,
                        ShadowDepth = 0
                    }
                };
                Canvas.SetLeft(eye, cx + ex - 11);
                Canvas.SetTop(eye, cy + ey - 11);
                parent.Children.Add(eye);

                var p = new Ellipse { Width = 6, Height = 10, Fill = Brushes.Black };
                Canvas.SetLeft(p, cx + ex - 3);
                Canvas.SetTop(p, cy + ey - 5);
                parent.Children.Add(p);
            }

            // 3. Four Lateral Venomous Green Outer Eyes
            (double x, double y)[] greenEyes = { (-110, -8), (110, -8), (-95, 38), (95, 38) };
            foreach (var (gx, gy) in greenEyes)
            {
                var eye = new Ellipse
                {
                    Width = 16,
                    Height = 16,
                    Fill = new RadialGradientBrush
                    {
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(120, 255, 60), 0.0),
                            new GradientStop(Color.FromRgb(30, 160, 20), 0.7),
                            new GradientStop(Color.FromRgb(0, 40, 5), 1.0)
                        }
                    },
                    Effect = new DropShadowEffect
                    {
                        BlurRadius = 14,
                        Color = Color.FromRgb(57, 255, 20),
                        Opacity = 0.85,
                        ShadowDepth = 0
                    }
                };
                Canvas.SetLeft(eye, cx + gx - 8);
                Canvas.SetTop(eye, cy + gy - 8);
                parent.Children.Add(eye);
            }
        }

        private void BuildPedipalps(Canvas parent, double cx, double cy)
        {
            var leftPalp = new Path
            {
                Data = Geometry.Parse("M 360,440 Q 300,470 270,540 Q 250,590 230,640"),
                Stroke = new SolidColorBrush(Color.FromRgb(20, 8, 12)),
                StrokeThickness = 12,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Triangle
            };
            var rightPalp = new Path
            {
                Data = Geometry.Parse("M 480,440 Q 540,470 570,540 Q 590,590 610,640"),
                Stroke = new SolidColorBrush(Color.FromRgb(20, 8, 12)),
                StrokeThickness = 12,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Triangle
            };
            parent.Children.Add(leftPalp);
            parent.Children.Add(rightPalp);
        }

        private void BuildSnappingFangs(Canvas parent, double cx, double cy)
        {
            // Left Fang with snapping rotation transform
            var leftFangCanvas = new Canvas { Width = 120, Height = 220 };
            FangLeftTransform = new RotateTransform(-28, 60, 20);
            leftFangCanvas.RenderTransform = FangLeftTransform;

            var leftFang = new Path
            {
                Data = Geometry.Parse("M 40,20 C 65,15 85,25 75,55 C 65,100 45,155 10,210 C 25,160 38,100 40,20 Z"),
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(220, 215, 200), 0.0),
                        new GradientStop(Color.FromRgb(70, 15, 20), 0.5),
                        new GradientStop(Color.FromRgb(15, 10, 15), 1.0)
                    }
                },
                Stroke = new SolidColorBrush(Color.FromArgb(180, 255, 240, 200)),
                StrokeThickness = 2
            };
            leftFang.Effect = new DropShadowEffect
            {
                BlurRadius = 15,
                Color = Color.FromRgb(57, 255, 20),
                Opacity = 0.7,
                ShadowDepth = 0
            };
            leftFangCanvas.Children.Add(leftFang);

            var leftVenomDrop = new Path
            {
                Data = Geometry.Parse("M 10,210 Q 5,235 10,245 Q 15,235 10,210 Z"),
                Fill = new SolidColorBrush(Color.FromArgb(230, 57, 255, 20))
            };
            leftFangCanvas.Children.Add(leftVenomDrop);

            Canvas.SetLeft(leftFangCanvas, cx - 110);
            Canvas.SetTop(leftFangCanvas, cy);
            parent.Children.Add(leftFangCanvas);

            // Right Fang with snapping rotation transform
            var rightFangCanvas = new Canvas { Width = 120, Height = 220 };
            FangRightTransform = new RotateTransform(28, 60, 20);
            rightFangCanvas.RenderTransform = FangRightTransform;

            var rightFang = new Path
            {
                Data = Geometry.Parse("M 80,20 C 55,15 35,25 45,55 C 55,100 75,155 110,210 C 95,160 82,100 80,20 Z"),
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(1, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(220, 215, 200), 0.0),
                        new GradientStop(Color.FromRgb(70, 15, 20), 0.5),
                        new GradientStop(Color.FromRgb(15, 10, 15), 1.0)
                    }
                },
                Stroke = new SolidColorBrush(Color.FromArgb(180, 255, 240, 200)),
                StrokeThickness = 2
            };
            rightFang.Effect = new DropShadowEffect
            {
                BlurRadius = 15,
                Color = Color.FromRgb(57, 255, 20),
                Opacity = 0.7,
                ShadowDepth = 0
            };
            rightFangCanvas.Children.Add(rightFang);

            var rightVenomDrop = new Path
            {
                Data = Geometry.Parse("M 110,210 Q 105,235 110,245 Q 115,235 110,210 Z"),
                Fill = new SolidColorBrush(Color.FromArgb(230, 57, 255, 20))
            };
            rightFangCanvas.Children.Add(rightVenomDrop);

            Canvas.SetLeft(rightFangCanvas, cx - 10);
            Canvas.SetTop(rightFangCanvas, cy);
            parent.Children.Add(rightFangCanvas);
        }

        /// <summary>
        /// Builds the colossal Mutant Vampire Mosquito horror face with giant hypodermic needle proboscis,
        /// glowing compound eyes, and blood spray impact decal.
        /// </summary>
        public void BuildMutantMosquito(double screenWidth, double screenHeight)
        {
            Clear();

            MonsterRoot = new Canvas
            {
                Width = 840,
                Height = 840,
                IsHitTestVisible = false
            };

            var transformGroup = new TransformGroup();
            MonsterScaleTransform = new ScaleTransform(1.0, 1.0, 420, 420);
            MonsterJitterTransform = new TranslateTransform(0, 0);
            transformGroup.Children.Add(MonsterScaleTransform);
            transformGroup.Children.Add(MonsterJitterTransform);
            MonsterRoot.RenderTransform = transformGroup;

            double cy = 350;

            // 1. Massive Blurred Translucent Insect Wings
            var leftWing = new Path
            {
                Data = Geometry.Parse("M 380,260 C 200,60 -60,120 -180,240 C -80,340 180,360 380,280 Z"),
                Fill = new SolidColorBrush(Color.FromArgb(90, 230, 245, 255)),
                Stroke = new SolidColorBrush(Color.FromArgb(160, 255, 50, 50)),
                StrokeThickness = 2
            };
            var rightWing = new Path
            {
                Data = Geometry.Parse("M 460,260 C 640,60 900,120 1020,240 C 920,340 660,360 460,280 Z"),
                Fill = new SolidColorBrush(Color.FromArgb(90, 230, 245, 255)),
                Stroke = new SolidColorBrush(Color.FromArgb(160, 255, 50, 50)),
                StrokeThickness = 2
            };
            MonsterRoot.Children.Add(leftWing);
            MonsterRoot.Children.Add(rightWing);

            // 2. Thorax & Head
            var head = new Path
            {
                Data = Geometry.Parse("M 310,260 C 310,160 530,160 530,260 C 530,340 480,410 420,430 C 360,410 310,340 310,260 Z"),
                Fill = new RadialGradientBrush
                {
                    Center = new Point(0.5, 0.4),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(40, 10, 15), 0.0),
                        new GradientStop(Color.FromRgb(15, 6, 8), 0.7),
                        new GradientStop(Color.FromRgb(3, 2, 3), 1.0)
                    }
                },
                Stroke = new SolidColorBrush(Color.FromArgb(160, 255, 30, 30)),
                StrokeThickness = 3
            };
            head.Effect = new DropShadowEffect
            {
                BlurRadius = 25,
                Color = Color.FromRgb(255, 0, 30),
                Opacity = 0.7,
                ShadowDepth = 0
            };
            MonsterRoot.Children.Add(head);

            // 3. Huge Bloodshot Compound Eyes
            double[] eyeX = { 335, 505 };
            foreach (double ex in eyeX)
            {
                var compEye = new Ellipse
                {
                    Width = 78,
                    Height = 96,
                    Fill = new RadialGradientBrush
                    {
                        Center = new Point(0.4, 0.4),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(255, 30, 50), 0.0),
                            new GradientStop(Color.FromRgb(180, 0, 25), 0.5),
                            new GradientStop(Color.FromRgb(60, 0, 10), 1.0)
                        }
                    },
                    Effect = new DropShadowEffect
                    {
                        BlurRadius = 24,
                        Color = Color.FromRgb(255, 0, 40),
                        Opacity = 0.95,
                        ShadowDepth = 0
                    }
                };
                Canvas.SetLeft(compEye, ex - 39);
                Canvas.SetTop(compEye, cy - 130);
                MonsterRoot.Children.Add(compEye);

                var hexGlint = new Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))
                };
                Canvas.SetLeft(hexGlint, ex - 18);
                Canvas.SetTop(hexGlint, cy - 110);
                MonsterRoot.Children.Add(hexGlint);
            }

            // 4. Feathery Sensory Antennae
            var leftAnt = new Path
            {
                Data = Geometry.Parse("M 380,180 Q 300,90 200,40"),
                Stroke = new SolidColorBrush(Color.FromRgb(200, 50, 50)),
                StrokeThickness = 5
            };
            var rightAnt = new Path
            {
                Data = Geometry.Parse("M 460,180 Q 540,90 640,40"),
                Stroke = new SolidColorBrush(Color.FromRgb(200, 50, 50)),
                StrokeThickness = 5
            };
            MonsterRoot.Children.Add(leftAnt);
            MonsterRoot.Children.Add(rightAnt);

            // 5. Colossal Hypodermic Needle Proboscis Stabbing Forward
            var proboscis = new Path
            {
                Data = Geometry.Parse("M 406,390 L 414,750 L 420,810 L 426,750 L 434,390 Z"),
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(20, 10, 15), 0.0),
                        new GradientStop(Color.FromRgb(160, 0, 20), 0.4),
                        new GradientStop(Color.FromRgb(255, 20, 30), 0.85),
                        new GradientStop(Color.FromRgb(240, 230, 230), 1.0)
                    }
                },
                Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 80, 80)),
                StrokeThickness = 2.5
            };
            proboscis.Effect = new DropShadowEffect
            {
                BlurRadius = 20,
                Color = Color.FromRgb(255, 0, 30),
                Opacity = 0.85,
                ShadowDepth = 0
            };
            MonsterRoot.Children.Add(proboscis);

            // Needle impact blood splatter burst decal
            var impactBlood = new Path
            {
                Data = Geometry.Parse("M 420,800 C 440,780 470,790 460,815 C 480,830 450,860 420,845 C 390,860 360,830 380,815 C 370,790 400,780 420,800 Z"),
                Fill = new SolidColorBrush(Color.FromArgb(240, 200, 0, 15))
            };
            MonsterRoot.Children.Add(impactBlood);

            Canvas.SetLeft(MonsterRoot, (screenWidth - 840) / 2.0);
            Canvas.SetTop(MonsterRoot, (screenHeight - 840) / 2.0);

            _container.Children.Add(MonsterRoot);
        }

        public void AnimateFangSnap()
        {
            if (FangLeftTransform == null || FangRightTransform == null) return;

            var biteLeft = new DoubleAnimation(-28, 6, TimeSpan.FromMilliseconds(38))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            var biteRight = new DoubleAnimation(28, -6, TimeSpan.FromMilliseconds(38))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            FangLeftTransform.BeginAnimation(RotateTransform.AngleProperty, biteLeft);
            FangRightTransform.BeginAnimation(RotateTransform.AngleProperty, biteRight);
        }
    }
}
