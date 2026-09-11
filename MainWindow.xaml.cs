using System;
using System.Linq;
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
        private readonly Random _random = new();

        private Creature? _activeCreature;
        private BloodSplatterSystem? _bloodSystem;
        private InputManager? _inputManager;
        private JumpscareManager? _jumpscareManager;
        private WeaponAnimationManager? _weaponManager;

        private PlayerSaveData _saveData = new();
        private LevelDefinition _currentLevelDef = ProgressionManager.Levels[0];
        private CleanerDefinition _currentCleaner = CleanerProgression.Cleaners[0];
        private readonly Random _cleanerRand = new();
        private int _totalXp = 0;
        private bool _isLevelUpFreezing = false;
        private bool _isHudCollapsed = false;
        private bool _isPaused = false;

        private Point _lastTrackedMousePos;
        private double _mouseIdleTimer = 0;

        private DateTime _lastFrameTime = DateTime.UtcNow;
        private bool _isClosing = false;
        private double _baseSpeedMultiplier = 1.0;

        private readonly DispatcherTimer _respawnTimer = new();
        private readonly DispatcherTimer _resetConfirmTimer = new();
        private bool _isResetConfirming = false;

        public MainWindow()
        {
            try
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"), $"[{DateTime.UtcNow:O}] MainWindow constructor\n");
            }
            catch { }
            InitializeComponent();

            _respawnTimer.Tick += OnRespawnTimerTick;

            _resetConfirmTimer.Interval = TimeSpan.FromSeconds(3.5);
            _resetConfirmTimer.Tick += (s, e) =>
            {
                _resetConfirmTimer.Stop();
                _isResetConfirming = false;
                ResetTxt.Text = "🔄";
                ResetTxt.Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                ResetBtn.Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));
                ResetBtn.ToolTip = "Reset Kills & Progress (R)";
                ResetProgressSettingsTxt.Text = "⚠️ Reset Progress";
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"), $"[{DateTime.UtcNow:O}] MainWindow.Window_Loaded start\n");
            }
            catch { }

            try
            {
                // Initialize Win32 styles (Transparent, Layered, ToolWindow, NoActivate, Topmost)
                _overlayManager.InitializeOverlay(this);

                // Initialize Blood Splatter system
                _bloodSystem = new BloodSplatterSystem(BloodCanvas);

                // Initialize Weapon Animation Manager
                _weaponManager = new WeaponAnimationManager(WeaponFxCanvas);

                // Load persistent player progression
                _saveData = SaveManager.Load();
                _totalXp = _saveData.TotalXP;
                _gameState.RestoreKills(_saveData.TotalKills);
                _currentLevelDef = ProgressionManager.GetLevelForKills(_gameState.KillCount);
                _currentCleaner = CleanerProgression.GetCleanerForKills(_gameState.KillCount);
                if (!string.IsNullOrEmpty(_saveData.EquippedWeaponId))
                {
                    var savedWeapon = ProgressionManager.GetDefinitionForWeapon(_saveData.EquippedWeaponId);
                    if (savedWeapon.Level <= _currentLevelDef.Level)
                    {
                        _currentLevelDef = savedWeapon;
                    }
                }

                _baseSpeedMultiplier = _saveData.BaseSpeedMultiplier;
                GameConfig.Instance.SoundEnabled = !_saveData.SoundMuted;
                SoundManager.Instance.IsMuted = _saveData.SoundMuted;
                GameConfig.Instance.JumpscaresEnabled = _saveData.JumpscaresEnabled;
                GameConfig.Instance.JumpscareChance = _saveData.JumpscareChance;

                if (!string.IsNullOrEmpty(_saveData.SelectedCreatureType) &&
                    Enum.TryParse<CreatureSelectionMode>(_saveData.SelectedCreatureType, out var savedMode))
                {
                    GameConfig.Instance.SelectionMode = savedMode;
                }

                if (_saveData.HudCollapsed)
                {
                    SetHudCollapsed(true);
                }

                // Initialize GameState events
                _gameState.CreatureKilled += OnCreatureKilled;
                _gameState.BloodCleaned += OnBloodCleaned;
                _gameState.KillCountChanged += OnKillCountChanged;

                // Spawn initial creature
                SpawnNextCreature();

                // Initialize InputManager (Windows low-level mouse hook and hit-testing)
                _inputManager = new InputManager(
                    this,
                    _overlayManager,
                    () => _activeCreature,
                    _bloodSystem,
                    _gameState);

                _inputManager.SwatClicked += OnSwatClicked;
                _inputManager.Wiping += OnWiping;

                // Enable HUD hit-testing for interactive control widgets
                _inputManager.IsOverHud = (pt) =>
                {
                    try
                    {
                        // Check Expanded HUD
                        if (HudExpandedContainer.Visibility == Visibility.Visible)
                        {
                            Point hudPos = HudExpandedContainer.TranslatePoint(new Point(0, 0), OverlayCanvas);
                            var bounds = new Rect(hudPos.X, hudPos.Y, HudExpandedContainer.ActualWidth, HudExpandedContainer.ActualHeight);
                            bounds.Inflate(6, 6);
                            if (bounds.Contains(pt)) return true;
                        }

                        // Check Collapsed Pill
                        if (HudCollapsedPill.Visibility == Visibility.Visible)
                        {
                            Point pillPos = HudCollapsedPill.TranslatePoint(new Point(0, 0), OverlayCanvas);
                            var bounds = new Rect(pillPos.X, pillPos.Y, HudCollapsedPill.ActualWidth, HudCollapsedPill.ActualHeight);
                            bounds.Inflate(6, 6);
                            if (bounds.Contains(pt)) return true;
                        }

                        // Check Bottom Progression Bar
                        if (BottomProgressBarContainer.Visibility == Visibility.Visible)
                        {
                            Point barPos = BottomProgressBarContainer.TranslatePoint(new Point(0, 0), OverlayCanvas);
                            var bounds = new Rect(barPos.X, barPos.Y, BottomProgressBarContainer.ActualWidth, BottomProgressBarContainer.ActualHeight);
                            bounds.Inflate(4, 4);
                            if (bounds.Contains(pt)) return true;
                        }

                        // Check LevelUp Banner when active
                        if (LevelUpBanner.Opacity > 0.2)
                        {
                            Point bannerPos = LevelUpBanner.TranslatePoint(new Point(0, 0), OverlayCanvas);
                            var bounds = new Rect(bannerPos.X, bannerPos.Y, LevelUpBanner.ActualWidth, LevelUpBanner.ActualHeight);
                            if (bounds.Contains(pt)) return true;
                        }
                    }
                    catch
                    {
                        // Fallback
                    }
                    return false;
                };

                // Position Bottom Bar and Level-Up Banner centered on screen
                PositionOverlays();

                // Restore HUD and button states
                UpdateSoundButtonState();
                UpdateJumpscareButtonState();
                SetHudCollapsed(_saveData.HudCollapsed);
                UpdateProgressionDisplay();

                // Initialize Jumpscare Manager
                _jumpscareManager = new JumpscareManager(
                    this,
                    OverlayCanvas,
                    GlassCrackCanvas,
                    NightmareCanvas,
                    ScreenShakeTransform,
                    HorrorFlashOverlay,
                    BlackoutOverlay);

                // Attach 60 FPS Render loop
                CompositionTarget.Rendering += OnRenderFrame;

                try
                {
                    System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"), $"[{DateTime.UtcNow:O}] MainWindow.Window_Loaded finished successfully\n");
                }
                catch { }
            }
            catch (Exception ex)
            {
                try
                {
                    System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"), $"[{DateTime.UtcNow:O}] MainWindow.Window_Loaded EXCEPTION: {ex}\n");
                }
                catch { }
            }
        }

        private void PositionOverlays()
        {
            double screenWidth = ActualWidth > 100 ? ActualWidth : SystemParameters.PrimaryScreenWidth;
            double screenHeight = ActualHeight > 100 ? ActualHeight : SystemParameters.PrimaryScreenHeight;

            // Center Bottom Progression Bar
            Canvas.SetLeft(BottomProgressBarContainer, (screenWidth - 352) / 2.0);
            Canvas.SetBottom(BottomProgressBarContainer, 18);

            // Center Level-Up Banner
            Canvas.SetLeft(LevelUpBanner, (screenWidth - 352) / 2.0);
            Canvas.SetTop(LevelUpBanner, (screenHeight - 220) / 2.0);
        }

        private void SpawnNextCreature()
        {
            if (_activeCreature != null)
            {
                CreatureContainer.Children.Remove(_activeCreature.VisualElement);
            }

            CreatureType nextType = GameConfig.Instance.ChooseNextCreatureType(_gameState.KillCount, _random);

            switch (nextType)
            {
                case CreatureType.BossGiantMosquito:
                    _activeCreature = new BossGiantMosquito();
                    CreatureIconText.Text = "👑";
                    CollapsedCreatureIcon.Text = "👑";
                    break;
                case CreatureType.BossGiantSpider:
                    _activeCreature = new BossGiantSpider();
                    CreatureIconText.Text = "👑";
                    CollapsedCreatureIcon.Text = "👑";
                    break;
                case CreatureType.BossMutantSpider:
                    _activeCreature = new BossMutantSpider();
                    CreatureIconText.Text = "👑";
                    CollapsedCreatureIcon.Text = "👑";
                    break;
                case CreatureType.Spider:
                    _activeCreature = new Spider();
                    CreatureIconText.Text = "🕷️";
                    CollapsedCreatureIcon.Text = "🕷️";
                    break;
                case CreatureType.GiantSpider:
                    _activeCreature = new GiantSpider();
                    CreatureIconText.Text = "💀";
                    CollapsedCreatureIcon.Text = "💀";
                    break;
                case CreatureType.Mosquito:
                default:
                    _activeCreature = new Mosquito();
                    CreatureIconText.Text = "🦟";
                    CollapsedCreatureIcon.Text = "🦟";
                    break;
            }

            ApplyCurrentSpeed();
            CreatureContainer.Children.Add(_activeCreature.VisualElement);
            _activeCreature.ResetToRandomPosition(ActualWidth, ActualHeight);
            _activeCreature.SetVisibility(true);
        }

        private void ApplyCurrentSpeed()
        {
            if (_activeCreature == null) return;
            double difficultyScaling = GameConfig.Instance.GetSpeedScaling(_gameState.KillCount);
            _activeCreature.SpeedMultiplier = _baseSpeedMultiplier * difficultyScaling;
        }

        private void OnRenderFrame(object? sender, EventArgs e)
        {
            if (_isClosing || _activeCreature == null || _isLevelUpFreezing)
                return;

            DateTime now = DateTime.UtcNow;
            double deltaTime = (now - _lastFrameTime).TotalSeconds;
            _lastFrameTime = now;

            if (deltaTime > 0.1) deltaTime = 0.016;

            if (_gameState.CurrentPhase == GamePhase.Flying)
            {
                _activeCreature.Update(deltaTime, ActualWidth, ActualHeight);

                // Play mosquito buzz when flying if sound is enabled
                if (_activeCreature is Mosquito && !SoundManager.Instance.IsMuted)
                {
                    SoundManager.Instance.StartMosquitoBuzz();
                }

                // Ambient ambush jumpscare check
                if (GameConfig.Instance.JumpscaresEnabled &&
                    _activeCreature is Spider &&
                    _jumpscareManager != null && !_jumpscareManager.IsJumpscareActive)
                {
                    if (InputManager.GetCursorPos(out var pt))
                    {
                        var screenPt = new Point(pt.X, pt.Y);
                        if ((screenPt - _lastTrackedMousePos).Length < 8.0)
                        {
                            _mouseIdleTimer += deltaTime;
                            if (_mouseIdleTimer > 6.0)
                            {
                                _mouseIdleTimer = 0;
                                double dist = (screenPt - new Point(_activeCreature.DisplayX, _activeCreature.DisplayY)).Length;
                                if (dist < 320 && _random.NextDouble() < 0.40)
                                {
                                    _jumpscareManager.TriggerSpiderGlassPounce(_activeCreature);
                                }
                            }
                        }
                        else
                        {
                            _lastTrackedMousePos = screenPt;
                            _mouseIdleTimer = 0;
                        }
                    }
                }
            }
            else if (_gameState.CurrentPhase == GamePhase.DeadSplatter ||
                     _gameState.CurrentPhase == GamePhase.Cleaning)
            {
                _bloodSystem?.UpdateFade(deltaTime);
            }

            _inputManager?.CheckCursorState();
        }

        private void OnSwatClicked(Point pt)
        {
            if (_gameState.CurrentPhase != GamePhase.Flying || _activeCreature == null || _isLevelUpFreezing)
                return;

            string weaponId = _currentLevelDef.WeaponId;

            // 1. Play weapon-specific attack sound
            SoundManager.Instance.PlayWeaponAttack(weaponId);

            // 2. Overlay-only screen shake if equipped weapon has weight
            if (_currentLevelDef.OverlayShakeMagnitude > 0)
            {
                _jumpscareManager?.PerformScreenShake(_currentLevelDef.OverlayShakeMagnitude, 160);
            }

            // 3. Play animated weapon vector attack on the weapon layer
            _weaponManager?.PlayAttack(
                weaponId,
                pt,
                _activeCreature,
                onImpact: () =>
                {
                    // Terrifying horror jumpscare chance on swat attempt against any creature
                    if (GameConfig.Instance.JumpscaresEnabled &&
                        _activeCreature != null &&
                        _jumpscareManager != null && !_jumpscareManager.IsJumpscareActive &&
                        _random.NextDouble() < GameConfig.Instance.JumpscareChance)
                    {
                        _jumpscareManager.TriggerSpiderGlassPounce(_activeCreature, () =>
                        {
                            ProcessDamageOrKill(pt);
                        });
                        return;
                    }

                    if (weaponId == "washer" && _bloodSystem != null && _bloodSystem.IsActive)
                    {
                        PerformWipe(pt);
                    }

                    ProcessDamageOrKill(pt);
                },
                onFinished: () =>
                {
                });
        }

        private void ProcessDamageOrKill(Point pt)
        {
            if (_activeCreature == null) return;

            bool isDead = _activeCreature.TakeDamage(_currentLevelDef.AttackDamage);
            if (isDead)
            {
                KillCreatureAt(pt);
            }
            else
            {
                // Boss took damage but still alive
                TriggerImpactRipple(pt, 75);
                SoundManager.Instance.PlaySmack();
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

        private void KillCreatureAt(Point pt)
        {
            if (_activeCreature == null || _bloodSystem == null)
                return;

            SoundManager.Instance.StopMosquitoBuzz();

            CreatureType type = _activeCreature.CreatureType;
            _totalXp += _activeCreature.XpReward;

            // Blood scaling based on Creature + Weapon combination
            BloodConfig bloodConfig = _activeCreature.BloodConfig.WithWeaponModifier(
                _currentLevelDef.BloodParticleMultiplier,
                _currentLevelDef.BloodRadiusMultiplier);

            // Hide creature visual
            _activeCreature.SetVisibility(false);

            int previousLevel = _currentLevelDef.Level;

            // Trigger game state (increments KillCount)
            _gameState.Kill(pt, type);

            // Spawn scaled blood splatter
            _bloodSystem.SpawnSplatter(pt, bloodConfig);

            // Play impact ripple animation
            TriggerImpactRipple(pt, bloodConfig.SplatterRadius);

            // Position and show cleaning prompt near blood splatter
            ShowCleaningPrompt(pt);

            // Update bottom progress bar and top HUD
            var newLevelDef = ProgressionManager.GetLevelForKills(_gameState.KillCount);
            UpdateProgressionDisplay();
            SaveCurrentGame();

            // Trigger Level-Up Celebration if tier increased!
            if (newLevelDef.Level > previousLevel)
            {
                TriggerLevelUpExperience(newLevelDef);
            }
        }

        private void TriggerLevelUpExperience(LevelDefinition newLevelDef)
        {
            _isLevelUpFreezing = true;
            SoundManager.Instance.StopMosquitoBuzz();
            SoundManager.Instance.PlayLevelUp(forceAudio: true);

            // Automatically equip unlocked weapon
            _currentLevelDef = newLevelDef;
            _saveData.EquippedWeaponId = newLevelDef.WeaponId;

            var prevCleaner = _currentCleaner;
            var newCleaner = CleanerProgression.GetCleanerForKills(_gameState.KillCount);
            _currentCleaner = newCleaner;

            LevelUpLevelText.Text = $"LEVEL {newLevelDef.Level}";
            LevelUpWeaponIcon.Text = newLevelDef.WeaponIcon;
            LevelUpWeaponName.Text = $"{newLevelDef.WeaponName.ToUpperInvariant()} UNLOCKED";

            if (newCleaner.Tier > prevCleaner.Tier)
            {
                LevelUpMilestoneText.Text = $"{newLevelDef.RequiredKills} KILLS • NEW CLEANER: {newCleaner.Icon} {newCleaner.Name.ToUpperInvariant()}!";
            }
            else
            {
                LevelUpMilestoneText.Text = $"{newLevelDef.RequiredKills} KILLS REACHED";
            }

            PositionOverlays();

            // Animate banner bounce & fade in
            var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(180));
            var scaleX = new DoubleAnimation(0.6, 1.0, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new BackEase { Amplitude = 0.4, EasingMode = EasingMode.EaseOut }
            };
            var scaleY = new DoubleAnimation(0.6, 1.0, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new BackEase { Amplitude = 0.4, EasingMode = EasingMode.EaseOut }
            };

            LevelUpBanner.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            LevelUpScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            LevelUpScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);

            // Freeze gameplay for 1.6s, then fade out banner and resume
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1600) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(220));
                fadeOut.Completed += (fs, fe) =>
                {
                    _isLevelUpFreezing = false;
                    SaveCurrentGame();
                };
                LevelUpBanner.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            };
            timer.Start();
        }

        private void UpdateProgressionDisplay()
        {
            ProgressionManager.GetProgressToNextLevel(
                _gameState.KillCount,
                out var currentDef,
                out var nextDef,
                out int curKillsInLevel,
                out int reqKillsForNext,
                out double pct);

            BottomLevelText.Text = $"LV {currentDef.Level}";
            BottomWeaponIcon.Text = currentDef.WeaponIcon;
            BottomWeaponName.Text = currentDef.WeaponName;

            if (nextDef != null)
            {
                BottomKillsProgressText.Text = $"{_gameState.KillCount} / {nextDef.RequiredKills} KILLS";
                BottomNextUnlockHint.Text = $"Next: {nextDef.WeaponIcon} {nextDef.WeaponName}";
            }
            else
            {
                BottomKillsProgressText.Text = $"{_gameState.KillCount} KILLS (MAX)";
                BottomNextUnlockHint.Text = "👑 Boss Mode Master";
            }

            // Animate glowing progress bar fill
            double targetWidth = Math.Clamp(pct * 320.0, 0, 320.0);
            var widthAnim = new DoubleAnimation(BottomProgressBarFill.Width, targetWidth, TimeSpan.FromMilliseconds(200));
            BottomProgressBarFill.BeginAnimation(WidthProperty, widthAnim);

            // Update cleaner tier display
            _currentCleaner = CleanerProgression.GetCleanerForKills(_gameState.KillCount);
            BottomCleanerTierText.Text = $"Cleaner: {_currentCleaner.Icon} {_currentCleaner.Name}";

            BottomXpText.Text = $"★ {_totalXp} XP";

            // Update Top Compact HUD: 🦟  LV5  25  ⏸ 🔊 ⚙ ×
            DifficultyText.Text = $"LV{currentDef.Level}";
            KillCountText.Text = $"{_gameState.KillCount}";
            HudCollapsedPill.ToolTip = $"Pandaara കൊതുക് (LV{currentDef.Level}, {_gameState.KillCount} Kills) - Click to expand";
        }

        private void TriggerImpactRipple(Point pt, double radius)
        {
            Canvas.SetLeft(ImpactRipple, pt.X - 5);
            Canvas.SetTop(ImpactRipple, pt.Y - 5);
            ImpactRipple.Width = 10;
            ImpactRipple.Height = 10;
            ImpactRipple.Opacity = 0.95;

            double targetSize = Math.Max(70, radius * 0.9);
            var sizeAnim = new DoubleAnimation(10, targetSize, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var opacityAnim = new DoubleAnimation(0.95, 0.0, TimeSpan.FromMilliseconds(260));

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
            _currentCleaner = CleanerProgression.GetCleanerForKills(_gameState.KillCount);

            double screenWidth = ActualWidth > 100 ? ActualWidth : SystemParameters.PrimaryScreenWidth;
            double screenHeight = ActualHeight > 100 ? ActualHeight : SystemParameters.PrimaryScreenHeight;

            double promptX = pt.X - 90;
            double promptY = pt.Y + 45;

            if (promptX < 20) promptX = 20;
            if (promptX > screenWidth - 270) promptX = screenWidth - 270;
            if (promptY > screenHeight - 90) promptY = pt.Y - 65;
            if (promptY < 20) promptY = pt.Y + 35;
            if (promptY < 20) promptY = 20;

            Canvas.SetLeft(CleaningPrompt, promptX);
            Canvas.SetTop(CleaningPrompt, promptY);

            CleaningToolIcon.Text = _currentCleaner.Icon;
            CleaningToolName.Text = _currentCleaner.Name;
            CleaningBonusXpText.Text = $"+{_currentCleaner.BonusCleanXp} Clean XP";
            CleaningProgressBar.Value = 0;
            CleaningPromptText.Text = $"Rub to clean with {_currentCleaner.Name}!";

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            CleaningPrompt.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void PerformWipe(Point pt)
        {
            if (_bloodSystem == null || !_bloodSystem.IsActive)
                return;

            _gameState.StartCleaning();

            // Position and display floating companion cleaner cursor
            Canvas.SetLeft(CleanerCursorFx, pt.X + 16);
            Canvas.SetTop(CleanerCursorFx, pt.Y - 26);
            CleanerCursorIcon.Text = _currentCleaner.Icon;
            CleanerCursorName.Text = _currentCleaner.Name;
            if (CleanerCursorFx.Opacity < 0.9)
            {
                CleanerCursorFx.BeginAnimation(OpacityProperty, new DoubleAnimation(0.95, TimeSpan.FromMilliseconds(120)));
            }

            bool wiped = _bloodSystem.WipeAt(pt, wipeRadius: _currentCleaner.WipeRadius, power: _currentCleaner.WipePower);
            if (wiped)
            {
                SoundManager.Instance.PlayWipe();

                // Spawn bubbles / suds if cleaner supports it
                if (_currentCleaner.SpawnsBubbles && _cleanerRand.NextDouble() < 0.65)
                {
                    SpawnCleaningBubble(pt);
                }

                // Spawn laser gleam if ultimate sonic cleaner
                if (_currentCleaner.SpawnsLaserGleam && _cleanerRand.NextDouble() < 0.40)
                {
                    SpawnLaserGleam(pt);
                }

                double cleanPercent = _bloodSystem.CleanProgress;
                CleaningProgressBar.Value = cleanPercent * 100;
                CleaningPromptText.Text = $"CLEANING {(int)(cleanPercent * 100)}%";

                // Complete cleaning if 85% or more has been scrubbed or only <= 3 tiny specks remain
                if (cleanPercent >= 0.85 || _bloodSystem.RemainingUncleanedCount <= 3)
                {
                    CleaningProgressBar.Value = 100;
                    CleaningPromptText.Text = "CLEANING 100%!";
                    CleanerCursorFx.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(150)));
                    _gameState.CompleteCleaning();
                }
            }
        }

        private void SpawnCleaningBubble(Point pt)
        {
            if (BubbleFxCanvas.Children.Count > 35) return;

            double size = _cleanerRand.Next(10, 26);
            double offsetX = (_cleanerRand.NextDouble() - 0.5) * _currentCleaner.WipeRadius * 0.8;
            double offsetY = (_cleanerRand.NextDouble() - 0.5) * _currentCleaner.WipeRadius * 0.8;

            var bubble = new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(size / 2.0),
                Background = new SolidColorBrush(Color.FromArgb(90, 180, 235, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                BorderThickness = new Thickness(1.2),
                IsHitTestVisible = false,
                Opacity = 0.85
            };

            Canvas.SetLeft(bubble, pt.X + offsetX - size / 2.0);
            Canvas.SetTop(bubble, pt.Y + offsetY - size / 2.0);
            BubbleFxCanvas.Children.Add(bubble);

            // Float slightly upward and fade out
            double targetY = pt.Y + offsetY - size / 2.0 - _cleanerRand.Next(15, 40);
            var yAnim = new DoubleAnimation(Canvas.GetTop(bubble), targetY, TimeSpan.FromMilliseconds(450))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var fadeAnim = new DoubleAnimation(0.85, 0.0, TimeSpan.FromMilliseconds(450));
            fadeAnim.Completed += (s, e) =>
            {
                BubbleFxCanvas.Children.Remove(bubble);
            };

            bubble.BeginAnimation(Canvas.TopProperty, yAnim);
            bubble.BeginAnimation(OpacityProperty, fadeAnim);
        }

        private void SpawnLaserGleam(Point pt)
        {
            if (BubbleFxCanvas.Children.Count > 40) return;

            double size = _cleanerRand.Next(18, 38);
            double offsetX = (_cleanerRand.NextDouble() - 0.5) * 60;
            double offsetY = (_cleanerRand.NextDouble() - 0.5) * 60;

            var gleam = new TextBlock
            {
                Text = "✦",
                FontSize = size,
                Foreground = new SolidColorBrush(Color.FromArgb(240, 100, 240, 255)),
                IsHitTestVisible = false,
                Opacity = 1.0
            };
            gleam.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 12,
                Color = Color.FromRgb(50, 220, 255),
                Opacity = 0.9,
                ShadowDepth = 0
            };

            Canvas.SetLeft(gleam, pt.X + offsetX - size / 2.0);
            Canvas.SetTop(gleam, pt.Y + offsetY - size / 2.0);
            BubbleFxCanvas.Children.Add(gleam);

            var fadeAnim = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(300));
            fadeAnim.Completed += (s, e) =>
            {
                BubbleFxCanvas.Children.Remove(gleam);
            };

            gleam.BeginAnimation(OpacityProperty, fadeAnim);
        }

        private void OnCreatureKilled(Point hitLocation, CreatureType type)
        {
            // Handled in KillCreatureAt
        }

        private void OnBloodCleaned()
        {
            if (_bloodSystem == null) return;

            Point center = _bloodSystem.Center;

            // Hide floating cleaner cursor
            CleanerCursorFx.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(150)));

            // Fade out prompt
            CleaningPrompt.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200)));

            // Award bonus clean XP based on cleaner tier
            _totalXp += _currentCleaner.BonusCleanXp;
            BottomXpText.Text = $"★ {_totalXp} XP";

            // Play squeaky clean sound effect
            SoundManager.Instance.PlayCleanSqueak();

            // Show Cleaned Sparkle ("✨ SQUEEGEE CLEAN! +25 XP ✨")
            SparkleFx.Text = $"✨ {_currentCleaner.Name.ToUpperInvariant()} CLEAN! +{_currentCleaner.BonusCleanXp} XP ✨";
            Canvas.SetLeft(SparkleFx, Math.Max(20, center.X - 110));
            Canvas.SetTop(SparkleFx, Math.Max(20, center.Y - 25));

            var sparkleOpacity = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
            {
                AutoReverse = true
            };
            sparkleOpacity.Completed += (s, ev) =>
            {
                SparkleFx.Opacity = 0;
            };
            SparkleFx.BeginAnimation(OpacityProperty, sparkleOpacity);

            _bloodSystem.DissolveRemaining();
            _overlayManager.SetClickThrough(true);

            SaveCurrentGame();

            _respawnTimer.Interval = GameConfig.Instance.GetRespawnInterval(_gameState.KillCount);
            _respawnTimer.Start();
        }

        private void OnRespawnTimerTick(object? sender, EventArgs e)
        {
            _respawnTimer.Stop();

            SpawnNextCreature();
            _gameState.Respawn();
        }

        private void OnKillCountChanged(int kills)
        {
            KillCountText.Text = $"{kills} {(kills == 1 ? "Kill" : "Kills")}";

            ApplyCurrentSpeed();
            UpdateProgressionDisplay();

            var anim = new DoubleAnimation(1.0, 0.4, TimeSpan.FromMilliseconds(150))
            {
                AutoReverse = true
            };
            HudBadge.BeginAnimation(OpacityProperty, anim);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition(OverlayCanvas);

            if (_gameState.CurrentPhase == GamePhase.Flying)
            {
                if (_activeCreature != null && _activeCreature.IsHit(pt))
                {
                    OnSwatClicked(pt);
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
                else if (CleanerCursorFx.Opacity > 0.05)
                {
                    CleanerCursorFx.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(150)));
                }
            }
        }

        private void SpeedBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string tagStr &&
                double.TryParse(tagStr, System.Globalization.CultureInfo.InvariantCulture, out double speed))
            {
                SetSpeed(speed);
                e.Handled = true;
            }
        }

        private void SetSpeed(double speed)
        {
            _baseSpeedMultiplier = speed;
            ApplyCurrentSpeed();

            ResetSpeedButton(SpeedBtnSlow, SpeedTxtSlow, "0.5x");
            ResetSpeedButton(SpeedBtnNormal, SpeedTxtNormal, "1.0x");
            ResetSpeedButton(SpeedBtnFast, SpeedTxtFast, "1.8x");
            ResetSpeedButton(SpeedBtnChaos, SpeedTxtChaos, "⚡ 2.8x");

            if (Math.Abs(speed - 0.5) < 0.01) HighlightSpeedButton(SpeedBtnSlow, SpeedTxtSlow);
            else if (Math.Abs(speed - 1.0) < 0.01) HighlightSpeedButton(SpeedBtnNormal, SpeedTxtNormal);
            else if (Math.Abs(speed - 1.8) < 0.01) HighlightSpeedButton(SpeedBtnFast, SpeedTxtFast);
            else if (Math.Abs(speed - 2.8) < 0.01) HighlightSpeedButton(SpeedBtnChaos, SpeedTxtChaos);

            SaveCurrentGame();
        }

        private static void ResetSpeedButton(Border btn, TextBlock txt, string label)
        {
            btn.Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));
            txt.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            txt.FontWeight = FontWeights.SemiBold;
            txt.Text = label;
        }

        private static void HighlightSpeedButton(Border btn, TextBlock txt)
        {
            btn.Background = new SolidColorBrush(Color.FromArgb(204, 255, 34, 34));
            txt.Foreground = Brushes.White;
            txt.FontWeight = FontWeights.Bold;
        }

        private void SoundBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ToggleSound();
            e.Handled = true;
        }

        private void ToggleSound()
        {
            SoundManager.Instance.IsMuted = !SoundManager.Instance.IsMuted;
            GameConfig.Instance.SoundEnabled = !SoundManager.Instance.IsMuted;
            UpdateSoundButtonState();
            SaveCurrentGame();
        }

        private void UpdateSoundButtonState()
        {
            if (SoundManager.Instance.IsMuted)
            {
                SoundTxt.Text = "🔇";
                SoundTxt.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));
                SoundBtn.Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));
                SoundBtn.ToolTip = "Unmute Sound (M)";
            }
            else
            {
                SoundTxt.Text = "🔊";
                SoundTxt.Foreground = Brushes.White;
                SoundBtn.Background = new SolidColorBrush(Color.FromArgb(204, 40, 160, 40));
                SoundBtn.ToolTip = "Mute Sound (M)";
            }
        }

        private void JumpscareBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ToggleJumpscares();
            e.Handled = true;
        }

        private void ToggleJumpscares()
        {
            if (!GameConfig.Instance.JumpscaresEnabled)
            {
                GameConfig.Instance.JumpscaresEnabled = true;
                GameConfig.Instance.JumpscareChance = 0.25;
            }
            else if (GameConfig.Instance.JumpscareChance < 0.40)
            {
                GameConfig.Instance.JumpscaresEnabled = true;
                GameConfig.Instance.JumpscareChance = 0.60;
            }
            else
            {
                GameConfig.Instance.JumpscaresEnabled = false;
                GameConfig.Instance.JumpscareChance = 0.25;
            }
            UpdateJumpscareButtonState();
            SaveCurrentGame();
        }

        private void UpdateJumpscareButtonState()
        {
            if (!GameConfig.Instance.JumpscaresEnabled)
            {
                JumpscareTxt.Text = "👻 Scares: OFF";
                JumpscareTxt.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
                JumpscareBtn.Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));
            }
            else if (GameConfig.Instance.JumpscareChance > 0.40)
            {
                JumpscareTxt.Text = "💀 Scares: CHAOS (60%)";
                JumpscareTxt.Foreground = new SolidColorBrush(Color.FromRgb(255, 220, 60));
                JumpscareBtn.Background = new SolidColorBrush(Color.FromArgb(230, 220, 20, 20));
            }
            else
            {
                JumpscareTxt.Text = "👻 Scares: ON (25%)";
                JumpscareTxt.Foreground = Brushes.White;
                JumpscareBtn.Background = new SolidColorBrush(Color.FromArgb(204, 180, 34, 34));
            }
        }

        public void TriggerTestJumpscare()
        {
            if (_jumpscareManager == null) return;
            if (_activeCreature == null)
            {
                SpawnNextCreature();
            }
            if (_activeCreature != null)
            {
                _jumpscareManager.TriggerSpiderGlassPounce(_activeCreature);
            }
        }

        private void TypeBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string tagStr &&
                Enum.TryParse<CreatureSelectionMode>(tagStr, out var mode))
            {
                SetCreatureSelectionMode(mode);
                e.Handled = true;
            }
        }

        private void SetCreatureSelectionMode(CreatureSelectionMode mode)
        {
            GameConfig.Instance.SelectionMode = mode;

            ResetTypeButton(TypeBtnAuto, TypeTxtAuto, "🎲 Auto");
            ResetTypeButton(TypeBtnMosquito, TypeTxtMosquito, "🦟 Mosquito");
            ResetTypeButton(TypeBtnSpider, TypeTxtSpider, "🕷️ Spider");
            ResetTypeButton(TypeBtnGiant, TypeTxtGiant, "💀 Giant");
            ResetTypeButton(TypeBtnBoss, TypeTxtBoss, "👑 Boss");

            switch (mode)
            {
                case CreatureSelectionMode.Auto:
                    HighlightTypeButton(TypeBtnAuto, TypeTxtAuto);
                    break;
                case CreatureSelectionMode.Mosquito:
                    HighlightTypeButton(TypeBtnMosquito, TypeTxtMosquito);
                    break;
                case CreatureSelectionMode.Spider:
                    HighlightTypeButton(TypeBtnSpider, TypeTxtSpider);
                    break;
                case CreatureSelectionMode.GiantSpider:
                    HighlightTypeButton(TypeBtnGiant, TypeTxtGiant);
                    break;
                case CreatureSelectionMode.Boss:
                    HighlightTypeButton(TypeBtnBoss, TypeTxtBoss);
                    break;
            }

            SpawnNextCreature();
        }

        private static void ResetTypeButton(Border btn, TextBlock txt, string label)
        {
            btn.Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));
            txt.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            txt.FontWeight = FontWeights.SemiBold;
            txt.Text = label;
        }

        private static void HighlightTypeButton(Border btn, TextBlock txt)
        {
            btn.Background = new SolidColorBrush(Color.FromArgb(204, 255, 34, 34));
            txt.Foreground = Brushes.White;
            txt.FontWeight = FontWeights.Bold;
        }

        private void CollapseBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetHudCollapsed(true);
            e.Handled = true;
        }

        private void HudCollapsedPill_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetHudCollapsed(false);
            e.Handled = true;
        }

        private void SetHudCollapsed(bool collapsed)
        {
            _isHudCollapsed = collapsed;
            _saveData.HudCollapsed = collapsed;
            if (collapsed)
            {
                HudExpandedContainer.Visibility = Visibility.Collapsed;
                HudCollapsedPill.Visibility = Visibility.Visible;
            }
            else
            {
                HudExpandedContainer.Visibility = Visibility.Visible;
                HudCollapsedPill.Visibility = Visibility.Collapsed;
            }
            SaveCurrentGame();
        }

        private void PauseBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TogglePause();
            e.Handled = true;
        }

        private void TogglePause()
        {
            _isPaused = !_isPaused;
            PauseTxt.Text = _isPaused ? "▶" : "⏸";
            PauseBtn.ToolTip = _isPaused ? "Resume Movement (P or Space)" : "Pause Movement (P or Space)";
            if (_isPaused)
            {
                SoundManager.Instance.StopMosquitoBuzz();
            }
        }

        private void SettingsBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            bool isVisible = HudSettingsDrawer.Visibility == Visibility.Visible;
            HudSettingsDrawer.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
            SettingsTxt.Foreground = !isVisible
                ? new SolidColorBrush(Color.FromRgb(255, 215, 0))
                : new SolidColorBrush(Color.FromRgb(204, 204, 204));
            e.Handled = true;
        }

        private void CreatureCycleBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CycleCreatureType();
            e.Handled = true;
        }

        private void BottomBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Cycle equipped weapon among unlocked weapons
            int currentLevel = ProgressionManager.GetLevelForKills(_gameState.KillCount).Level;
            var unlocked = ProgressionManager.Levels.Where(l => l.Level <= currentLevel).ToList();
            if (unlocked.Count <= 1) return;

            int currentIndex = unlocked.FindIndex(l => l.WeaponId == _currentLevelDef.WeaponId);
            int nextIndex = (currentIndex + 1) % unlocked.Count;
            _currentLevelDef = unlocked[nextIndex];
            _saveData.EquippedWeaponId = _currentLevelDef.WeaponId;

            UpdateProgressionDisplay();
            SaveCurrentGame();
            SoundManager.Instance.PlaySmack();
            e.Handled = true;
        }

        private void SaveCurrentGame()
        {
            _saveData.TotalKills = _gameState.KillCount;
            _saveData.TotalXP = _totalXp;
            _saveData.EquippedWeaponId = _currentLevelDef.WeaponId;
            _saveData.SoundMuted = SoundManager.Instance.IsMuted;
            _saveData.JumpscaresEnabled = GameConfig.Instance.JumpscaresEnabled;
            _saveData.JumpscareChance = GameConfig.Instance.JumpscareChance;
            _saveData.HudCollapsed = _isHudCollapsed;
            _saveData.BaseSpeedMultiplier = _baseSpeedMultiplier;
            _saveData.SelectedCreatureType = GameConfig.Instance.SelectionMode.ToString();
            SaveManager.Save(_saveData);
        }

        private void ResetBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleResetRequest();
            e.Handled = true;
        }

        private void ResetProgressSettingsBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleResetRequest();
            e.Handled = true;
        }

        private void RespawnCreatureBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RespawnCurrentCreature();
            e.Handled = true;
        }

        private void HandleResetRequest()
        {
            if (!_isResetConfirming)
            {
                _isResetConfirming = true;
                ResetTxt.Text = "Sure?";
                ResetTxt.Foreground = Brushes.White;
                ResetBtn.Background = new SolidColorBrush(Color.FromArgb(204, 220, 30, 30));
                ResetBtn.ToolTip = "Click again to CONFIRM reset to Level 1!";
                ResetProgressSettingsTxt.Text = "⚠️ Click Again to Confirm!";
                _resetConfirmTimer.Stop();
                _resetConfirmTimer.Start();
            }
            else
            {
                _resetConfirmTimer.Stop();
                _isResetConfirming = false;
                ResetTxt.Text = "🔄";
                ResetTxt.Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                ResetBtn.Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));
                ResetBtn.ToolTip = "Reset Kills & Progress (R)";
                ResetProgressSettingsTxt.Text = "⚠️ Reset Progress";

                ExecuteFullReset();
            }
        }

        private void RespawnCurrentCreature()
        {
            _bloodSystem?.Clear();
            CleaningPrompt.Opacity = 0;
            CleanerCursorFx.Opacity = 0;
            _gameState.Respawn();
            SpawnNextCreature();
            SoundManager.Instance.PlaySmack();
        }

        private void ExecuteFullReset()
        {
            // 1. Reset GameState kills & phase
            _gameState.RestoreKills(0);
            _totalXp = 0;

            // 2. Reset definitions to Level 1 and Tier 1 cleaner
            _currentLevelDef = ProgressionManager.Levels[0]; // Hand
            _currentCleaner = CleanerProgression.Cleaners[0]; // Tissue Paper

            // 3. Reset persistent save data
            _saveData.TotalKills = 0;
            _saveData.TotalXP = 0;
            _saveData.EquippedWeaponId = "hand";
            SaveManager.Save(_saveData);

            // 4. Clear blood & prompts
            _bloodSystem?.Clear();
            CleaningPrompt.Opacity = 0;
            CleanerCursorFx.Opacity = 0;
            _respawnTimer.Stop();

            // 5. Respawn creature
            _gameState.Respawn();
            SpawnNextCreature();

            // 6. Refresh progression UI
            UpdateProgressionDisplay();

            // 7. Visual & sound feedback
            SoundManager.Instance.PlayCleanSqueak();

            SparkleFx.Text = "✨ GAME RESET TO LEVEL 1 ✨";
            double screenWidth = ActualWidth > 100 ? ActualWidth : SystemParameters.PrimaryScreenWidth;
            double screenHeight = ActualHeight > 100 ? ActualHeight : SystemParameters.PrimaryScreenHeight;
            Canvas.SetLeft(SparkleFx, Math.Max(20, (screenWidth - 260) / 2.0));
            Canvas.SetTop(SparkleFx, Math.Max(20, (screenHeight - 60) / 2.0));
            var sparkleAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
            {
                AutoReverse = true
            };
            SparkleFx.BeginAnimation(OpacityProperty, sparkleAnim);
        }

        private void ExitBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SaveCurrentGame();
            Close();
            e.Handled = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    SaveCurrentGame();
                    Close();
                    break;
                case Key.D1:
                    SetSpeed(0.5);
                    break;
                case Key.D2:
                    SetSpeed(1.0);
                    break;
                case Key.D3:
                    SetSpeed(1.8);
                    break;
                case Key.D4:
                    SetSpeed(2.8);
                    break;
                case Key.M:
                    ToggleSound();
                    break;
                case Key.J:
                    ToggleJumpscares();
                    break;
                case Key.K:
                    TriggerTestJumpscare();
                    break;
                case Key.C:
                    CycleCreatureType();
                    break;
                case Key.P:
                case Key.Space:
                    TogglePause();
                    break;
                case Key.H:
                    SetHudCollapsed(!_isHudCollapsed);
                    break;
                case Key.R:
                    HandleResetRequest();
                    break;
                case Key.L:
                    // Cheat/Test shortcut: add 5 kills to test progression thresholds
                    _gameState.Kill(new Point(ActualWidth / 2, ActualHeight / 2), CreatureType.Mosquito);
                    break;
            }
        }

        private void CycleCreatureType()
        {
            var current = GameConfig.Instance.SelectionMode;
            var next = current switch
            {
                CreatureSelectionMode.Auto => CreatureSelectionMode.Mosquito,
                CreatureSelectionMode.Mosquito => CreatureSelectionMode.Spider,
                CreatureSelectionMode.Spider => CreatureSelectionMode.GiantSpider,
                CreatureSelectionMode.GiantSpider => CreatureSelectionMode.Boss,
                CreatureSelectionMode.Boss => CreatureSelectionMode.Auto,
                _ => CreatureSelectionMode.Auto
            };
            SetCreatureSelectionMode(next);
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"), $"[{DateTime.UtcNow:O}] MainWindow.Window_Closing called\n");
            }
            catch { }
            _isClosing = true;
            SaveCurrentGame();
            SoundManager.Instance.StopMosquitoBuzz();
            CompositionTarget.Rendering -= OnRenderFrame;
            _respawnTimer.Stop();
            _resetConfirmTimer.Stop();
            _inputManager?.Dispose();
        }
    }
}