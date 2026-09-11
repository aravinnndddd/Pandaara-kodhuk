using System;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DigitalMosquito
{
    public class SoundManager
    {
        private static SoundManager? _instance;
        public static SoundManager Instance => _instance ??= new SoundManager();

        public bool IsMuted { get; set; } = true;

        private SoundPlayer? _playerSmack;
        private SoundPlayer? _playerSplat;
        private SoundPlayer? _playerWipe;
        private SoundPlayer? _playerBuzz;
        private SoundPlayer? _playerJumpscare;
        private SoundPlayer? _playerGlassThud;
        private SoundPlayer? _playerMasterJumpscare;
        private SoundPlayer? _playerSwatter;
        private SoundPlayer? _playerNewspaper;
        private SoundPlayer? _playerSlipper;
        private SoundPlayer? _playerBat;
        private SoundPlayer? _playerHammer;
        private SoundPlayer? _playerVacuum;
        private SoundPlayer? _playerWasher;
        private SoundPlayer? _playerElectric;
        private SoundPlayer? _playerGiantSlipper;
        private SoundPlayer? _playerLevelUp;
        private SoundPlayer? _playerCleanSqueak;

        private MediaPlayer? _mosquitoMediaPlayer;
        private bool _isMosquitoSoundPlaying = false;

        private DateTime _lastWipeSoundTime = DateTime.MinValue;
        private DateTime _lastBuzzSoundTime = DateTime.MinValue;

        public SoundManager()
        {
            InitializeSounds();
        }

        private void InitializeSounds()
        {
            try
            {
                // Initialize user-provided mosquito-sound.mp3 via MediaPlayer
                InitializeMosquitoMp3();

                // Generate procedural fallback sounds in-memory
                _playerSmack = CreatePlayer("Sounds/smack.wav", GenerateSmackSound);
                _playerSplat = CreatePlayer("Sounds/splat.wav", GenerateSplatSound);
                _playerWipe = CreatePlayer("Sounds/wipe.wav", GenerateWipeSound);
                _playerBuzz = CreatePlayer("Sounds/buzz.wav", GenerateBuzzSound);
                _playerMasterJumpscare = CreatePlayer("Sounds/jumpscare_master.wav", GenerateMasterJumpscareSound);
                _playerJumpscare = _playerMasterJumpscare;
                _playerGlassThud = CreatePlayer("Sounds/glassthud.wav", GenerateGlassThudSound);

                _playerSwatter = CreatePlayer("Sounds/swatter.wav", GenerateSwatterSound);
                _playerNewspaper = CreatePlayer("Sounds/newspaper.wav", GenerateNewspaperSound);
                _playerSlipper = CreatePlayer("Sounds/slipper.wav", GenerateSlipperSound);
                _playerBat = CreatePlayer("Sounds/bat.wav", GenerateBatSound);
                _playerHammer = CreatePlayer("Sounds/hammer.wav", GenerateHammerSound);
                _playerVacuum = CreatePlayer("Sounds/vacuum.wav", GenerateVacuumSound);
                _playerWasher = CreatePlayer("Sounds/washer.wav", GenerateWasherSound);
                _playerElectric = CreatePlayer("Sounds/electric.wav", GenerateElectricSound);
                _playerGiantSlipper = CreatePlayer("Sounds/giant_slipper.wav", GenerateGiantSlipperSound);
                _playerLevelUp = CreatePlayer("Sounds/levelup.wav", GenerateLevelUpSound);
                _playerCleanSqueak = CreatePlayer("Sounds/squeak.wav", GenerateCleanSqueakSound);
            }
            catch
            {
                // Audio initialization failed gracefully
            }
        }

        private void InitializeMosquitoMp3()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string persistentCacheDir = Path.Combine(localAppData, "PandaaraKodhuk");
                string persistentCachePath = Path.Combine(persistentCacheDir, "mosquito-sound.mp3");

                string tempCacheDir = Path.Combine(Path.GetTempPath(), "PandaaraKodhuk");
                string tempCachePath = Path.Combine(tempCacheDir, "mosquito-sound.mp3");

                string[] possiblePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", "mosquito-sound.mp3"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mosquito-sound.mp3"),
                    persistentCachePath,
                    tempCachePath,
                    Path.Combine(Directory.GetCurrentDirectory(), "Sounds", "mosquito-sound.mp3"),
                    Path.Combine(Directory.GetCurrentDirectory(), "mosquito-sound.mp3"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "mosquito-sound.mp3"),
                    "mosquito-sound.mp3"
                };

                string? resolvedPath = null;
                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path) && new FileInfo(path).Length > 0)
                    {
                        resolvedPath = Path.GetFullPath(path);
                        break;
                    }
                }

                // If not found as a loose file on disk, extract embedded resource from assembly
                if (resolvedPath == null)
                {
                    var asm = typeof(SoundManager).Assembly;
                    string? resourceName = asm.GetManifestResourceNames()
                        .FirstOrDefault(n => n.EndsWith("mosquito-sound.mp3", StringComparison.OrdinalIgnoreCase));

                    if (resourceName != null)
                    {
                        string extractTarget = persistentCachePath;
                        try
                        {
                            Directory.CreateDirectory(persistentCacheDir);
                        }
                        catch
                        {
                            extractTarget = tempCachePath;
                            Directory.CreateDirectory(tempCacheDir);
                        }

                        using (var resStream = asm.GetManifestResourceStream(resourceName))
                        {
                            if (resStream != null)
                            {
                                using var fs = new FileStream(extractTarget, FileMode.Create, FileAccess.Write, FileShare.Read);
                                resStream.CopyTo(fs);
                            }
                        }

                        if (File.Exists(extractTarget) && new FileInfo(extractTarget).Length > 0)
                        {
                            resolvedPath = Path.GetFullPath(extractTarget);
                        }
                    }
                }

                if (resolvedPath != null && File.Exists(resolvedPath))
                {
                    _mosquitoMediaPlayer = new MediaPlayer();
                    _mosquitoMediaPlayer.Open(new Uri(resolvedPath, UriKind.Absolute));
                    _mosquitoMediaPlayer.Volume = 0.90;
                    _mosquitoMediaPlayer.MediaEnded += (s, e) =>
                    {
                        if (_isMosquitoSoundPlaying && !IsMuted)
                        {
                            _mosquitoMediaPlayer.Stop();
                            _mosquitoMediaPlayer.Position = TimeSpan.Zero;
                            _mosquitoMediaPlayer.Play();
                        }
                    };
                    _mosquitoMediaPlayer.MediaFailed += (s, e) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"Mosquito MediaPlayer failed: {e.ErrorException?.Message}");
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mosquito MP3 init exception: {ex.Message}");
            }
        }

        private SoundPlayer? CreatePlayer(string relativePath, Func<MemoryStream> proceduralGenerator)
        {
            try
            {
                if (File.Exists(relativePath))
                {
                    var player = new SoundPlayer(relativePath);
                    player.LoadAsync();
                    return player;
                }
                else
                {
                    var stream = proceduralGenerator();
                    stream.Position = 0;
                    var player = new SoundPlayer(stream);
                    player.LoadAsync();
                    return player;
                }
            }
            catch
            {
                return null;
            }
        }

        public void StartMosquitoBuzz()
        {
            if (IsMuted)
            {
                StopMosquitoBuzz();
                return;
            }

            if (_mosquitoMediaPlayer == null)
            {
                InitializeMosquitoMp3();
            }

            if (_mosquitoMediaPlayer != null)
            {
                if (!_isMosquitoSoundPlaying)
                {
                    _isMosquitoSoundPlaying = true;
                    try
                    {
                        if (_mosquitoMediaPlayer.NaturalDuration.HasTimeSpan &&
                            _mosquitoMediaPlayer.Position >= _mosquitoMediaPlayer.NaturalDuration.TimeSpan)
                        {
                            _mosquitoMediaPlayer.Position = TimeSpan.Zero;
                        }
                        _mosquitoMediaPlayer.Play();
                    }
                    catch
                    {
                        _mosquitoMediaPlayer.Play();
                    }
                }
            }
        }

        public void StopMosquitoBuzz()
        {
            _isMosquitoSoundPlaying = false;
            if (_mosquitoMediaPlayer != null)
            {
                try
                {
                    _mosquitoMediaPlayer.Pause();
                }
                catch { }
            }
        }

        public void PlayMosquitoBuzz()
        {
            if (IsMuted || _playerBuzz == null) return;

            // Throttle buzzing repetition
            if ((DateTime.UtcNow - _lastBuzzSoundTime).TotalMilliseconds < 800) return;
            _lastBuzzSoundTime = DateTime.UtcNow;

            PlayAsync(_playerBuzz);
        }

        public void PlaySmack()
        {
            if (IsMuted || _playerSmack == null) return;
            PlayAsync(_playerSmack);
        }

        public void PlaySplat()
        {
            if (IsMuted || _playerSplat == null) return;
            PlayAsync(_playerSplat);
        }

        public void PlayWipe()
        {
            if (IsMuted || _playerWipe == null) return;

            // Throttle wiping scrub sound to avoid rapid audio stacking
            if ((DateTime.UtcNow - _lastWipeSoundTime).TotalMilliseconds < 160) return;
            _lastWipeSoundTime = DateTime.UtcNow;

            PlayAsync(_playerWipe);
        }

        public void PlayJumpscare(bool forceAudio = true)
        {
            if (IsMuted && !forceAudio) return;

            // Silence mosquito hum immediately so jumpscare hits from sudden silence
            StopMosquitoBuzz();

            if (_playerMasterJumpscare != null)
            {
                PlayAsync(_playerMasterJumpscare);
            }
            else if (_playerJumpscare != null)
            {
                PlayAsync(_playerJumpscare);
            }
        }

        public void PlayGlassThud()
        {
            if (IsMuted || _playerGlassThud == null) return;
            PlayAsync(_playerGlassThud);
        }

        public void PlayWeaponAttack(string weaponId)
        {
            if (IsMuted) return;
            switch (weaponId.ToLowerInvariant())
            {
                case "hand":
                    PlaySmack();
                    break;
                case "swatter":
                    PlayAsync(_playerSwatter ?? _playerSmack);
                    break;
                case "newspaper":
                    PlayAsync(_playerNewspaper ?? _playerSmack);
                    break;
                case "slipper":
                    PlayAsync(_playerSlipper ?? _playerSmack);
                    break;
                case "bat":
                    PlayAsync(_playerBat ?? _playerSmack);
                    break;
                case "hammer":
                    PlayAsync(_playerHammer ?? _playerGlassThud);
                    break;
                case "vacuum":
                    PlayAsync(_playerVacuum ?? _playerSmack);
                    break;
                case "washer":
                    PlayAsync(_playerWasher ?? _playerSmack);
                    break;
                case "electric":
                    PlayAsync(_playerElectric ?? _playerSmack);
                    break;
                case "giant_slipper":
                    PlayAsync(_playerGiantSlipper ?? _playerGlassThud);
                    break;
                case "boss_mode":
                default:
                    PlayAsync(_playerLevelUp ?? _playerSmack);
                    break;
            }
        }

        public void PlayLevelUp(bool forceAudio = false)
        {
            if (IsMuted && !forceAudio) return;
            if (_playerLevelUp != null) PlayAsync(_playerLevelUp);
        }

        public void PlayCleanSqueak()
        {
            if (IsMuted || _playerCleanSqueak == null) return;
            PlayAsync(_playerCleanSqueak);
        }

        private static void PlayAsync(SoundPlayer? player)
        {
            if (player == null) return;
            Task.Run(() =>
            {
                try
                {
                    player.Play();
                }
                catch
                {
                    // Ignore transient playback errors
                }
            });
        }

        #region Procedural Audio Generators

        private static MemoryStream GenerateBuzzSound()
        {
            // High frequency insect buzzing hum with harmonic overtone & LFO flutter (0.35s)
            int sampleRate = 22050;
            double duration = 0.35;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double lfo = 0.7 + 0.3 * Math.Sin(2 * Math.PI * 18 * t);
                double envelope = Math.Sin(Math.PI * (t / duration)); // Smooth bell envelope

                // Fundamental buzzing frequencies (approx 260Hz + 520Hz + 780Hz)
                double wave = 0.45 * Math.Sin(2 * Math.PI * 260 * t)
                            + 0.35 * Math.Sin(2 * Math.PI * 520 * t)
                            + 0.20 * Math.Sin(2 * Math.PI * 780 * t);

                // Add slight buzz grit
                wave = Math.Clamp(wave * 1.6, -1.0, 1.0);

                samples[i] = (short)(wave * envelope * lfo * 14000);
            }

            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateSmackSound()
        {
            // Snappy swat impact: fast noise burst + low thud (0.12s)
            int sampleRate = 22050;
            double duration = 0.12;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(42);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double decay = Math.Exp(-t * 35.0);
                double noise = (rand.NextDouble() * 2.0 - 1.0) * 0.7;
                double thud = Math.Sin(2 * Math.PI * (160 - t * 600) * t) * 0.5;

                double mixed = (noise * 0.6 + thud * 0.4) * decay;
                samples[i] = (short)(Math.Clamp(mixed, -1.0, 1.0) * 22000);
            }

            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateSplatSound()
        {
            // Heavy juicy arcade splat: punchy low impact + squelchy pitch drop (0.22s)
            int sampleRate = 22050;
            double duration = 0.22;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(1337);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double decay = Math.Exp(-t * 18.0);

                // Pitch bends down from 140Hz to 35Hz
                double freq = Math.Max(35, 140 - t * 450);
                double body = Math.Sin(2 * Math.PI * freq * t);

                // Squelch texture
                double squelch = (rand.NextDouble() * 2.0 - 1.0) * Math.Sin(2 * Math.PI * 40 * t);

                double mixed = (body * 0.7 + squelch * 0.5) * decay;
                samples[i] = (short)(Math.Clamp(mixed, -1.0, 1.0) * 26000);
            }

            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateWipeSound()
        {
            // Soft squeak / friction whoosh (0.10s)
            int sampleRate = 22050;
            double duration = 0.10;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(77);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double env = Math.Sin(Math.PI * (t / duration));
                double noise = (rand.NextDouble() * 2.0 - 1.0);
                // Pitch swell
                double tone = Math.Sin(2 * Math.PI * (600 + Math.Sin(t * 30) * 200) * t) * 0.3;

                double mixed = (noise * 0.4 + tone * 0.6) * env;
                samples[i] = (short)(Math.Clamp(mixed, -1.0, 1.0) * 12000);
            }

            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateJumpscareSound()
        {
            // Terrifying visceral horror jumpscare: instant harsh screech + monster roar + roaring noise (0.65s)
            int sampleRate = 22050;
            double duration = 0.65;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(666);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                // Blindingly fast attack (1ms), then heavy lingering terror decay
                double attack = Math.Min(1.0, t * 1000.0);
                double decay = Math.Exp(-t * 4.5);
                double envelope = attack * decay;

                // Dissonant piercing shrieking frequencies (minor 2nd, tritone, high shrill harmonics)
                double f1 = Math.Sin(2 * Math.PI * 780 * t);
                double f2 = Math.Sin(2 * Math.PI * 830 * t); // Dissonant beat
                double f3 = Math.Sin(2 * Math.PI * 1350 * t);
                double f4 = Math.Sin(2 * Math.PI * 2200 * t);
                double f5 = Math.Sin(2 * Math.PI * (3400 + Math.Sin(t * 70) * 800) * t); // Shrill shrieking slide

                // Monster growl sub-harmonic
                double roar = Math.Sin(2 * Math.PI * 95 * t) * 0.5;

                // Violent aggressive noise burst
                double noise = (rand.NextDouble() * 2.0 - 1.0) * 0.65;

                double raw = (f1 * 0.35 + f2 * 0.35 + f3 * 0.25 + f4 * 0.2 + f5 * 0.25 + roar + noise);

                // Harsh distortion clipping for maximum scare shock
                double distorted = Math.Clamp(raw * 2.2, -1.0, 1.0);

                samples[i] = (short)(distorted * envelope * 32700);
            }

            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateGlassThudSound()
        {
            // Shockwave sub-bass thud + glass shatter crack (0.45s)
            int sampleRate = 22050;
            double duration = 0.45;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(88);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double decay = Math.Exp(-t * 9.0);

                // Violent glass shatter transient
                double glassShatter = 0;
                if (t < 0.08)
                {
                    glassShatter = (rand.NextDouble() * 2.0 - 1.0) * Math.Sin(2 * Math.PI * 4500 * t);
                }

                // Thunderous chest-thumping bass drop (90Hz down to 25Hz)
                double freq = Math.Max(25, 90 - t * 180);
                double subBass = Math.Sin(2 * Math.PI * freq * t);

                double mixed = (glassShatter * 0.6 + subBass * 0.8) * decay;
                samples[i] = (short)(Math.Clamp(mixed * 1.8, -1.0, 1.0) * 32000);
            }

            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateMasterJumpscareSound()
        {
            // AAA Cinema-Grade Horror Jumpscare (0.85s):
            // 0ms instant explosive sub-bass shockwave drop + high-frequency glass/chitin shatter +
            // blood-curdling FM banshee screech + guttural monster roar + rapid clicking chitin spasm.
            int sampleRate = 44100;
            double duration = 0.85;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(1337);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;

                // 1. Initial Explosive Transient & Glass Shatter (0 - 95ms)
                double shatter = 0.0;
                if (t < 0.095)
                {
                    double shatterEnv = Math.Exp(-t * 35.0);
                    double highNoise = (rand.NextDouble() * 2.0 - 1.0);
                    double glassRing = Math.Sin(2 * Math.PI * 4800 * t) * 0.5 + Math.Sin(2 * Math.PI * 6200 * t) * 0.4;
                    shatter = (highNoise * 0.65 + glassRing * 0.45) * shatterEnv;
                }

                // 2. Earth-shattering Sub-Bass Drop (0 - 450ms)
                // Glides down from 125Hz to 28Hz for maximum subwoofer/headphone vibration
                double bassEnv = Math.Exp(-t * 4.8);
                double bassFreq = Math.Max(26.0, 125.0 - t * 240.0);
                double subBass = Math.Sin(2 * Math.PI * bassFreq * t) * bassEnv;

                // 3. Piercing Banshee Horror Screech (0 - 650ms)
                // Dissonant minor-second and tritone clusters with 48Hz chaotic vibrato
                double screechEnv = Math.Exp(-t * 3.4);
                double vibrato = Math.Sin(2 * Math.PI * 48.0 * t) * 140.0;
                double sc1 = Math.Sin(2 * Math.PI * (820.0 + vibrato) * t);
                double sc2 = Math.Sin(2 * Math.PI * (1250.0 + vibrato * 1.5) * t);
                double sc3 = Math.Sin(2 * Math.PI * (2140.0 + vibrato * 2.0) * t);
                double sc4 = Math.Sin(2 * Math.PI * (3450.0 + vibrato * 3.0) * t);
                double sc5 = Math.Sin(2 * Math.PI * (4900.0 + Math.Sin(t * 80) * 400.0) * t);
                double screech = (sc1 * 0.3 + sc2 * 0.3 + sc3 * 0.25 + sc4 * 0.25 + sc5 * 0.2) * screechEnv;

                // 4. Guttural Demonic Monster Roar (0 - 750ms)
                double roarEnv = Math.Exp(-t * 3.8);
                double roarPitch = Math.Max(50.0, 92.0 - t * 50.0);
                double roarSaw = ((t * roarPitch) % 1.0) * 2.0 - 1.0;
                double roarNoise = (rand.NextDouble() * 2.0 - 1.0) * 0.6;
                double roar = (roarSaw * 0.7 + roarNoise * 0.5) * roarEnv;

                // 5. Creepy Chitinous Leg Skittering & Spasm Clicks (200ms - 800ms)
                double skitter = 0.0;
                if (t > 0.18 && t < 0.78)
                {
                    double clickPhase = (t - 0.18) % 0.026; // Every 26ms
                    if (clickPhase < 0.003)
                    {
                        skitter = (rand.NextDouble() * 2.0 - 1.0) * Math.Sin(2 * Math.PI * 3200 * t) * 0.65;
                    }
                }

                // Layer and combine
                double total = shatter * 1.2 + subBass * 1.1 + screech * 0.95 + roar * 0.75 + skitter * 0.5;

                // Soft-limiting overdrive saturation for maximum perceived loudness without digital clipping
                double mastered = Math.Tanh(total * 2.4);
                samples[i] = (short)(mastered * 32600.0);
            }

            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateSwatterSound()
        {
            int sampleRate = 22050;
            double duration = 0.08;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(11);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double env = Math.Exp(-t * 50.0);
                double noise = (rand.NextDouble() * 2.0 - 1.0);
                double snap = Math.Sin(2 * Math.PI * 1400 * t);
                double mixed = (noise * 0.7 + snap * 0.5) * env;
                samples[i] = (short)(Math.Clamp(mixed * 2.0, -1.0, 1.0) * 28000);
            }
            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateNewspaperSound()
        {
            int sampleRate = 22050;
            double duration = 0.11;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(22);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double env = Math.Exp(-t * 35.0);
                double noise = (rand.NextDouble() * 2.0 - 1.0);
                double pop = Math.Sin(2 * Math.PI * 340 * t);
                double mixed = (noise * 0.6 + pop * 0.7) * env;
                samples[i] = (short)(Math.Clamp(mixed * 1.9, -1.0, 1.0) * 29000);
            }
            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateSlipperSound()
        {
            int sampleRate = 22050;
            double duration = 0.14;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(33);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double env = Math.Exp(-t * 28.0);
                double slapNoise = (rand.NextDouble() * 2.0 - 1.0);
                double thud = Math.Sin(2 * Math.PI * 180 * t);
                double mixed = (slapNoise * 0.65 + thud * 0.8) * env;
                samples[i] = (short)(Math.Clamp(mixed * 2.2, -1.0, 1.0) * 31000);
            }
            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateBatSound()
        {
            // Whoosh swing + loud solid crack
            int sampleRate = 22050;
            double duration = 0.20;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(44);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double whoosh = (t < 0.05) ? (rand.NextDouble() * 2.0 - 1.0) * Math.Sin(Math.PI * (t / 0.05)) * 0.4 : 0;
                double crackTime = t - 0.05;
                double crack = 0;
                if (crackTime >= 0)
                {
                    double env = Math.Exp(-crackTime * 30.0);
                    double woodSnap = Math.Sin(2 * Math.PI * 980 * crackTime) * 0.7;
                    double lowThump = Math.Sin(2 * Math.PI * 120 * crackTime) * 0.8;
                    double noise = (rand.NextDouble() * 2.0 - 1.0) * 0.5;
                    crack = (woodSnap + lowThump + noise) * env;
                }
                double mixed = whoosh + crack;
                samples[i] = (short)(Math.Clamp(mixed * 2.0, -1.0, 1.0) * 32000);
            }
            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateHammerSound()
        {
            // Heavy metallic smash + deep iron thud
            int sampleRate = 22050;
            double duration = 0.22;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(55);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double env = Math.Exp(-t * 22.0);
                double anvilRing = Math.Sin(2 * Math.PI * 2600 * t) * 0.4 + Math.Sin(2 * Math.PI * 3400 * t) * 0.3;
                double subThud = Math.Sin(2 * Math.PI * 85 * t) * 0.9;
                double noise = (rand.NextDouble() * 2.0 - 1.0) * 0.6;
                double mixed = (anvilRing + subThud + noise) * env;
                samples[i] = (short)(Math.Clamp(mixed * 2.2, -1.0, 1.0) * 32000);
            }
            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateVacuumSound()
        {
            // Vortex suction whoosh + cork pop
            int sampleRate = 22050;
            double duration = 0.32;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(66);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double freq = 200 + t * 900;
                double suction = (rand.NextDouble() * 2.0 - 1.0) * Math.Sin(2 * Math.PI * freq * t) * (t / duration);
                double pop = 0;
                if (t > 0.26)
                {
                    double pt = t - 0.26;
                    pop = Math.Sin(2 * Math.PI * 160 * pt) * Math.Exp(-pt * 60.0);
                }
                double mixed = suction * 0.8 + pop * 1.2;
                samples[i] = (short)(Math.Clamp(mixed * 1.8, -1.0, 1.0) * 30000);
            }
            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateWasherSound()
        {
            // High-pressure water jet spray hiss + liquid splash squeak
            int sampleRate = 22050;
            double duration = 0.22;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(66);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double env = Math.Exp(-t * 12.0);
                // Pressurized spray hiss (high-frequency filtered noise)
                double hiss = (rand.NextDouble() * 2.0 - 1.0) * (0.6 + 0.4 * Math.Sin(2 * Math.PI * 1800 * t));
                // Water surge splash
                double splash = Math.Sin(2 * Math.PI * (400 - t * 600) * t) * 0.5;
                // Squeegee rubber squeak chirp
                double squeak = (t > 0.04 && t < 0.10) ? Math.Sin(2 * Math.PI * 2600 * t) * 0.35 : 0;

                double mixed = (hiss * 0.75 + splash * 0.6 + squeak) * env;
                samples[i] = (short)(Math.Clamp(mixed * 2.2, -1.0, 1.0) * 31000);
            }
            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateElectricSound()
        {
            // High-voltage electric arc zap
            int sampleRate = 22050;
            double duration = 0.18;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(77);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double env = Math.Exp(-t * 20.0);
                double buzz = Math.Sign(Math.Sin(2 * Math.PI * 120 * t)) * 0.5;
                double sparks = (rand.NextDouble() > 0.85) ? (rand.NextDouble() * 2.0 - 1.0) * 1.2 : 0;
                double mixed = (buzz + sparks) * env;
                samples[i] = (short)(Math.Clamp(mixed * 2.2, -1.0, 1.0) * 32000);
            }
            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateGiantSlipperSound()
        {
            // Thunderous comic slam
            int sampleRate = 22050;
            double duration = 0.30;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            var rand = new Random(88);

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double env = Math.Exp(-t * 14.0);
                double subBoom = Math.Sin(2 * Math.PI * 55 * t) * 1.1;
                double slapNoise = (rand.NextDouble() * 2.0 - 1.0) * 0.8;
                double mixed = (subBoom + slapNoise) * env;
                samples[i] = (short)(Math.Clamp(mixed * 2.5, -1.0, 1.0) * 32600);
            }
            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateLevelUpSound()
        {
            // Celebratory victory arpeggio: C5 (523Hz), E5 (659Hz), G5 (784Hz), C6 (1046Hz)
            int sampleRate = 22050;
            double duration = 0.55;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];

            double[] notes = { 523.25, 659.25, 783.99, 1046.50 };
            double noteDuration = 0.12;

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                int noteIndex = Math.Min(notes.Length - 1, (int)(t / noteDuration));
                double noteTime = t - (noteIndex * noteDuration);
                double freq = notes[noteIndex];

                double noteEnv = Math.Exp(-noteTime * 7.0);
                double chime = Math.Sin(2 * Math.PI * freq * t) * 0.7
                             + Math.Sin(2 * Math.PI * (freq * 2) * t) * 0.3;

                double mixed = chime * noteEnv;
                samples[i] = (short)(Math.Clamp(mixed * 1.8, -1.0, 1.0) * 28000);
            }
            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream GenerateCleanSqueakSound()
        {
            // Crisp rubber squeegee on glass squeak / shine chime
            int sampleRate = 22050;
            double duration = 0.20;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double env = Math.Exp(-t * 18.0);
                double freq1 = 2200 + Math.Sin(t * 70.0) * 800;
                double freq2 = 2850 + Math.Cos(t * 60.0) * 600;
                double squeak = (Math.Sin(2 * Math.PI * freq1 * t) + Math.Sin(2 * Math.PI * freq2 * t) * 0.5) * 0.45;
                double sparkle = Math.Sin(2 * Math.PI * 3520 * t) * Math.Exp(-t * 12.0) * 0.35;
                double mixed = (squeak + sparkle) * env;
                samples[i] = (short)(Math.Clamp(mixed * 2.2, -1.0, 1.0) * 31000);
            }
            return BuildWavStream(samples, sampleRate);
        }

        private static MemoryStream BuildWavStream(short[] samples, int sampleRate)
        {
            var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            int byteRate = sampleRate * 2;
            int dataLength = samples.Length * 2;

            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataLength);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt subchunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Subchunk1Size
            writer.Write((short)1); // PCM format
            writer.Write((short)1); // Mono
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)2); // BlockAlign
            writer.Write((short)16); // BitsPerSample

            // data subchunk
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataLength);

            for (int i = 0; i < samples.Length; i++)
            {
                writer.Write(samples[i]);
            }

            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        #endregion
    }
}
