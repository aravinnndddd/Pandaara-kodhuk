using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DigitalMosquito
{
    public class JumpscareManager
    {
        private readonly Window _window;
        private readonly Canvas _overlayCanvas;
        private readonly TranslateTransform _screenShakeTransform;
        private readonly GlassCrackRenderer _crackRenderer;
        private readonly NightmareMonsterRenderer _nightmareRenderer;
        private readonly FrameworkElement? _horrorFlashOverlay;
        private readonly FrameworkElement? _blackoutOverlay;
        private readonly Random _random = new();

        public bool IsJumpscareActive { get; private set; } = false;

        public JumpscareManager(
            Window window,
            Canvas overlayCanvas,
            Canvas glassCrackCanvas,
            Canvas nightmareCanvas,
            TranslateTransform screenShakeTransform,
            FrameworkElement? horrorFlashOverlay = null,
            FrameworkElement? blackoutOverlay = null)
        {
            _window = window;
            _overlayCanvas = overlayCanvas;
            _screenShakeTransform = screenShakeTransform;
            _crackRenderer = new GlassCrackRenderer(glassCrackCanvas);
            _nightmareRenderer = new NightmareMonsterRenderer(nightmareCanvas);
            _horrorFlashOverlay = horrorFlashOverlay;
            _blackoutOverlay = blackoutOverlay;
        }

        public void TriggerSpiderGlassPounce(Creature creature, Action? onCompleted = null)
        {
            if (IsJumpscareActive || creature == null) return;
            IsJumpscareActive = true;

            double screenWidth = _window.ActualWidth > 100 ? _window.ActualWidth : SystemParameters.PrimaryScreenWidth;
            double screenHeight = _window.ActualHeight > 100 ? _window.ActualHeight : SystemParameters.PrimaryScreenHeight;

            Point pouncePoint = new Point(creature.DisplayX, creature.DisplayY);

            // 1. Play AAA Cinema-Grade Terrifying Horror Scream Audio
            SoundManager.Instance.PlayJumpscare(forceAudio: true);

            // 2. Blackout strobe + Blinding Bloody Red horror flash
            TriggerHorrorFlash();

            // 3. Spawn massive cracked glass & web fracture decal
            double crackRadius = creature is GiantSpider ? 380.0 : 290.0;
            _crackRenderer.SpawnCrackAt(pouncePoint, radius: crackRadius);

            // 4. Violent high-magnitude Screen Shake (56px magnitude, 24 frames over 440ms)
            PerformScreenShake(magnitude: 56.0, durationMs: 440);

            // 5. Construct and launch the Nightmare Monster horror face
            if (creature is Mosquito)
            {
                _nightmareRenderer.BuildMutantMosquito(screenWidth, screenHeight);
            }
            else
            {
                _nightmareRenderer.BuildDemonArachnid(screenWidth, screenHeight);
            }

            // 6. Explosive 35ms Camera Rush right into the player's face
            if (_nightmareRenderer.MonsterScaleTransform != null)
            {
                var scaleXAnim = new DoubleAnimation(0.10, 1.15, TimeSpan.FromMilliseconds(35))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var scaleYAnim = new DoubleAnimation(0.10, 1.15, TimeSpan.FromMilliseconds(35))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                scaleXAnim.Completed += (s, e) =>
                {
                    // Snap fangs shut violently on impact!
                    _nightmareRenderer.AnimateFangSnap();

                    // Frantic violent thrashing and leg spasm on the glass (24 frames of high-frequency jitter)
                    var jitterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
                    int jitterCount = 0;
                    jitterTimer.Tick += (js, je) =>
                    {
                        jitterCount++;
                        if (jitterCount > 24)
                        {
                            jitterTimer.Stop();
                            return;
                        }

                        if (_nightmareRenderer.MonsterJitterTransform != null)
                        {
                            _nightmareRenderer.MonsterJitterTransform.X = (_random.NextDouble() * 2.0 - 1.0) * 18.0;
                            _nightmareRenderer.MonsterJitterTransform.Y = (_random.NextDouble() * 2.0 - 1.0) * 18.0;
                        }
                    };
                    jitterTimer.Start();

                    // Cling aggressively for 650ms, then retreat or die
                    var clingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(650) };
                    clingTimer.Tick += (ts, te) =>
                    {
                        clingTimer.Stop();

                        // Rapid shrink and fade
                        var shrinkX = new DoubleAnimation(1.15, 0.05, TimeSpan.FromMilliseconds(180))
                        {
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        var shrinkY = new DoubleAnimation(1.15, 0.05, TimeSpan.FromMilliseconds(180))
                        {
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        var fadeAnim = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(180));

                        shrinkX.Completed += (ss, se) =>
                        {
                            _nightmareRenderer.Clear();
                            IsJumpscareActive = false;
                            onCompleted?.Invoke();
                        };

                        _nightmareRenderer.MonsterScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, shrinkX);
                        _nightmareRenderer.MonsterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, shrinkY);
                        _nightmareRenderer.MonsterRoot.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
                    };
                    clingTimer.Start();
                };

                _nightmareRenderer.MonsterScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
                _nightmareRenderer.MonsterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
            }
        }

        private void TriggerHorrorFlash()
        {
            // 1. Sudden pitch blackout spike (0 - 25ms)
            if (_blackoutOverlay != null)
            {
                var blackAnim = new DoubleAnimationUsingKeyFrames();
                blackAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.95, TimeSpan.FromMilliseconds(12)));
                blackAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, TimeSpan.FromMilliseconds(30)));
                _blackoutOverlay.BeginAnimation(UIElement.OpacityProperty, blackAnim);
            }

            // 2. Blinding bloody red horror vignette strobe
            if (_horrorFlashOverlay != null)
            {
                var flashAnim = new DoubleAnimationUsingKeyFrames();
                flashAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, TimeSpan.FromMilliseconds(20)));
                flashAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.98, TimeSpan.FromMilliseconds(40))); // Sudden blinding red strike
                flashAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.45, TimeSpan.FromMilliseconds(110)));
                flashAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.85, TimeSpan.FromMilliseconds(170))); // Secondary shock
                flashAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.50, TimeSpan.FromMilliseconds(260)));
                flashAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, TimeSpan.FromMilliseconds(460)));

                _horrorFlashOverlay.BeginAnimation(UIElement.OpacityProperty, flashAnim);
            }
        }

        public void PerformScreenShake(double magnitude = 56.0, int durationMs = 440)
        {
            var xAnim = new DoubleAnimationUsingKeyFrames();
            var yAnim = new DoubleAnimationUsingKeyFrames();

            int frames = 24;
            double frameTime = (double)durationMs / frames;

            for (int i = 0; i < frames; i++)
            {
                double decay = 1.0 - ((double)i / frames);
                double currentMag = magnitude * Math.Pow(decay, 1.15);
                double offsetX = (_random.NextDouble() * 2.0 - 1.0) * currentMag;
                double offsetY = (_random.NextDouble() * 2.0 - 1.0) * currentMag;

                var time = TimeSpan.FromMilliseconds(frameTime * (i + 1));
                xAnim.KeyFrames.Add(new LinearDoubleKeyFrame(offsetX, time));
                yAnim.KeyFrames.Add(new LinearDoubleKeyFrame(offsetY, time));
            }

            xAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.FromMilliseconds(durationMs)));
            yAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.FromMilliseconds(durationMs)));

            _screenShakeTransform.BeginAnimation(TranslateTransform.XProperty, xAnim);
            _screenShakeTransform.BeginAnimation(TranslateTransform.YProperty, yAnim);
        }
    }
}
