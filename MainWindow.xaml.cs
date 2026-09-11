using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DigitalMosquito
{
    public partial class MainWindow : Window
    {
        private readonly OverlayManager _overlayManager = new();
        private readonly GameState _gameState = new();

        private Mosquito? _mosquito;
        private MosquitoRenderer? _mosquitoRenderer;
        private BloodSplatterSystem? _bloodSystem;
        private InputManager? _inputManager;

        private DateTime _lastFrameTime = DateTime.UtcNow;
        private bool _isClosing = false;

        private readonly DispatcherTimer _respawnTimer = new();

        public MainWindow()
        {
            InitializeComponent();

            _respawnTimer.Interval = TimeSpan.FromMilliseconds(750);
            _respawnTimer.Tick += OnRespawnTimerTick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize Win32 styles (Transparent, Layered, ToolWindow, NoActivate, Topmost)
            _overlayManager.InitializeOverlay(this);

            // Initialize Mosquito and Vector Renderer
            _mosquito = new Mosquito();
            _mosquito.ResetToRandomPosition(ActualWidth, ActualHeight);

            _mosquitoRenderer = new MosquitoRenderer();
            MosquitoContainer.Children.Add(_mosquitoRenderer.VisualElement);

            // Initialize Blood Splatter system
            _bloodSystem = new BloodSplatterSystem(BloodCanvas);

            // Initialize GameState events
            _gameState.MosquitoKilled += OnMosquitoKilled;
            _gameState.BloodCleaned += OnBloodCleaned;
            _gameState.KillCountChanged += OnKillCountChanged;

            // Initialize InputManager (Windows low-level mouse hook and hit-testing)
            _inputManager = new InputManager(this, _overlayManager, _mosquito, _bloodSystem, _gameState);
            _inputManager.SwatClicked += OnSwatClicked;
            _inputManager.Wiping += OnWiping;

            // Attach 60 FPS Render loop
            CompositionTarget.Rendering += OnRenderFrame;
        }

        private void OnRenderFrame(object? sender, EventArgs e)
        {
            if (_isClosing || _mosquito == null || _mosquitoRenderer == null)
                return;

            DateTime now = DateTime.UtcNow;
            double deltaTime = (now - _lastFrameTime).TotalSeconds;
            _lastFrameTime = now;

            // Clamp delta time to avoid huge physics jumps if window was frozen
            if (deltaTime > 0.1) deltaTime = 0.016;

            if (_gameState.CurrentPhase == GamePhase.Flying)
            {
                _mosquito.Update(deltaTime, ActualWidth, ActualHeight);
                _mosquitoRenderer.Render(_mosquito);
            }

            // Keep overlay topmost and verify cursor state
            _inputManager?.CheckCursorState();
        }

        private void OnSwatClicked(Point pt)
        {
            if (_gameState.CurrentPhase == GamePhase.Flying)
            {
                KillMosquitoAt(pt);
            }
        }

        private void OnWiping(Point pt)
        {
            if (_gameState.CurrentPhase == GamePhase.DeadSplatter ||
                _gameState.CurrentPhase == GamePhase.Cleaning)
            {
                PerformWipe(pt);
            }
        }

        private void KillMosquitoAt(Point pt)
        {
            if (_mosquito == null || _mosquitoRenderer == null || _bloodSystem == null)
                return;

            // Hide mosquito
            _mosquitoRenderer.SetVisibility(false);

            // Trigger game state
            _gameState.Kill(pt);

            // Spawn blood splatter
            _bloodSystem.SpawnSplatter(pt);

            // Play impact ripple animation
            TriggerImpactRipple(pt);

            // Position and show cleaning prompt near blood splatter
            ShowCleaningPrompt(pt);
        }

        private void TriggerImpactRipple(Point pt)
        {
            Canvas.SetLeft(ImpactRipple, pt.X - 5);
            Canvas.SetTop(ImpactRipple, pt.Y - 5);
            ImpactRipple.Width = 10;
            ImpactRipple.Height = 10;
            ImpactRipple.Opacity = 0.9;

            var sizeAnim = new DoubleAnimation(10, 90, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var opacityAnim = new DoubleAnimation(0.9, 0.0, TimeSpan.FromMilliseconds(300));

            sizeAnim.Completed += (s, ev) =>
            {
                ImpactRipple.Opacity = 0;
            };

            ImpactRipple.BeginAnimation(WidthProperty, sizeAnim);
            ImpactRipple.BeginAnimation(HeightProperty, sizeAnim);
            ImpactRipple.BeginAnimation(OpacityProperty, opacityAnim);
        }

        private void ShowCleaningPrompt(Point pt)
        {
            double promptX = pt.X - 90;
            double promptY = pt.Y + 65;

            // Clamp inside screen bounds
            if (promptX < 20) promptX = 20;
            if (promptX > ActualWidth - 220) promptX = ActualWidth - 220;
            if (promptY > ActualHeight - 60) promptY = pt.Y - 75;

            Canvas.SetLeft(CleaningPrompt, promptX);
            Canvas.SetTop(CleaningPrompt, promptY);

            CleaningProgressBar.Value = 0;
            CleaningPrompt.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)));
        }

        private void PerformWipe(Point pt)
        {
            if (_bloodSystem == null || !_bloodSystem.IsActive)
                return;

            _gameState.StartCleaning();

            bool wiped = _bloodSystem.WipeAt(pt, wipeRadius: 38.0, power: 0.26);
            if (wiped)
            {
                double cleanPercent = _bloodSystem.CleanProgress;
                CleaningProgressBar.Value = cleanPercent * 100;

                // When 92%+ cleaned, trigger complete cleaning
                if (cleanPercent >= 0.92)
                {
                    _gameState.CompleteCleaning();
                }
            }
        }

        private void OnMosquitoKilled(Point hitLocation)
        {
            // Handled in KillMosquitoAt
        }

        private void OnBloodCleaned()
        {
            if (_bloodSystem == null) return;

            Point center = _bloodSystem.Center;

            // Fade out prompt
            CleaningPrompt.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200)));

            // Show Cleaned Sparkle
            Canvas.SetLeft(SparkleFx, center.X - 40);
            Canvas.SetTop(SparkleFx, center.Y - 20);

            var sparkleOpacity = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
            {
                AutoReverse = true
            };
            sparkleOpacity.Completed += (s, ev) =>
            {
                SparkleFx.Opacity = 0;
            };
            SparkleFx.BeginAnimation(OpacityProperty, sparkleOpacity);

            // Clean up blood particles
            _bloodSystem.DissolveRemaining();

            // Set overlay to click-through while waiting for respawn
            _overlayManager.SetClickThrough(true);

            // Start respawn timer
            _respawnTimer.Start();
        }

        private void OnRespawnTimerTick(object? sender, EventArgs e)
        {
            _respawnTimer.Stop();

            if (_mosquito != null && _mosquitoRenderer != null)
            {
                _mosquito.ResetToRandomPosition(ActualWidth, ActualHeight);
                _mosquitoRenderer.SetVisibility(true);
                _gameState.Respawn();
            }
        }

        private void OnKillCountChanged(int kills)
        {
            KillCountText.Text = $"{kills} {(kills == 1 ? "Kill" : "Kills")}";

            // Subtle bounce animation on kill badge
            var anim = new DoubleAnimation(1.0, 0.4, TimeSpan.FromMilliseconds(150))
            {
                AutoReverse = true
            };
            HudBadge.BeginAnimation(OpacityProperty, anim);
        }

        // Direct WPF mouse fallback handlers in case click lands directly on window surface
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition(OverlayCanvas);

            if (_gameState.CurrentPhase == GamePhase.Flying)
            {
                if (_mosquito != null && _mosquito.IsHit(pt))
                {
                    KillMosquitoAt(pt);
                    e.Handled = true;
                }
            }
            else if (_gameState.CurrentPhase == GamePhase.DeadSplatter ||
                     _gameState.CurrentPhase == GamePhase.Cleaning)
            {
                if (_bloodSystem != null && _bloodSystem.IsOverSplatter(pt))
                {
                    PerformWipe(pt);
                    e.Handled = true;
                }
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point pt = e.GetPosition(OverlayCanvas);

            if (_gameState.CurrentPhase == GamePhase.DeadSplatter ||
                _gameState.CurrentPhase == GamePhase.Cleaning)
            {
                if (_bloodSystem != null && _bloodSystem.IsOverSplatter(pt))
                {
                    PerformWipe(pt);
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _isClosing = true;
            CompositionTarget.Rendering -= OnRenderFrame;
            _respawnTimer.Stop();
            _inputManager?.Dispose();
        }
    }
}