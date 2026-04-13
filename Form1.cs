using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GoonWarfareX.Models;

namespace GoonWarfareX;

public partial class Form1 : Form
{
    private enum GameState { MainMenu, NameEntry, ModeSelect, CharacterSelect, StageSelect, TimeSelect, RoundSelect, Playing, VictoryScreen }
    private GameState _currentState = GameState.MainMenu;

    // RULE SETTINGS
    private bool _isTimeInfinite = false;
    private int _maxRounds = 3;
    private int _p1RoundsWon = 0;
    private int _p2RoundsWon = 0;
    private int _matchTimer = 99;
    private int _tickAccumulator = 0;
    private bool _isKO = false;
    private int _koTimer = 0;

    // NATIVE GAMEPAD SUPPORT (DirectInput / WinMM)
    [StructLayout(LayoutKind.Sequential)]
    public struct JOYINFOEX
    {
        public int dwSize;
        public int dwFlags;
        public int dwXpos;
        public int dwYpos;
        public int dwZpos;
        public int dwRpos;
        public int dwUpos;
        public int dwVpos;
        public int dwButtons;
        public int dwButtonNumber;
        public int dwPOV;
        public int dwReserved1;
        public int dwReserved2;
    }

    [DllImport("winmm.dll")]
    public static extern int joyGetPosEx(int uJoyID, ref JOYINFOEX pji);

    // Name Entry & Mode Select Config
    private string _playerName = "";
    private bool _showCursor = true;
    private bool _showComingSoonMsg = false;
    private string _comingSoonTitle = "";

    private Image? _menuBackground;
    private List<Image> _stages = new List<Image>();
    private int _currentStageIndex = 0;
    private int _selectedStageIndex = 0; // The stage chosen by the user
    private int _stageTimerTicks = 0;
    private bool _isTrainingMode = false;


    // CHARACTER SELECT IMAGES
    private Image? _imgIdle_Grimace;
    private Image? _imgIdle_Willie;
    private Image? _imgIdle_Ramon;
    private Image? _imgIdle_Arzadown;
    private Image? _imgIdle_Du30;
    private Image? _imgIdle_Jabee;
    private Image? _imgIdle_Janitor;
    private Image? _imgIdle_Larry;
    private Image? _imgIdle_Matrix;
    private Image? _imgIdle_Stephen;
    private int _selectedCharacter = 0;   // P1: 0=Grimace, 1=Willie, 2=Ramon
    private int _p2SelectedCharacter = 0; // P2: 0=Grimace, 1=Willie, 2=Ramon
    private bool _selectingP2 = false;    // false = picking P1, true = picking P2

    private Image? _imgIdle, _imgWalk, _imgStance, _imgPunch, _imgKick, _imgFlyingPunch, _imgHadouken, _imgSpecial, _imgJump;
    private Image? _jabeeProjectile; // Jabee's custom chickenjoy fireball
    // P2 INDIVIDUAL FRAME IMAGES
    private Image? _p2ImgIdle, _p2ImgWalk, _p2ImgStance, _p2ImgPunch, _p2ImgKick, _p2ImgFlyingPunch, _p2ImgHadouken, _p2ImgSpecial, _p2ImgJump;

    private System.Windows.Forms.Timer _animationTimer;
    private bool _showMenuText = true;
    private int _blinkCounter = 0;

    // DINO EVOLUTION STATE
    private bool _p1DinoEvolved = false;
    private bool _p2DinoEvolved = false;

    // LARRY GADON E SPECIAL - one-time stage switch + heal
    private bool _larryEUsed = false;

    // STARFIELD BACKGROUND
    private PointF[] _stars = new PointF[150];
    private Random _rand = new Random();

    // STAGE DUST EFFECT
    private PointF[] _stageDust = new PointF[80];
    private float[] _dustSpeeds = new float[80];
    private float[] _dustWobble = new float[80];

    // STREET FIGHTER CHAR SELECT EFFECTS
    private int _csHoverIndex = 0;       // The cell currently under mouse
    private float _csPulse = 0f;         // Glow pulse phase (0-2pi)
    private int _csFlashTimer = 0;       // Used to flash "PLAYER X SELECT" header

    // BLOOD ANIMATION
    private float _bloodDropY = 365;

    // GAMEPLAY VARIABLES
    private int p1Health = 300; // Tank health!
    private int p2Health = 80;
    private int p1X = 250;
    private int p1Y = 450;
    private bool _isPaused = false;
    private int _p1HurtTimer = 0;
    private int _p2HurtTimer = 0;
    private int _victoryTimer = 0;
    private int _matchWinner = -1;

    // PHYSICS & INPUT
    private bool _keyLeft, _keyRight, _keyQ, _keyW, _keyE, _keyR, _keyT;
    private bool _isJumping = false;
    private bool _facingLeft = false;
    private float _jumpVelocity = 0f;
    private const float GRAVITY = 2.5f;
    private const int GROUND_Y = 450;

    // PROJECTILE LOGIC
    private bool _projActive = false;
    private float _projX = 0f;
    private float _projY = 0f;
    private int _projDir = 1;

    // EFFECTS & P2 AI
    private int _impactTimer = 0;
    private float _impactX = 0f;
    private float _impactY = 0f;
    private bool _p2ProjActive = false;
    private float _p2ProjX = 0f;
    private float _p2ProjY = 0f;
    private int _p2ProjTimer = 80;

    // REAL FIGHTER MECHANICS
    private int _attackTimer = 0;
    private string _currentAttack = "";
    private bool _hasHitTarget = false;

    // PLAYER COMBO SYSTEM
    private string _lastAttack = "";
    private int _comboWindowTimer = 0;  // Ticks remaining to chain a combo
    private string _comboText = "";    // "STREET COMBO!" / "POWER COMBO!"
    private int _comboDisplayTimer = 0;

    // P2 AI COMBO BRAIN
    private int _p2X = 850;            // P2 moves now!
    private int _p2ActionTimer = 0;    // Time before next AI decision
    private string _p2Action = "";     // Current AI action
    private int _p2AttackTimer = 0;    // P2 melee lockout
    private string _p2CurrentAttack = "";
    private bool _p2HasHit = false;
    private int _p2ComboStep = 0;      // Which combo move is next
    private static readonly string[] _comboSequences = { "Q", "Q", "W", "E" }; // Q Q Kick Flying Punch!

    // DYNAMIC STAGE EFFECTS (Wrecking System)
    private int _shakeTimer = 0;
    private float _shakeIntensity = 0f;
    private int _flashTimer = 0;
    private Color _flashColor = Color.White;
    private class WreckageParticle { public float X, Y, VX, VY; public int Life; public Color Color; }
    private List<WreckageParticle> _wreckParticles = new List<WreckageParticle>();
    // WAR ZONE EFFECTS
    private bool _isWarZone = false;
    private List<WreckageParticle> _emberParticles = new List<WreckageParticle>();
    private SolidBrush _warBrushOverlay = new SolidBrush(Color.FromArgb(90, 80, 10, 0));

    public Form1()
    {
        InitializeComponent();

        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        this.UpdateStyles();
        this.ClientSize = new Size(1280, 720);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "Goon Warfare X";

        LoadImages();

        for (int i = 0; i < _stars.Length; i++)
        {
            _stars[i] = new PointF(_rand.Next(0, 1280), _rand.Next(0, 720));
        }

        for (int i = 0; i < _stageDust.Length; i++)
        {
            _stageDust[i] = new PointF(_rand.Next(0, 1280), _rand.Next(0, 720));
            _dustSpeeds[i] = (float)(_rand.NextDouble() * 3.5 + 0.5); // Random drift speed
            _dustWobble[i] = (float)(_rand.NextDouble() * Math.PI * 2); // Random initial wave
        }

        _animationTimer = new System.Windows.Forms.Timer();
        _animationTimer.Interval = 16; // 60 FPS for absolutely zero lag/choppiness
        _animationTimer.Tick += AnimationTimer_Tick;
        _animationTimer.Start();

        this.KeyDown += Form1_KeyDown;
        this.KeyUp += Form1_KeyUp;
        this.MouseMove += Form1_MouseMove;
        this.MouseWheel += (s, ev) =>
        {
            if (_currentState == GameState.StageSelect)
            {
                // Added 60 pixels for the text to be visible
                int totalRows = (_stages.Count + 2) / 3;
                int maxScroll = Math.Max(0, (180 + (totalRows - 1) * 220 + 240) - 680);
                _stageScrollY = Math.Clamp(_stageScrollY - ev.Delta / 2, 0, maxScroll);
                this.Invalidate();
            }
        };
    }
    private int _stageScrollY = 0;

    private string GetCharacterName(int id)
    {
        string[] names = { "GRIMACE", "WILLIE", "RAMON", "ARZADOWN", "DU30", "JABEE", "JANITOR", "LARRY", "MATRIX", "DINO HWK" };
        if (id >= 0 && id < names.Length) return names[id];
        return "UNKNOWN";
    }

    private string GetStageName(int id)
    {
        string[] names = { "Home of the Goons", "Paradahan ni Willie", "Dasma-SOGO", "BAHAY-NG-SOLONS", "SpaceX", "Museo ni Lods", "Feeling-Healthy" };
        if (id >= 0 && id < names.Length) return names[id];
        return "UNKNOWN STAGE";
    }
    private void StartMatch()
    {
        _currentState = GameState.Playing;
        _stageTimerTicks = 0;

        int totalRounds = _p1RoundsWon + _p2RoundsWon;
        if (totalRounds == 0)
        {
            _currentStageIndex = _selectedStageIndex;
        }
        else
        {
            // Pick a random stage for subsequent rounds
            if (_stages.Count > 1)
            {
                int nextStage = _currentStageIndex;
                while (nextStage == _currentStageIndex) nextStage = _rand.Next(0, _stages.Count);
                _currentStageIndex = nextStage;
            }
        }

        if (_stages.Count > _currentStageIndex)
        {
            this.BackgroundImage = _stages[_currentStageIndex];
        }

        p1Health = 200;
        p2Health = 200;
        p1X = 250;
        _p2X = 850;
        _p2ActionTimer = 0;
        _p2ComboStep = 0;
        _p2AttackTimer = 0;
        _p2CurrentAttack = "";
        _p2Action = "";
        _p2ProjActive = false;
        _projActive = false;
        _isKO = false;
        _p1HurtTimer = 0;
        _p2HurtTimer = 0;
        _matchTimer = 99;
        _tickAccumulator = 0;
        _larryEUsed = false; // Reset Larry's one-time E ability each match

        // Reset effects
        _shakeTimer = 0;
        _flashTimer = 0;
        _wreckParticles.Clear();
        _emberParticles.Clear();
        _isWarZone = false;
    }

    private void TriggerShake(int duration, float intensity)
    {
        _shakeTimer = duration;
        _shakeIntensity = intensity;
    }

    private void TriggerFlash(int duration, Color color)
    {
        _flashTimer = duration;
        _flashColor = color;
    }

    private void DemolishAndNextStage()
    {
        _isWarZone = true; // Turn the stage into a chaotic war environment!

        // Massive wreckage outburst
        SpawnWreckage(640, 400, 40);
        TriggerShake(25, 25f);
    }

    private void SpawnWreckage(int x, int y, int count)
    {
        for (int i = 0; i < count; i++)
        {
            _wreckParticles.Add(new WreckageParticle
            {
                X = x,
                Y = y,
                VX = (float)(_rand.NextDouble() * 30 - 15),
                VY = (float)(_rand.NextDouble() * -25 - 5),
                Life = _rand.Next(15, 40),
                Color = _rand.Next(0, 10) > 5 ? Color.FromArgb(150, 40, 40, 40) : Color.FromArgb(150, 100, 100, 100)
            });
        }
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (_currentState == GameState.Playing)
        {
            if (_isPaused) return;

            if (_p1HurtTimer > 0) _p1HurtTimer--;
            if (_p2HurtTimer > 0) _p2HurtTimer--;

            // DUST PARTICLE EFFECT UPDATE
            for (int i = 0; i < _stageDust.Length; i++)
            {
                _dustWobble[i] += 0.05f; // wave phase
                _stageDust[i].X -= _dustSpeeds[i]; // drift left
                _stageDust[i].Y += (float)Math.Sin(_dustWobble[i]) * 1.5f; // Bob up and down

                if (_stageDust[i].X < -20)
                {
                    _stageDust[i].X = 1300;
                    _stageDust[i].Y = _rand.Next(0, 720);
                }
            }

            if (_isKO)
            {
                _koTimer--;
                if (_koTimer <= 0)
                {
                    // Evaluate Match Winner
                    if (_p1RoundsWon >= _maxRounds || _p2RoundsWon >= _maxRounds)
                    {
                        _currentState = GameState.VictoryScreen;
                        _matchWinner = _p1RoundsWon >= _maxRounds ? 0 : 1;
                        _victoryTimer = 0;
                    }
                    else StartMatch(); // Next round!
                }
                this.Invalidate();
                return;
            }

            // UPDATE DYNAMIC EFFECTS
            if (_shakeTimer > 0) _shakeTimer--;
            if (_flashTimer > 0) _flashTimer--;
            for (int i = _wreckParticles.Count - 1; i >= 0; i--)
            {
                var p = _wreckParticles[i];
                p.X += p.VX;
                p.Y += p.VY;
                p.VY += 1.5f; // Gravity
                p.Life--;
                if (p.Y > 670) { p.Y = 670; p.VY = -p.VY * 0.4f; p.VX *= 0.8f; } // Floor bounce
                if (p.Life <= 0) _wreckParticles.RemoveAt(i);
            }

            if (_isWarZone)
            {
                // Spawn new embers randomly
                if (_rand.Next(0, 10) < 5)
                {
                    _emberParticles.Add(new WreckageParticle
                    {
                        X = _rand.Next(0, 1280),
                        Y = 720,
                        VX = (float)(_rand.NextDouble() * 4 - 2),
                        VY = (float)(_rand.NextDouble() * -4 - 2),
                        Life = _rand.Next(50, 100),
                        Color = Color.FromArgb(200, 255, _rand.Next(100, 200), 0)
                    });
                }

                // Drift embers upwards
                for (int i = _emberParticles.Count - 1; i >= 0; i--)
                {
                    var p = _emberParticles[i];
                    p.X += p.VX + (float)(Math.Sin(_tickAccumulator * 0.5) * 2); // Wavy rising
                    p.Y += p.VY;
                    p.Life--;
                    if (p.Life <= 0) _emberParticles.RemoveAt(i);
                }
            }

            // Normal match timer
            if (!_isTimeInfinite)
            {
                _tickAccumulator++;
                if (_tickAccumulator >= 60) // 60 ticks of 16ms = ~1 sec
                {
                    _tickAccumulator = 0;
                    _matchTimer--;
                    if (_matchTimer <= 0)
                    {
                        _isKO = true; // Time Out!
                        _koTimer = 100;
                        if (p1Health > (p2Health * 1.5)) _p1RoundsWon++; // Scaled ratio win
                        else _p2RoundsWon++;
                    }
                }
            }

            // Rotate background stage mid-round (after 50 seconds elapsed)
            _stageTimerTicks++;
            if (_stageTimerTicks == 60 * 50) // roughly 50 seconds elapsed
            {
                // Advance exactly once for the second half of the round
                if (_stages.Count > 0)
                {
                    _currentStageIndex = (_currentStageIndex + 1) % _stages.Count;
                    this.BackgroundImage = _stages[_currentStageIndex];
                }
            }

            // Check KO (Health 0)
            if (p1Health <= 0 || p2Health <= 0)
            {
                _isKO = true;
                _koTimer = 100;
                if (p1Health <= 0) _p2RoundsWon++;
                if (p2Health <= 0) _p1RoundsWon++;
            }

            // --- GAMEPAD POLLING (Player 1) ---
            bool padLeft = false, padRight = false;
            bool btnJump = false, btnQ = false, btnW = false, btnE = false, btnR = false, btnT = false;

            JOYINFOEX info = new JOYINFOEX();
            info.dwSize = Marshal.SizeOf(typeof(JOYINFOEX));
            info.dwFlags = 255; // JOY_RETURNALL
            if (joyGetPosEx(0, ref info) == 0) // Joystick 0 connected
            {
                // X Axis
                if (info.dwXpos < 20000) padLeft = true;
                if (info.dwXpos > 45000) padRight = true;

                // D-Pad POV Hat
                if (info.dwPOV == 27000) padLeft = true;
                if (info.dwPOV == 9000) padRight = true;

                // Face Buttons (Common DirectInput map for PS4/Generic)
                if ((info.dwButtons & 2) != 0) btnJump = true; // Cross
                if ((info.dwButtons & 1) != 0) btnQ = true;    // Square
                if ((info.dwButtons & 8) != 0) btnW = true;    // Triangle
                if ((info.dwButtons & 4) != 0) btnE = true;    // Circle
                if ((info.dwButtons & 32) != 0 || (info.dwButtons & 128) != 0) btnR = true;  // R1/R2
                if ((info.dwButtons & 16) != 0 || (info.dwButtons & 64) != 0) btnT = true;   // L1/L2
            }

            // JUMP TRIGGER (can interrupt everything but not mid-air)
            if (btnJump && !_isJumping) { _isJumping = true; _jumpVelocity = -32f; }

            // FIGHTER STATE MACHINE (Attack Lockouts)
            if (_attackTimer > 0)
            {
                _attackTimer--;
                if (_attackTimer == 0) _currentAttack = "";
            }

            // MOVEMENT (Locked out while attacking, unless jumping)
            if (_attackTimer == 0 || _isJumping)
            {
                if (_keyRight || padRight) { p1X += 22; _facingLeft = false; }
                if (_keyLeft || padLeft) { p1X -= 22; _facingLeft = true; }
            }

            if (p1X < 0) p1X = 0;
            if (p1X > 1280 - 250) p1X = 1280 - 250;

            // JUMP LOGIC
            if (_isJumping)
            {
                p1Y += (int)_jumpVelocity;
                _jumpVelocity += GRAVITY;
                if (p1Y >= GROUND_Y)
                {
                    p1Y = GROUND_Y;
                    _isJumping = false;
                    _jumpVelocity = 0;
                }
            }

            // COMBO WINDOW COOLDOWN
            if (_comboWindowTimer > 0) _comboWindowTimer--;
            else _lastAttack = "";
            if (_comboDisplayTimer > 0) _comboDisplayTimer--;

            // ATTACK INPUT + COMBO DETECTION
            if (_attackTimer == 0) // Removed !_isJumping to allow mid-air attacks!
            {
                string next = "";
                if (_keyQ || btnQ) next = "Q";
                else if (_keyW || btnW) next = "W";
                else if (_keyE || btnE) next = "E";
                else if (_keyR || btnR) next = "R";
                else if (_keyT || btnT) next = "T";

                if (next != "")
                {
                    // Check for combo
                    bool combo = false;
                    if (next == "W" && _lastAttack == "Q" && _comboWindowTimer > 0)
                    { // Q → W = STREET COMBO
                        _currentAttack = "W"; _attackTimer = 12; _hasHitTarget = false;
                        _comboText = "STREET COMBO!"; _comboDisplayTimer = 40;
                        combo = true;
                        // Bonus instant damage
                        int p1c = p1X + 110; int p2c = _p2X + 55;
                        if (Math.Abs(p1c - p2c) <= 250) { p2Health -= 8; if (p2Health < 0) p2Health = 0; _p2HurtTimer = 12; _impactX = (p1c + p2c) / 2; _impactY = 500; _impactTimer = 8; }
                    }
                    else if (next == "E" && _lastAttack == "Q" && _comboWindowTimer > 0)
                    { // Q → E = POWER COMBO
                        _currentAttack = "E"; _attackTimer = 14; _hasHitTarget = false;
                        _comboText = "POWER COMBO!"; _comboDisplayTimer = 40;
                        combo = true;
                        p1X += _facingLeft ? -50 : 50;
                        int p1c = p1X + 110; int p2c = _p2X + 55;
                        if (Math.Abs(p1c - p2c) <= 280) { p2Health -= 18; if (p2Health < 0) p2Health = 0; _p2HurtTimer = 16; _impactX = (p1c + p2c) / 2; _impactY = 500; _impactTimer = 10; }
                    }

                    if (!combo)
                    {
                        _currentAttack = next; _hasHitTarget = false;
                        if (next == "Q") { _attackTimer = 6; }
                        else if (next == "W") { _attackTimer = 8; }
                        else if (next == "E")
                        {
                            _attackTimer = 10;
                            // LARRY GADON E: one-time stage switch to 4th stage + health boost
                            if (_selectedCharacter == 7 && !_larryEUsed && _stages.Count >= 4)
                            {
                                _larryEUsed = true;
                                _currentStageIndex = 3; // Force 4th stage (index 3)
                                this.BackgroundImage = _stages[_currentStageIndex];
                                p1Health = Math.Min(200, p1Health + 80); // Heal up to 80 HP (capped at 200)
                                _comboText = "LARRY HEALS!"; _comboDisplayTimer = 60;
                            }
                            else if (_selectedCharacter != 7) { p1X += _facingLeft ? -40 : 40; } // normal E for others
                        }
                        else if (next == "R") { _attackTimer = 8; if (!_projActive) { _projActive = true; _projDir = _facingLeft ? -1 : 1; _projX = p1X + (_facingLeft ? -40 : 200); _projY = p1Y + 120; } }
                        else if (next == "T") { _attackTimer = 14; p1X += _facingLeft ? -30 : 30; } // Heavy smash
                    }

                    _lastAttack = next;
                    _comboWindowTimer = 18; // ~0.5s window to chain
                }
            }

            _blinkCounter++;

            // ================================================================
            // P2 AI COMBO BRAIN
            // ================================================================
            int p2CenterX = _p2X + 55; // center of p2 body
            int p1CenterX = p1X + 110;
            int dist = Math.Abs(p2CenterX - p1CenterX);

            if (!_isTrainingMode)
            {
                // P2 MELEE LOCKOUT
                if (_p2AttackTimer > 0)
                {
                    _p2AttackTimer--;
                    if (_p2AttackTimer == 0) { _p2CurrentAttack = ""; _p2HasHit = false; }
                }

                // P2 ACTION TIMER - decides next move
                if (_p2ActionTimer > 0)
                {
                    _p2ActionTimer--;
                }
                else
                {
                    // Decision logic
                    if (dist > 300) // Far away -> approach or shoot
                    {
                        _p2Action = _rand.Next(2) == 0 ? "WALK" : "SHOOT";
                        _p2ActionTimer = _rand.Next(20, 50);
                    }
                    else if (dist > 130) // Mid range -> walk in or shoot
                    {
                        _p2Action = _rand.Next(3) == 0 ? "SHOOT" : "WALK";
                        _p2ActionTimer = _rand.Next(10, 30);
                    }
                    else // Close range -> combo!
                    {
                        if (_p2AttackTimer == 0)
                        {
                            _p2CurrentAttack = _comboSequences[_p2ComboStep % _comboSequences.Length];
                            _p2ComboStep++;
                            _p2AttackTimer = _p2CurrentAttack == "E" ? 14 : (_p2CurrentAttack == "W" ? 10 : 7);
                            _p2HasHit = false;
                            _p2Action = "ATTACK";
                        }
                        _p2ActionTimer = _rand.Next(5, 15);
                    }
                }

                // Execute P2 movement
                if (_p2Action == "WALK" && _p2AttackTimer == 0)
                {
                    int step = 12; // Increased AI speed for faster gameplay
                    if (p2CenterX > p1CenterX) _p2X -= step; // Walk left (toward P1)
                    else _p2X += step;
                    _p2X = Math.Clamp(_p2X, 300, 1200);
                }

                // P2 SHOOT (projectile)
                _p2ProjTimer--;
                if (_p2ProjTimer <= 0 || _p2Action == "SHOOT")
                {
                    _p2Action = "";
                    _p2ProjActive = true;
                    _p2ProjX = _p2X;
                    _p2ProjY = _p2X + 60; // chest height relative
                    _p2ProjY = 510f;       // fixed ground-level
                    _p2ProjTimer = _rand.Next(60, 130);
                }

                // P2 MELEE HIT DETECTION
                if (_p2AttackTimer > 0 && !_p2HasHit && _p2CurrentAttack != "")
                {
                    int p2Reach = _p2CurrentAttack == "E" ? 180 : 120;
                    // P2 always faces left, so attacks reach left
                    Rectangle p2AttackBox = new Rectangle(_p2X - p2Reach, 450 + 50, p2Reach, 100);
                    Rectangle p1HurtBox = new Rectangle(p1X + 40, p1Y + 40, 130, 180);
                    if (p2AttackBox.IntersectsWith(p1HurtBox))
                    {
                        _p2HasHit = true;
                        int dmg = _p2CurrentAttack == "Q" ? 5 : (_p2CurrentAttack == "W" ? 9 : 14);
                        p1Health -= dmg;
                        if (p1Health < 0) p1Health = 0;
                        _p1HurtTimer = _p2CurrentAttack == "E" ? 16 : 10;
                        _impactX = p1X + 110; _impactY = p1Y + 100; _impactTimer = 6;
                        TriggerShake(5, 5f); // Minor shake on hit
                    }
                }

                if (_p2ProjActive)
                {
                    _p2ProjX -= 25; // Travels left!
                    if (_p2ProjX < -100) _p2ProjActive = false;

                    Rectangle p2ProjHitbox = new Rectangle((int)_p2ProjX, (int)_p2ProjY, 50, 25);
                    Rectangle p1Hitbox = new Rectangle(p1X + 50, p1Y + 40, 120, 180);
                    if (p2ProjHitbox.IntersectsWith(p1Hitbox) && !_isJumping)
                    {
                        p1Health -= 7; // Projectile damage - same as P1's hadouken
                        if (p1Health < 0) p1Health = 0;
                        _p1HurtTimer = 12;
                        _p2ProjActive = false;
                        _impactX = p1X + 110; _impactY = _p2ProjY; _impactTimer = 6;
                        TriggerShake(6, 6f);
                        TriggerFlash(3, Color.FromArgb(100, Color.White));
                    }
                }
            } // End AI Bypass for Training

            // IMPACT TIMER
            if (_impactTimer > 0) _impactTimer--;

            // PROJECTILE HIT DETECTION
            if (_projActive)
            {
                _projX += 30 * _projDir;
                if (_projX < -200 || _projX > 1400) _projActive = false;
                Rectangle p2HBProj = new Rectangle(_p2X, 450, 110, 220);
                Rectangle projHitbox = new Rectangle((int)_projX, (int)_projY, 80, 40);
                if (_projActive && p2HBProj.IntersectsWith(projHitbox))
                {
                    p2Health -= 7; if (p2Health < 0) p2Health = 0; // Matches P2 proj damage
                    _p2HurtTimer = 12;
                    _projActive = false;
                    _impactX = _projX + 40; _impactY = _projY; _impactTimer = 6; // Giant spark on The Dean!

                    // Trigger Stage Wrecking Effects!
                    if (_selectedCharacter == 4) // Du30 Bomb
                    {
                        TriggerShake(15, 18f);
                        TriggerFlash(10, Color.FromArgb(180, Color.OrangeRed));
                        SpawnWreckage((int)_impactX, (int)_impactY, 20);
                        DemolishAndNextStage(); // Du30 bomb demolishes world!
                    }
                    else if (_selectedCharacter == 7) // Larry Nuke
                    {
                        TriggerShake(35, 30f);
                        TriggerFlash(25, Color.FromArgb(200, Color.White));
                        SpawnWreckage((int)_impactX, (int)_impactY, 50);
                        DemolishAndNextStage(); // Larry nuke demolishes world!
                    }
                    else // Normal Hadouken
                    {
                        TriggerShake(8, 7f);
                        TriggerFlash(4, Color.FromArgb(80, Color.MediumPurple));
                    }
                }
            }

            // MELEE HIT DETECTION - pure distance, no facing requirement
            if (_attackTimer > 0 && !_hasHitTarget && (_currentAttack == "Q" || _currentAttack == "W" || _currentAttack == "E" || _currentAttack == "T"))
            {
                int p1Center = p1X + 110;
                int p2Center = _p2X + 55;
                int meleeDist = Math.Abs(p1Center - p2Center);
                int reach = (_currentAttack == "E" || _currentAttack == "T") ? 260 : 200;
                if (meleeDist <= reach)
                {
                    _hasHitTarget = true;
                    int damage = _currentAttack == "Q" ? 5 : (_currentAttack == "W" ? 9 : (_currentAttack == "T" ? 20 : 14));
                    p2Health -= damage;
                    if (p2Health < 0) p2Health = 0;
                    _p2HurtTimer = (_currentAttack == "T" || _currentAttack == "E") ? 16 : 10;
                    _impactX = (p1Center + p2Center) / 2; _impactY = 500; _impactTimer = 6;
                }
            }

            // TRAINING MODE INFINITE HEALTH
            if (_isTrainingMode)
            {
                p2Health = 200;
                p1Health = 200;
            }

            this.Invalidate();
        }
        else
        {
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].Y += (i % 4) + 1;
                if (_stars[i].Y > 720) { _stars[i].Y = 0; _stars[i].X = _rand.Next(0, 1280); }
            }

            _blinkCounter++;
            if (_blinkCounter >= 16) { _showMenuText = !_showMenuText; _showCursor = !_showCursor; _blinkCounter = 0; }
            _bloodDropY += 4; if (_bloodDropY > 800) _bloodDropY = 365;
            this.Invalidate();
        }
    }

    private Bitmap CropImage(Image source, Rectangle cropRect)
    {
        Bitmap bmp = new Bitmap(cropRect.Width, cropRect.Height);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.DrawImage(source, new Rectangle(0, 0, bmp.Width, bmp.Height), cropRect, GraphicsUnit.Pixel);
        }
        return bmp;
    }

    // Auto-crop transparent/near-black border padding from a bitmap so the character fills the frame tightly
    private Bitmap TrimTransparentEdges(Bitmap src)
    {
        int left = src.Width, top = src.Height, right = 0, bottom = 0;
        for (int y = 0; y < src.Height; y++)
        {
            for (int x = 0; x < src.Width; x++)
            {
                Color c = src.GetPixel(x, y);
                if (c.A > 20) // visible pixel
                {
                    if (x < left) left = x;
                    if (x > right) right = x;
                    if (y < top) top = y;
                    if (y > bottom) bottom = y;
                }
            }
        }
        if (right <= left || bottom <= top) return src; // nothing to trim
        int pad = 2; // tight padding for head-to-toe look
        left = Math.Max(0, left - pad);
        top = Math.Max(0, top - pad);
        right = Math.Min(src.Width - 1, right + pad);
        bottom = Math.Min(src.Height - 1, bottom + pad);
        Rectangle trimRect = new Rectangle(left, top, right - left + 1, bottom - top + 1);
        return src.Clone(trimRect, src.PixelFormat);
    }

    private void LoadImages()
    {
        string assetsDir = Path.Combine(Application.StartupPath, "Assets");
        if (File.Exists(Path.Combine(assetsDir, "Menu bg.jpg"))) _menuBackground = Image.FromFile(Path.Combine(assetsDir, "Menu bg.jpg"));

        // Load all stages in order
        string[] stageFiles = { "First stage.jpg", "Second stage.jpg", "third_stage.jpg", "fourth_stage.jpg", "fifth_stage.jpg", "sixth_stage.jpg", "PhilHealth.jpg" };
        foreach (var sf in stageFiles)
        {
            string p = Path.Combine(assetsDir, sf);
            if (File.Exists(p)) _stages.Add(Image.FromFile(p));
        }

        string cjoyPath = Path.Combine(assetsDir, "chickenjoy.jpg");
        if (!File.Exists(cjoyPath)) cjoyPath = Path.Combine(assetsDir, "chickenjoy.png");
        if (File.Exists(cjoyPath)) _jabeeProjectile = ProcessBackgroundRemoval(cjoyPath, true, false, false, true);

        Image? LoadIdle(string filename, bool wBg = true, bool bBg = false, bool yBg = false, bool autoBg = false)
        {
            string p = Path.Combine(assetsDir, filename);
            if (!File.Exists(p)) return null;
            Image? baseImg = ProcessBackgroundRemoval(p, wBg, bBg, yBg, autoBg);
            if (baseImg == null) return null;
            int offset = (filename == "Willie.jpg" || filename == "Ramon ang.jpg") ? (baseImg.Height > baseImg.Width + 100 ? 80 : 0) : 0;
            return TrimTransparentEdges(CropImage(baseImg, new Rectangle(0, offset, baseImg.Width / 4, (baseImg.Height - offset) / 4)));
        }

        _imgIdle_Grimace = LoadIdle("grimace.jpg", false, false, true); // yellow bg removal
        if (_imgIdle_Grimace == null) _imgIdle_Grimace = LoadIdle("grimace.png", false, false, true);
        _imgIdle_Willie = LoadIdle("Willie.jpg", false, false, false, true); // Willie uses auto keying to protect dark suit
        _imgIdle_Ramon = LoadIdle("Ramon ang.jpg", true);
        _imgIdle_Arzadown = LoadIdle("Arzadown.jpg", false, false, false, true);
        _imgIdle_Du30 = LoadIdle("Du30.jpg", true); // Du30 has clean white background, use dedicated white removal to clear halo
        _imgIdle_Jabee = LoadIdle("Jabee2.jpg", false, false, false, true);
        _imgIdle_Janitor = LoadIdle("Janitor_in_mcdo.jpg", false, false, false, true);
        _imgIdle_Larry = LoadIdle("larry_gadon.jpg", false, false, false, true);
        _imgIdle_Matrix = LoadIdle("matrix.jpg", false, false, false, true);
        _imgIdle_Stephen = LoadIdle("Dino Hwaking.jpg", true, false, false, false);

        this.BackgroundImage = _menuBackground;
        this.BackgroundImageLayout = ImageLayout.Stretch;

        // Actual frame loading happens in LoadCharacterFrames
    }

    private Bitmap ProcessBackgroundRemoval(string path, bool isWhiteBackground, bool isBlackBackground = false, bool isYellowBackground = false, bool isAutoKey = false)
    {
        using (Bitmap original = new Bitmap(path))
        {
            Bitmap bmp = original.Clone(new Rectangle(0, 0, original.Width, original.Height), PixelFormat.Format32bppArgb);

            Color cornerColor = bmp.GetPixel(Math.Min(5, bmp.Width - 1), Math.Min(5, bmp.Height - 1)); // Sample inside to avoid outer borders
            int tolerance = 15; // Tight threshold to avoid destroying white/gray shirts
            if (isYellowBackground) tolerance = 80; // High tolerance to crush yellow JPG artifacts for Grimace

            var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] rgbaValues = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, rgbaValues, 0, bytes);
            int stride = bmpData.Stride;
            int width = bmp.Width;
            int height = bmp.Height;

            if (isAutoKey)
            {
                // Use a flood fill from the outer edges to prevent removing colors INSIDE the character
                bool[] visited = new bool[width * height];
                int[] qX = new int[width * height];
                int[] qY = new int[width * height];
                int head = 0, tail = 0;

                for (int x = 0; x < width; x++)
                {
                    visited[0 * width + x] = true; qX[tail] = x; qY[tail++] = 0;
                    visited[(height - 1) * width + x] = true; qX[tail] = x; qY[tail++] = height - 1;
                }
                for (int y = 0; y < height; y++)
                {
                    if (!visited[y * width + 0]) { visited[y * width + 0] = true; qX[tail] = 0; qY[tail++] = y; }
                    if (!visited[y * width + width - 1]) { visited[y * width + width - 1] = true; qX[tail] = width - 1; qY[tail++] = y; }
                }

                while (head < tail)
                {
                    int x = qX[head];
                    int y = qY[head];
                    head++;

                    int i = y * stride + x * 4;
                    byte b = rgbaValues[i];
                    byte gr = rgbaValues[i + 1];
                    byte r = rgbaValues[i + 2];

                    if (Math.Abs(r - cornerColor.R) <= tolerance &&
                        Math.Abs(gr - cornerColor.G) <= tolerance &&
                        Math.Abs(b - cornerColor.B) <= tolerance)
                    {
                        rgbaValues[i + 3] = 0; // Set transparent

                        if (x > 0 && !visited[y * width + (x - 1)]) { visited[y * width + (x - 1)] = true; qX[tail] = x - 1; qY[tail++] = y; }
                        if (x < width - 1 && !visited[y * width + (x + 1)]) { visited[y * width + (x + 1)] = true; qX[tail] = x + 1; qY[tail++] = y; }
                        if (y > 0 && !visited[(y - 1) * width + x]) { visited[(y - 1) * width + x] = true; qX[tail] = x; qY[tail++] = y - 1; }
                        if (y < height - 1 && !visited[(y + 1) * width + x]) { visited[(y + 1) * width + x] = true; qX[tail] = x; qY[tail++] = y + 1; }
                    }
                }
            }
            else
            {
                for (int i = 0; i < rgbaValues.Length; i += 4)
                {
                    byte b = rgbaValues[i];     // Blue
                    byte gr = rgbaValues[i + 1];  // Green
                    byte r = rgbaValues[i + 2];   // Red
                    byte a = rgbaValues[i + 3];   // Alpha
                    if (a == 0) continue;
                    bool isBg = false;

                    if (isBlackBackground)
                    {
                        // Remove black + near-black background pixels aggressively for clean edges
                        isBg = (r < 50 && gr < 50 && b < 50);
                    }
                    else if (isWhiteBackground)
                    {
                        // Lowered to 215 to catch off-white artifacts/halos
                        isBg = (r > 215 && gr > 215 && b > 215);
                    }
                    else if (isYellowBackground)
                    {
                        isBg = (r > 150 && gr > 150 && b < 100);
                    }
                    else
                    {
                        // Green screen removal
                        isBg = (gr > r + 5 && gr > b + 5);
                    }
                    if (isBg) rgbaValues[i + 3] = 0; // Set alpha to transparent
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(rgbaValues, 0, bmpData.Scan0, bytes);
            bmp.UnlockBits(bmpData);
            return bmp;
        }
    }



    private void LoadCharacterFrames(int charType, int p2CharType)
    {
        string assetsDir = Path.Combine(Application.StartupPath, "Assets");

        // Custom frames fallback for P1
        string pIdle = File.Exists(Path.Combine(assetsDir, "idle.jpg")) ? "idle.jpg" : "idle.png";
        string pWalk = File.Exists(Path.Combine(assetsDir, "walk.jpg")) ? "walk.jpg" : "walk.png";
        string pStance = File.Exists(Path.Combine(assetsDir, "stance.jpg")) ? "stance.jpg" : "stance.png";
        string pPunch = File.Exists(Path.Combine(assetsDir, "punch.jpg")) ? "punch.jpg" : "punch.png";
        string pKick = File.Exists(Path.Combine(assetsDir, "kick.jpg")) ? "kick.jpg" : "kick.png";

        if (File.Exists(Path.Combine(assetsDir, pIdle)))
        {
            _imgIdle = ProcessBackgroundRemoval(Path.Combine(assetsDir, pIdle), false);
            _imgWalk = ProcessBackgroundRemoval(Path.Combine(assetsDir, pWalk), false);
            _imgStance = ProcessBackgroundRemoval(Path.Combine(assetsDir, pStance), false);
            _imgPunch = ProcessBackgroundRemoval(Path.Combine(assetsDir, pPunch), false);
            _imgKick = ProcessBackgroundRemoval(Path.Combine(assetsDir, pKick), false);
            return;
        }

        // Helper to load one character's frames into a set of out-variables
        void LoadFrames(int type,
            out Image? idle, out Image? walk, out Image? stance,
            out Image? punch, out Image? kick, out Image? flyingPunch, out Image? hadouken, out Image? special, out Image? jump)
        {
            idle = walk = stance = punch = kick = flyingPunch = hadouken = special = jump = null;
            Image? baseFighter = null;

            string fn = "";
            bool wBg = true, yBg = false, bBg = false, autoBg = false;
            int cols = 4, rows = 4;
            switch (type)
            {
                case 0: fn = "grimace.jpg"; wBg = false; yBg = true; break;
                case 1: fn = "Willie.jpg"; wBg = false; autoBg = true; break;
                case 2: fn = "Ramon ang.jpg"; break;
                case 3: fn = "Arzadown.jpg"; autoBg = true; break;
                case 4: fn = "Du30.jpg"; autoBg = true; break;
                case 5: fn = "Jabee2.jpg"; autoBg = true; break;
                case 6: fn = "Janitor_in_mcdo.jpg"; autoBg = true; break;
                case 7: fn = "larry_gadon.jpg"; autoBg = true; rows = 4; break;
                case 8: fn = "matrix.jpg"; autoBg = true; rows = 4; cols = 4; break;
                case 9: fn = "Dino Hwaking.jpg"; autoBg = false; wBg = true; break;
            }

            string p = Path.Combine(assetsDir, fn);
            if (!File.Exists(p) && type == 0) p = Path.Combine(assetsDir, "grimace.png");
            if (File.Exists(p)) baseFighter = ProcessBackgroundRemoval(p, wBg, bBg, yBg, autoBg);

            if (baseFighter != null)
            {
                int fW = baseFighter.Width / cols;
                // Only Willie (type==1) needs a top-offset; Ramon and all others start at y=0
                int offset = (type == 1) ? (baseFighter.Height > baseFighter.Width + 100 ? 80 : 0) : 0;
                int fH = baseFighter.Height / rows; // Always use full height divided by rows for clean tile sizing

                Image? frame(int col, int row, int off = -1)
                {
                    // Reduced inset to 4 (was 8) to preserve character silhouette while preventing bleed
                    int inset = (type == 4) ? 4 : 2;
                    int w = fW - inset * 2;
                    int h = fH - inset * 2;
                    int x = col * fW + inset;
                    int y = (off == -1 ? offset : off) + row * fH + inset;
                    if (w <= 0 || h <= 0) return null; // Safe check
                    Bitmap cell = CropImage(baseFighter, new Rectangle(x, y, w, h));

                    if (type == 9)
                    {
                        // Safely erase Top-Left corner numbers pixel-by-pixel to avoid GDI+ bugs
                        for (int py = 0; py < 60 && py < cell.Height; py++)
                        {
                            for (int px = 0; px < 60 && px < cell.Width; px++)
                            {
                                cell.SetPixel(px, py, Color.Transparent);
                            }
                        }
                    }
                    else if (type == 5)
                    {
                        // Erase the top 5 pixels to remove the white bleeding artifact line from the sprite sheet
                        for (int py = 0; py < 6 && py < cell.Height; py++)
                        {
                            for (int px = 0; px < cell.Width; px++)
                            {
                                cell.SetPixel(px, py, Color.Transparent);
                            }
                        }
                    }
                    return TrimTransparentEdges(cell);
                }

                if (type == 5) // Jabee's custom mapping
                {
                    idle = frame(0, 0); // Tile 1
                    walk = frame(1, 0);
                    stance = frame(0, 0); // Tile 1
                    punch = frame(3, 0); // Q = Tile 4
                    kick = frame(1, 1); // W = Tile 6
                    jump = frame(3, 1); // Space = Tile 8
                    flyingPunch = frame(3, 2); // E = Tile 12
                    hadouken = frame(0, 3); // R = Tile 13
                }
                else if (type == 8) // Matrix custom tile mapping (4col x 4row = 16 tiles)
                {
                    idle = frame(0, 0); // Tile 1  - idle (black coat, ready)
                    stance = frame(1, 0); // Tile 2  - fighting stance
                    walk = frame(0, 2); // Tile 9  - teleport dissolve (walk)
                    punch = frame(0, 3); // Tile 13 - Q punch
                    kick = frame(1, 2); // Tile 10 - W evolved kick
                    jump = frame(3, 2); // Tile 12 - jump/crouch
                    flyingPunch = frame(2, 2); // Tile 11 - E matrix code form (evolve)
                    hadouken = frame(1, 3); // Tile 14 - R Hadouken (green matrix ball)
                    special = frame(2, 3); // Tile 15 - green matrix body evolved
                }
                else if (type == 9) // Dino Hawking custom mapping
                {
                    idle = frame(2, 2); // Tile 11
                    walk = frame(2, 2);
                    stance = frame(2, 2); // Tile 11
                    punch = frame(0, 2); // Q = Tile 9
                    kick = frame(0, 0); // W = Tile 1 (evolve)
                    jump = frame(2, 2); // generic
                    flyingPunch = frame(3, 0); // E = Tile 4 (regenerate)
                    hadouken = frame(3, 3); // R = Tile 16 (self destruct)
                    special = frame(3, 3); // generic
                }
                else if (type == 2) // Ramon custom tile mapping (4col x 4row = 16 tiles, white bg)
                {
                    idle = frame(0, 0); // Tile 1  - idle fists raised
                    stance = frame(3, 0); // Tile 4  - fighting stance
                    walk = frame(3, 1); // Tile 8  - walk forward
                    punch = frame(0, 2); // Tile 9  - Q punch jab
                    kick = frame(3, 2); // Tile 12 - W Petron station slam
                    flyingPunch = frame(0, 3); // Tile 13 - E Petron lifted overhead
                    hadouken = frame(1, 3); // Tile 14 - R Goon ball Hadouken
                    jump = frame(3, 0); // Tile 4  - jump same as stance
                    special = frame(3, 3); // Tile 16 - Petron throw
                }
                else if (type == 7) // Larry Gadon custom tile mapping (4col x 4row = 16 tiles)
                {
                    idle = frame(0, 0); // Tile 1  - idle guard stance
                    stance = frame(3, 0); // Tile 4  - fighting stance
                    walk = frame(0, 1); // Tile 5  - walk step
                    punch = frame(0, 1); // Tile 5  - Q punch
                    kick = frame(1, 2); // Tile 10 - W high kick
                    flyingPunch = frame(3, 0); // Tile 4  - E (same as stance, effect is stage switch)
                    hadouken = frame(0, 3); // Tile 13 - R hadouken pose (BOBO projectile)
                    jump = frame(3, 0); // Tile 4  - jump same as stance
                    special = frame(3, 3); // Tile 16 - special kick
                }
                else if (type == 4) // Du30 custom tile mapping (4col x 4row = 16 tiles)
                {
                    // Tiles: Row0=cols0-3 (T1-T4), Row1=cols0-3 (T5-T8), Row2=cols0-3 (T9-T12), Row3=cols0-3 (T13-T16)
                    idle = frame(3, 0); // Tile 4  - Requested stance/idle
                    stance = frame(3, 0); // Tile 4  - Requested fighting stance
                    walk = frame(1, 0); // Tile 2  - walk / advance
                    punch = frame(1, 1); // Tile 6  - Q: jab punch step
                    kick = frame(2, 2); // Tile 11 - W: gun aiming/firing
                    flyingPunch = frame(0, 1); // Tile 5  - E: NOW MAPPED TO TILE 5 as requested
                    hadouken = frame(1, 3); // Tile 14 - R: gun muzzle BOMB shot
                    jump = frame(0, 1); // Tile 5  - jump pose
                    special = frame(3, 3); // Tile 16 - T: rifle slung special
                }
                else if (type == 6) // Janitor custom tile mapping
                {
                    idle = frame(0, 0);
                    walk = frame(1, 0);
                    stance = frame(2, 0);
                    punch = frame(0, 1);
                    kick = frame(1, 1);
                    flyingPunch = frame(2, 1);
                    hadouken = frame(1, 3); // Tile 14 as requested!
                    special = frame(0, 2);
                    jump = frame(1, 2);
                }
                else if (type >= 3) // New Characters generic different skill mapping
                {
                    idle = frame(0, 0);
                    walk = frame(1, 0);
                    stance = frame(2, 0);
                    punch = frame(0, 1);
                    kick = frame(1, 1);
                    flyingPunch = frame(2, 1);
                    hadouken = frame(3, 1);
                    special = frame(0, 2);
                    jump = frame(1, 2);
                }
                else // Grimace & Willie
                {
                    idle = frame(0, 0);
                    walk = frame(1, 0);
                    punch = frame(0, 1);
                    kick = frame(2, 1);
                    flyingPunch = frame(3, 1);
                    hadouken = frame(1, 2);
                    stance = frame(3, 2);
                    if (type == 1) jump = frame(3, 3);
                }
            }
        }

        // Load P1
        LoadFrames(charType,
            out _imgIdle, out _imgWalk, out _imgStance,
            out _imgPunch, out _imgKick, out _imgFlyingPunch, out _imgHadouken, out _imgSpecial, out _imgJump);

        // Load P2
        LoadFrames(p2CharType,
            out _p2ImgIdle, out _p2ImgWalk, out _p2ImgStance,
            out _p2ImgPunch, out _p2ImgKick, out _p2ImgFlyingPunch, out _p2ImgHadouken, out _p2ImgSpecial, out _p2ImgJump);

    }

    private void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_currentState == GameState.Playing)
        {
            if (e.KeyCode == Keys.Right) _keyRight = true;
            if (e.KeyCode == Keys.Left) _keyLeft = true;
            if (e.KeyCode == Keys.Space && !_isJumping) { _isJumping = true; _jumpVelocity = -32f; }
            if (e.KeyCode == Keys.Q) _keyQ = true;
            if (e.KeyCode == Keys.W)
            {
                _keyW = true;
                if (_selectedCharacter == 9) _p1DinoEvolved = true;
            }
            if (e.KeyCode == Keys.E) _keyE = true;
            if (e.KeyCode == Keys.R) _keyR = true;
            if (e.KeyCode == Keys.T) _keyT = true;
        }
        else if (_currentState == GameState.VictoryScreen)
        {
            if (_victoryTimer > 60)
            {
                _currentState = GameState.MainMenu;
                this.BackgroundImage = _menuBackground;
            }
        }
    }

    private void Form1_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Right) _keyRight = false;
        if (e.KeyCode == Keys.Left) _keyLeft = false;
        if (e.KeyCode == Keys.Q) _keyQ = false;
        if (e.KeyCode == Keys.W) _keyW = false;
        if (e.KeyCode == Keys.E) _keyE = false;
        if (e.KeyCode == Keys.R) _keyR = false;
        if (e.KeyCode == Keys.T) _keyT = false;
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        if (_currentState == GameState.NameEntry)
        {
            if (e.KeyChar == (char)Keys.Enter && !string.IsNullOrWhiteSpace(_playerName)) _currentState = GameState.ModeSelect;
            else if (e.KeyChar == (char)Keys.Back && _playerName.Length > 0) _playerName = _playerName.Substring(0, _playerName.Length - 1);
            else if (char.IsLetterOrDigit(e.KeyChar) || e.KeyChar == ' ') if (_playerName.Length < 12) _playerName += e.KeyChar.ToString().ToUpper();
            this.Invalidate();
        }
    }

    private void Form1_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_currentState != GameState.CharacterSelect) return;
        int startX = 200, startY = 105;
        int cellW = 160, cellH = 140;
        int spacingX = 176, spacingY = 156;
        for (int i = 0; i < 10; i++)
        {
            int row = i / 5;
            int col = i % 5;
            int x = startX + col * spacingX;
            int y = startY + row * spacingY;
            if (new Rectangle(x - 6, y - 6, cellW + 12, cellH + 12).Contains(e.Location))
            {
                if (_csHoverIndex != i) { _csHoverIndex = i; this.Invalidate(); }
                return;
            }
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (_showComingSoonMsg) { _showComingSoonMsg = false; this.Invalidate(); return; }
        if (_currentState == GameState.MainMenu)
        {
            if (new Rectangle(390, 435, 500, 60).Contains(e.Location)) _currentState = GameState.NameEntry;
            else if (new Rectangle(390, 485, 500, 60).Contains(e.Location)) { _showComingSoonMsg = true; _comingSoonTitle = "OPTIONS"; }
            else if (new Rectangle(390, 535, 500, 60).Contains(e.Location)) Application.Exit();
        }
        else if (_currentState == GameState.ModeSelect)
        {
            if (new Rectangle(390, 435, 500, 60).Contains(e.Location)) { _isTrainingMode = false; _selectingP2 = false; _currentState = GameState.CharacterSelect; }
            else if (new Rectangle(390, 485, 500, 60).Contains(e.Location)) { _showComingSoonMsg = true; _comingSoonTitle = "STORY MODE"; }
            else if (new Rectangle(390, 535, 500, 60).Contains(e.Location)) { _isTrainingMode = true; _selectingP2 = false; _currentState = GameState.CharacterSelect; }
        }
        else if (_currentState == GameState.CharacterSelect)
        {
            int cellW = 160, cellH = 140;
            int spacingX = 176, spacingY = 156;
            int startX = 200, startY = 105;

            int clickedId = -1;
            for (int i = 0; i < 10; i++)
            {
                int row = i / 5;
                int col = i % 5;
                Rectangle rect = new Rectangle(startX + col * spacingX - 6, startY + row * spacingY - 6, cellW + 12, cellH + 12);
                if (rect.Contains(e.Location))
                {
                    clickedId = i;
                    break;
                }
            }

            if (clickedId != -1)
            {
                if (!_selectingP2)
                {
                    _selectedCharacter = clickedId;
                    _selectingP2 = true;
                }
                else
                {
                    _p2SelectedCharacter = clickedId;
                    LoadCharacterFrames(_selectedCharacter, _p2SelectedCharacter);
                    _selectingP2 = false;

                    // Move to Stage Selection!
                    _currentState = GameState.StageSelect;
                }
            }
        }
        else if (_currentState == GameState.StageSelect)
        {
            // Detect stage clicks
            int startX = 140, startY = 180;
            int cellW = 320, cellH = 180;
            int spaceX = 340, spaceY = 220;
            for (int i = 0; i < _stages.Count; i++)
            {
                int row = i / 3;
                int col = i % 3;
                Rectangle rect = new Rectangle(startX + col * spaceX, startY + row * spaceY - _stageScrollY, cellW, cellH);
                if (rect.Contains(e.Location))
                {
                    _selectedStageIndex = i;
                    if (_isTrainingMode)
                    {
                        _isTimeInfinite = true;
                        _maxRounds = 99;
                        _p1RoundsWon = 0;
                        _p2RoundsWon = 0;
                        StartMatch();
                    }
                    else
                    {
                        _currentState = GameState.TimeSelect;
                    }
                    break;
                }
            }
        }
        else if (_currentState == GameState.TimeSelect)
        {
            if (new Rectangle(300, 340, 680, 60).Contains(e.Location)) { _isTimeInfinite = false; _currentState = GameState.RoundSelect; }
            else if (new Rectangle(300, 410, 680, 60).Contains(e.Location)) { _isTimeInfinite = true; _currentState = GameState.RoundSelect; }
        }
        else if (_currentState == GameState.RoundSelect)
        {
            if (new Rectangle(300, 340, 680, 60).Contains(e.Location)) { _maxRounds = 3; _p1RoundsWon = 0; _p2RoundsWon = 0; StartMatch(); }
            else if (new Rectangle(300, 410, 680, 60).Contains(e.Location)) { _maxRounds = 5; _p1RoundsWon = 0; _p2RoundsWon = 0; StartMatch(); }
        }
        else if (_currentState == GameState.Playing)
        {
            if (_isPaused)
            {
                if (new Rectangle(340, 360, 600, 50).Contains(e.Location)) { _isPaused = false; }
                else if (new Rectangle(340, 440, 600, 50).Contains(e.Location))
                {
                    _isPaused = false;
                    _currentState = GameState.MainMenu;
                    this.BackgroundImage = _menuBackground;
                }
            }
            if (new Rectangle(610, 10, 60, 30).Contains(e.Location)) { _isPaused = !_isPaused; }
        }
        else if (_currentState == GameState.VictoryScreen)
        {
            if (_victoryTimer > 60)
            {
                _currentState = GameState.MainMenu;
                this.BackgroundImage = _menuBackground;
            }
        }
        this.Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;

        // SCREEN SHAKE TRANSFORM
        if (_currentState == GameState.Playing && _shakeTimer > 0)
        {
            float offsetX = (float)(_rand.NextDouble() * _shakeIntensity * 2 - _shakeIntensity);
            float offsetY = (float)(_rand.NextDouble() * _shakeIntensity * 2 - _shakeIntensity);
            g.TranslateTransform(offsetX, offsetY);
        }

        if (_currentState == GameState.MainMenu || _currentState == GameState.NameEntry || _currentState == GameState.ModeSelect)
        {
            g.Clear(Color.Black);
            foreach (var star in _stars) g.FillRectangle(Brushes.DarkRed, star.X, star.Y, 3, 3);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            g.FillEllipse(Brushes.DarkRed, 890, _bloodDropY, 8, 16);
            g.FillEllipse(Brushes.Red, 892, _bloodDropY + 2, 4, 10);

            using (StringFormat format = new StringFormat() { Alignment = StringAlignment.Center })
            {
                using (Font titleFont = new Font("Impact", 85))
                {
                    g.DrawString("GOON", titleFont, Brushes.DarkRed, 648, 158, format);
                    g.DrawString("WARFARE X", titleFont, Brushes.Gold, 640, 275, format);
                }
                if (_currentState == GameState.MainMenu && _showMenuText)
                {
                    using (Font menuFont = new Font("Courier New", 28, FontStyle.Bold))
                    {
                        g.DrawString("> NEW GAME <", menuFont, Brushes.White, 640, 450, format);
                        g.DrawString("> OPTIONS <", menuFont, Brushes.White, 640, 500, format);
                        g.DrawString("> EXIT <", menuFont, Brushes.White, 640, 550, format);
                    }
                }
                else if (_currentState == GameState.NameEntry)
                {
                    using (Font menuFont = new Font("Courier New", 28, FontStyle.Bold))
                    {
                        g.DrawString("ENTER YOUR NAME:", menuFont, Brushes.Gray, 640, 420, format);
                        g.DrawString(_playerName + (_showCursor ? "_" : ""), menuFont, Brushes.White, 640, 480, format);
                        if (_showMenuText)
                        {
                            using (Font smallFont = new Font("Courier New", 18, FontStyle.Bold))
                                g.DrawString("PRESS ENTER TO CONTINUE", smallFont, Brushes.DarkRed, 640, 560, format);
                        }
                    }
                }
                else if (_currentState == GameState.ModeSelect)
                {
                    using (Font menuFont = new Font("Courier New", 28, FontStyle.Bold))
                    {
                        g.DrawString("> SOLO GAME <", menuFont, Brushes.White, 640, 450, format);
                        g.DrawString("> STORY MODE <", menuFont, Brushes.White, 640, 500, format);
                        g.DrawString("> TRAINING <", menuFont, Brushes.White, 640, 550, format);
                    }
                }
                if (_showComingSoonMsg)
                {
                    Rectangle box = new Rectangle(340, 300, 600, 200);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(230, 10, 10, 10)), box);
                    g.DrawRectangle(new Pen(Color.DarkRed, 6), box);
                    using (Font f = new Font("Impact", 32)) g.DrawString(_comingSoonTitle, f, Brushes.DarkRed, 640, 320, format);
                    using (Font bodyFont = new Font("Courier New", 24, FontStyle.Bold)) g.DrawString("COMING SOON...", bodyFont, Brushes.White, 640, 390, format);
                    using (Font smallFont = new Font("Courier New", 14, FontStyle.Bold)) g.DrawString("(CLICK ANYWHERE TO CLOSE)", smallFont, Brushes.Gray, 640, 460, format);
                }
            }
        }
        else if (_currentState == GameState.StageSelect)
        {
            g.Clear(Color.FromArgb(10, 10, 15));
            using (StringFormat format = new StringFormat() { Alignment = StringAlignment.Center })
            {
                using (Font titleFont = new Font("Impact", 48)) g.DrawString("SELECT STAGEX", titleFont, Brushes.Gold, 640, 50, format);

                int startX = 140, startY = 180;
                int cellW = 320, cellH = 180;
                int spaceX = 340, spaceY = 220;

                // Scrolling container clipping
                g.SetClip(new Rectangle(0, 160, 1280, 520));

                for (int i = 0; i < _stages.Count; i++)
                {
                    int row = i / 3;
                    int col = i % 3;
                    int x = startX + col * spaceX;
                    int y = startY + row * spaceY - _stageScrollY;

                    Rectangle rect = new Rectangle(x, y, cellW, cellH);
                    g.DrawImage(_stages[i], rect);

                    bool isHovered = rect.Contains(this.PointToClient(Cursor.Position));
                    if (isHovered)
                    {
                        g.DrawRectangle(new Pen(Color.Gold, 4), rect);
                        using (Font f = new Font("Courier New", 14, FontStyle.Bold))
                            g.DrawString(GetStageName(i), f, Brushes.White, x + cellW / 2, y + cellH + 5, format);
                    }
                    else
                    {
                        g.DrawRectangle(Pens.Gray, rect);
                    }
                }
                g.ResetClip();
            }
        }
        else if (_currentState == GameState.TimeSelect || _currentState == GameState.RoundSelect)
        {
            g.Clear(Color.Black);
            using (StringFormat format = new StringFormat() { Alignment = StringAlignment.Center })
            {
                if (_currentState == GameState.TimeSelect)
                {
                    using (Font titleFont = new Font("Impact", 60)) g.DrawString("MATCH TIMER", titleFont, Brushes.DarkRed, 640, 150, format);
                    using (Font menuFont = new Font("Courier New", 32, FontStyle.Bold))
                    {
                        g.DrawString("> 99 SECONDS <", menuFont, Brushes.White, 640, 360, format);
                        g.DrawString("> INFINITE ∞ <", menuFont, Brushes.White, 640, 430, format);
                    }
                }
                else
                {
                    using (Font titleFont = new Font("Impact", 60)) g.DrawString("ROUNDS CAP", titleFont, Brushes.DarkRed, 640, 150, format);
                    using (Font menuFont = new Font("Courier New", 32, FontStyle.Bold))
                    {
                        g.DrawString("> BEST OF 3 <", menuFont, Brushes.White, 640, 360, format);
                        g.DrawString("> BEST OF 5 <", menuFont, Brushes.White, 640, 430, format);
                    }
                }
            }
        }
        else if (_currentState == GameState.CharacterSelect)
        {
            // === SF-STYLE CHARACTER SELECT ===

            // Tick pulse & flash
            _csPulse += 0.15f;
            if (_csPulse > Math.PI * 2) _csPulse -= (float)(Math.PI * 2);
            _csFlashTimer++;

            // 1. Dark hatched background
            g.Clear(Color.FromArgb(12, 10, 20));
            using (Pen stripePen = new Pen(Color.FromArgb(25, 255, 200, 0), 2))
            {
                for (int sx = -720; sx < 1280; sx += 30)
                    g.DrawLine(stripePen, sx, 0, sx + 720, 720);
            }

            // 2. Heavy top banner
            using (LinearGradientBrush bannerBrush = new LinearGradientBrush(
                new Rectangle(0, 0, 1280, 80),
                Color.FromArgb(220, 100, 0, 0), Color.FromArgb(220, 30, 0, 60), 0f))
            {
                g.FillRectangle(bannerBrush, 0, 0, 1280, 80);
            }
            g.DrawLine(new Pen(Color.Gold, 3), 0, 80, 1280, 80);

            using (StringFormat centerFmt = new StringFormat { Alignment = StringAlignment.Center })
            {
                // 3. Title / player header
                string heading = _selectingP2 ? "PLAYER 2 — CHOOSE YOUR FIGHTER!" : "PLAYER 1 — CHOOSE YOUR FIGHTER!";
                Color headerColor = _selectingP2 ? Color.DeepSkyBlue : Color.OrangeRed;
                bool flashOn = (_csFlashTimer / 12) % 2 == 0;
                using (Font titleFont = new Font("Impact", 36, FontStyle.Bold))
                {
                    if (flashOn)
                    {
                        // Glow shadow
                        using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb(100, headerColor)))
                            g.DrawString(heading, titleFont, glowBrush, 644, 14, centerFmt);
                        g.DrawString(heading, titleFont, new SolidBrush(headerColor), 640, 12, centerFmt);
                    }
                }

                // 4. Grid of character portraits
                int startX = 200, startY = 105;
                int cellW = 160, cellH = 140;
                int spacingX = 176, spacingY = 156;
                int cols = 5;

                Image?[] portraits = { _imgIdle_Grimace, _imgIdle_Willie, _imgIdle_Ramon, _imgIdle_Arzadown,
                                       _imgIdle_Du30, _imgIdle_Jabee, _imgIdle_Janitor, _imgIdle_Larry,
                                       _imgIdle_Matrix, _imgIdle_Stephen };

                int hovered = _csHoverIndex;

                for (int i = 0; i < 10; i++)
                {
                    int row = i / cols;
                    int col = i % cols;
                    int x = startX + col * spacingX;
                    int y = startY + row * spacingY;

                    bool isP1Selected = !_selectingP2 && _selectedCharacter == i;
                    bool isHovered = hovered == i;
                    bool isP2Locked = _selectingP2 && _p2SelectedCharacter == i;

                    // Cell background plate
                    Color plateColor = isP1Selected ? Color.FromArgb(180, 180, 120, 0)
                                     : isP2Locked ? Color.FromArgb(180, 0, 80, 200)
                                     : isHovered ? Color.FromArgb(120, 60, 60, 60)
                                     : Color.FromArgb(80, 20, 20, 30);
                    using (var plateBrush = new SolidBrush(plateColor))
                        g.FillRectangle(plateBrush, x - 6, y - 6, cellW + 12, cellH + 12);

                    // Portrait image
                    if (portraits[i] != null)
                        g.DrawImage(portraits[i]!, x, y, cellW, cellH);
                    else
                    {
                        // Placeholder silhouette
                        g.FillRectangle(Brushes.DimGray, x, y, cellW, cellH);
                        g.DrawString("?", new Font("Impact", 40), Brushes.White, x + cellW / 2, y + cellH / 2 - 25, centerFmt);
                    }

                    // Glowing border on selected/hovered cell
                    if (isP1Selected || isP2Locked || isHovered)
                    {
                        float glowAlpha = (float)((Math.Sin(_csPulse) + 1f) / 2f); // 0..1
                        Color borderColor = isP1Selected ? Color.FromArgb((int)(160 + 95 * glowAlpha), Color.Gold)
                                          : isP2Locked ? Color.FromArgb((int)(160 + 95 * glowAlpha), Color.DeepSkyBlue)
                                          : Color.FromArgb((int)(80 + 80 * glowAlpha), Color.White);
                        int bw = isP1Selected || isP2Locked ? 4 : 2;
                        using (Pen borderPen = new Pen(borderColor, bw))
                            g.DrawRectangle(borderPen, x - 6, y - 6, cellW + 12, cellH + 12);

                        // Corner sparks
                        if (isP1Selected || isP2Locked)
                        {
                            using (SolidBrush sparkBrush = new SolidBrush(Color.FromArgb((int)(200 * glowAlpha), Color.White)))
                            {
                                g.FillEllipse(sparkBrush, x - 10, y - 10, 8, 8);
                                g.FillEllipse(sparkBrush, x + cellW + 4, y - 10, 8, 8);
                                g.FillEllipse(sparkBrush, x - 10, y + cellH + 4, 8, 8);
                                g.FillEllipse(sparkBrush, x + cellW + 4, y + cellH + 4, 8, 8);
                            }
                        }
                    }

                    // Character name label below portrait
                    Brush nameBrush = (isP1Selected || isP2Locked) ? Brushes.Gold : Brushes.Gray;
                    using (Font nameFont = new Font("Courier New", 10, FontStyle.Bold))
                        g.DrawString(GetCharacterName(i), nameFont, nameBrush, x + cellW / 2, y + cellH + 2, centerFmt);
                }

                // 5. Bottom VS. matchup bar
                int barY = 530;
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 5, 5, 5)), 0, barY, 1280, 190);
                g.DrawLine(new Pen(Color.Gold, 2), 0, barY, 1280, barY);

                // P1 portrait preview (left side)
                Image? p1Prev = _selectedCharacter switch
                {
                    0 => _imgIdle_Grimace,
                    1 => _imgIdle_Willie,
                    2 => _imgIdle_Ramon,
                    3 => _imgIdle_Arzadown,
                    4 => _imgIdle_Du30,
                    5 => _imgIdle_Jabee,
                    6 => _imgIdle_Janitor,
                    7 => _imgIdle_Larry,
                    8 => _imgIdle_Matrix,
                    9 => _imgIdle_Stephen,
                    _ => null
                };

                // P2 / hover preview (right side)
                int p2PreviewId = _selectingP2 ? _csHoverIndex : _p2SelectedCharacter;
                Image? p2Prev = p2PreviewId switch
                {
                    0 => _imgIdle_Grimace,
                    1 => _imgIdle_Willie,
                    2 => _imgIdle_Ramon,
                    3 => _imgIdle_Arzadown,
                    4 => _imgIdle_Du30,
                    5 => _imgIdle_Jabee,
                    6 => _imgIdle_Janitor,
                    7 => _imgIdle_Larry,
                    8 => _imgIdle_Matrix,
                    9 => _imgIdle_Stephen,
                    _ => null
                };

                // P1 side
                if (p1Prev != null)
                    g.DrawImage(p1Prev, 30, barY + 10, 150, 165);
                using (Font pNameFont = new Font("Impact", 22, FontStyle.Bold))
                {
                    string p1Label = !_selectingP2 ? "P1 SELECT..." : GetCharacterName(_selectedCharacter).ToUpper();
                    g.DrawString(p1Label, pNameFont, Brushes.OrangeRed, 200, barY + 20);
                    if (_selectingP2)
                    {
                        using (Font small = new Font("Impact", 14)) g.DrawString("PLAYER 1", small, Brushes.Gold, 200, barY + 55);
                    }
                }

                // VS.
                float vsGlow = (float)((Math.Sin(_csPulse * 1.5f) + 1f) / 2f);
                using (Font vsFont = new Font("Impact", 52, FontStyle.Bold))
                using (SolidBrush vsBrush = new SolidBrush(Color.FromArgb((int)(180 + 75 * vsGlow), Color.Gold)))
                    g.DrawString("VS.", vsFont, vsBrush, 640, barY + 50, centerFmt);

                // P2 side
                if (p2Prev != null)
                {
                    // Flip P2 so they face left (mirror)
                    g.TranslateTransform(1280, 0);
                    g.ScaleTransform(-1, 1);
                    g.DrawImage(p2Prev, 30, barY + 10, 150, 165);
                    g.ScaleTransform(-1, 1);
                    g.TranslateTransform(-1280, 0);
                }
                using (Font pNameFont = new Font("Impact", 22, FontStyle.Bold))
                using (StringFormat rightFmt = new StringFormat { Alignment = StringAlignment.Far })
                {
                    string p2Label = _selectingP2 ? "P2 SELECT..." : GetCharacterName(_p2SelectedCharacter).ToUpper();
                    g.DrawString(p2Label, pNameFont, Brushes.DeepSkyBlue, 1080, barY + 20, rightFmt);
                    if (!_selectingP2)
                    {
                        using (Font small = new Font("Impact", 14)) g.DrawString("PLAYER 2", small, Brushes.Gold, 1080, barY + 55, rightFmt);
                    }
                }

                // 6. Instruction blurb at very bottom
                using (Font instrFont = new Font("Courier New", 12, FontStyle.Bold))
                    g.DrawString("CLICK A PORTRAIT TO SELECT YOUR FIGHTER", instrFont, Brushes.DarkGray, 640, 700, centerFmt);
            }
        }
        else if (_currentState == GameState.Playing)
        {
            // DRAW WAR ZONE OVERLAY FIRST
            if (_isWarZone)
            {
                g.FillRectangle(_warBrushOverlay, 0, 0, 1280, 720); // Cached brush to prevent lag!

                // Embers
                foreach (var ember in _emberParticles)
                {
                    int alpha = (int)(ember.Life * 2.5f);
                    if (alpha > 200) alpha = 200;
                    if (alpha < 0) alpha = 0;
                    using (var b = new SolidBrush(Color.FromArgb(alpha, ember.Color)))
                        g.FillRectangle(b, ember.X, ember.Y, 5, 5);
                }
            }

            // DYNAMIC BACKGROUND EFFECTS (Dust / Sparks) based on Stage Index!
            for (int i = 0; i < _stageDust.Length; i++)
            {
                // Different effects based on current stage color scheme
                Brush pBrush = (_currentStageIndex % 2 == 0) ? Brushes.Gold : Brushes.LightCyan;
                int pSize = (i % 3) + 2; // size varies 2 to 4
                // Draw slight glow
                using (SolidBrush glow = new SolidBrush(Color.FromArgb(50, (_currentStageIndex % 2 == 0) ? Color.Gold : Color.White)))
                    g.FillEllipse(glow, _stageDust[i].X - 2, _stageDust[i].Y - 2, pSize + 4, pSize + 4);
                g.FillEllipse(pBrush, _stageDust[i].X, _stageDust[i].Y, pSize, pSize);
            }

            // Use NearestNeighbor for pixel perfect character rendering (prevents lagg/blur)
            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            // Draw Ground Shadow under Player 1 to anchor him to the stage!
            g.FillEllipse(new SolidBrush(Color.FromArgb(120, 0, 0, 0)), p1X + 20, p1Y + 190, 180, 30);

            // Matrix, Ramon, Larry and Du30 use stance as default idle; others use idle tile
            Image? currentSprite = (_selectedCharacter == 8 || _selectedCharacter == 2 || _selectedCharacter == 7 || _selectedCharacter == 4) ? _imgStance : _imgIdle;

            if (_selectedCharacter == 9 && _p1DinoEvolved) currentSprite = _imgKick; // Base evolved state (Tile 1)

            if (_currentAttack == "Q") currentSprite = _imgPunch;
            else if (_currentAttack == "W") currentSprite = _imgKick;
            else if (_currentAttack == "E") currentSprite = _imgFlyingPunch;
            else if (_currentAttack == "R") currentSprite = _imgHadouken;
            else if (_currentAttack == "T") currentSprite = _imgSpecial ?? _imgFlyingPunch;
            else if (_isJumping) currentSprite = (_imgJump != null) ? _imgJump : _imgStance;
            else if (_keyLeft || _keyRight) currentSprite = (_tickAccumulator / 4) % 2 == 0 ? _imgWalk : (_imgStance ?? _imgIdle); // Bouncing Walk cycle

            if (_p1HurtTimer > 0) currentSprite = _imgJump ?? _imgKick ?? _imgIdle;

            if (currentSprite != null)
            {
                // Bottom-Anchoring logic: perfectly scale based on idle frame, and ground feet
                float scaleFactor = (_selectedCharacter == 4) ? (195f / _imgIdle!.Height) : (220f / _imgIdle!.Height);
                int drawHeight = (int)(currentSprite.Height * scaleFactor);
                int drawWidth = (int)(currentSprite.Width * scaleFactor);

                int shiftX = p1X + ((220 - drawWidth) / 2); // Center X
                // Raised anchor to 205 to perfectly place Sprite feet atop the generated Shadow (fixes Grimace sinking!)
                int drawY = (p1Y + 205) - drawHeight;

                if (_p1HurtTimer > 0)
                {
                    shiftX += (_p1HurtTimer % 4 < 2) ? -15 : 15; // Face/body shake!
                }

                Rectangle src = new Rectangle(0, 0, currentSprite.Width, currentSprite.Height);
                if (_facingLeft) 
                {
                    Rectangle dest = new Rectangle(shiftX + drawWidth, drawY, -drawWidth, drawHeight);
                    g.DrawImage(currentSprite, dest, src, GraphicsUnit.Pixel);
                }
                else 
                {
                    Rectangle dest = new Rectangle(shiftX, drawY, drawWidth, drawHeight);
                    g.DrawImage(currentSprite, dest, src, GraphicsUnit.Pixel);
                }
            }

            // Player 2 sprite (facing left) - moves dynamically with _p2X
            Image? p2Sprite = _p2ImgIdle;
            if (_p2SelectedCharacter == 9 && _p2DinoEvolved) p2Sprite = _p2ImgKick; // P2 evolved state

            if (_p2CurrentAttack == "Q") p2Sprite = _p2ImgPunch;
            else if (_p2CurrentAttack == "W") { p2Sprite = _p2ImgKick; if (_p2SelectedCharacter == 9) _p2DinoEvolved = true; }
            else if (_p2CurrentAttack == "E") p2Sprite = _p2ImgFlyingPunch;
            else if (_p2Action == "WALK") p2Sprite = (_tickAccumulator / 4) % 2 == 0 ? _p2ImgWalk : (_p2ImgStance ?? _p2ImgIdle);

            if (_p2HurtTimer > 0) p2Sprite = _p2ImgJump ?? _p2ImgKick ?? _p2ImgIdle;

            g.FillEllipse(new SolidBrush(Color.FromArgb(120, 0, 0, 0)), _p2X - 20, 450 + 190, 130, 30);
            if (p2Sprite != null)
            {
                float scaleFactor = (_p2SelectedCharacter == 4) ? (195f / _p2ImgIdle!.Height) : (220f / _p2ImgIdle!.Height);
                int drawHeight = (int)(p2Sprite.Height * scaleFactor);
                int drawWidth = (int)(p2Sprite.Width * scaleFactor);

                int shiftX = _p2X;
                if (_p2HurtTimer > 0)
                {
                    shiftX += (_p2HurtTimer % 4 < 2) ? -15 : 15; // Face/body shake!
                }

                int drawY = (450 + 205) - drawHeight; // Anchor Bottom onto Shadow perfectly

                Rectangle src2 = new Rectangle(0, 0, p2Sprite.Width, p2Sprite.Height);
                Rectangle dest2 = new Rectangle(shiftX + drawWidth + ((220 - drawWidth) / 2), drawY, -drawWidth, drawHeight);
                // Always face left (mirror)
                g.DrawImage(p2Sprite, dest2, src2, GraphicsUnit.Pixel);
            }
            else
            {
                g.FillRectangle(Brushes.Red, _p2X, 450, 110, 220);
            }

            g.InterpolationMode = InterpolationMode.Default; // Reset for UI elements


            // P1 HADOUKEN PROJECTILE (drawn as purple energy ball or custom image)
            if (_projActive)
            {
                if (_selectedCharacter == 5 && _jabeeProjectile != null)
                {
                    int pw = 110;
                    int ph = 110;

                    float pulse = (float)(Math.Sin(_blinkCounter * 0.8f) + 1f) / 2f;

                    // Draw the red horizontal speed lines behind the bucket!
                    using (Pen speedPen = new Pen(Color.OrangeRed, 4))
                    {
                        for (int lineY = -20; lineY <= 40; lineY += 18)
                        {
                            int startX = (int)_projX + (_facingLeft ? pw + 20 : -20);
                            int trailLen = 60 + (int)(pulse * 30) - Math.Abs(lineY);
                            int endX = startX + (_facingLeft ? trailLen : -trailLen);
                            g.DrawLine(speedPen, startX, (int)_projY + 40 + lineY, endX, (int)_projY + 40 + lineY);
                        }
                    }

                    // Spiky Fire Aura (Outer)
                    int cx = (int)_projX + pw / 2;
                    int cy = (int)_projY + ph / 2 - 15;
                    for (int i = 0; i < 12; i++)
                    {
                        float angle = i * (float)Math.PI / 6f + pulse;
                        int spikeLen = 80 + (i % 2 == 0 ? 10 : -10);
                        Point[] spike = {
                            new Point(cx + (int)(Math.Cos(angle - 0.2) * 40), cy + (int)(Math.Sin(angle - 0.2) * 40)),
                            new Point(cx + (int)(Math.Cos(angle) * spikeLen), cy + (int)(Math.Sin(angle) * spikeLen)),
                            new Point(cx + (int)(Math.Cos(angle + 0.2) * 40), cy + (int)(Math.Sin(angle + 0.2) * 40))
                        };
                        g.FillPolygon(Brushes.OrangeRed, spike);
                        g.FillPolygon(Brushes.Gold, new Point[] {
                            new Point(cx + (int)(Math.Cos(angle - 0.1) * 30), cy + (int)(Math.Sin(angle - 0.1) * 30)),
                            new Point(cx + (int)(Math.Cos(angle) * (spikeLen - 20)), cy + (int)(Math.Sin(angle) * (spikeLen - 20))),
                            new Point(cx + (int)(Math.Cos(angle + 0.1) * 30), cy + (int)(Math.Sin(angle + 0.1) * 30))
                        });
                    }

                    // Inner glowing core
                    for (int i = 0; i < 3; i++)
                    {
                        using (var glow = new SolidBrush(Color.FromArgb((int)(120 - i * 20), Color.DarkOrange)))
                            g.FillEllipse(glow, (int)_projX - 10 + i * 10, (int)_projY - 20 + i * 8, pw + 20 - i * 20, ph + 20 - i * 20);
                    }

                    if (_facingLeft)
                    {
                        g.DrawImage(_jabeeProjectile, new Rectangle((int)_projX + pw, (int)_projY - 15, -pw, ph), new Rectangle(0, 0, _jabeeProjectile.Width, _jabeeProjectile.Height), GraphicsUnit.Pixel);
                    }
                    else
                    {
                        g.DrawImage(_jabeeProjectile, (int)_projX, (int)_projY - 15, pw, ph);
                    }
                }
                else if (_selectedCharacter == 4) // DU30 - CLASSIC BOMB WITH FUSE
                {
                    float pulse = (float)(Math.Sin(_blinkCounter * 0.6f) + 1f) / 2f;
                    float pulse2 = (float)(Math.Sin(_blinkCounter * 1.2f) + 1f) / 2f;
                    int cx = (int)_projX + 25; // center of bomb
                    int cy = (int)_projY;

                    // === FIRE TRAIL ===
                    int trailDir = _projDir < 0 ? 1 : -1;
                    for (int t = 1; t <= 4; t++)
                    {
                        int tx = cx + trailDir * t * 22;
                        int alpha = (int)(160 - t * 35);
                        int sz = (int)(40 - t * 7);
                        using (var trailBrush = new SolidBrush(Color.FromArgb(alpha, t % 2 == 0 ? Color.OrangeRed : Color.Gold)))
                            g.FillEllipse(trailBrush, tx - sz / 2, cy - sz / 2, sz, sz);
                    }

                    // === BOMB BODY (dark sphere) ===
                    using (var bombBrush = new SolidBrush(Color.FromArgb(240, 20, 20, 20)))
                        g.FillEllipse(bombBrush, cx - 28, cy - 28, 56, 56);
                    // Shine highlight
                    using (var shineBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
                        g.FillEllipse(shineBrush, cx - 16, cy - 20, 18, 12);
                    // Outline
                    using (var outlinePen = new Pen(Color.FromArgb(200, 60, 60, 60), 3))
                        g.DrawEllipse(outlinePen, cx - 28, cy - 28, 56, 56);

                    // === FUSE (curved line from top of bomb) ===
                    using (var fusePen = new Pen(Color.SaddleBrown, 3))
                    {
                        g.DrawBezier(fusePen,
                            new Point(cx, cy - 28),           // Start at top of bomb
                            new Point(cx + 8, cy - 48),       // Control point 1
                            new Point(cx - 5, cy - 60),       // Control point 2
                            new Point(cx + 4, cy - 72));      // Fuse tip
                    }

                    // === SPARK AT FUSE TIP (animated) ===
                    int sparkX = cx + 4;
                    int sparkY = cy - 72;
                    int sparkSize = (int)(10 + 8 * pulse2);
                    // Spark glow
                    using (var sparkGlow = new SolidBrush(Color.FromArgb(80, Color.Yellow)))
                        g.FillEllipse(sparkGlow, sparkX - sparkSize, sparkY - sparkSize, sparkSize * 2, sparkSize * 2);
                    // Spark core
                    using (var sparkBrush = new SolidBrush(Color.FromArgb(255, pulse2 > 0.5f ? Color.White : Color.Yellow)))
                        g.FillEllipse(sparkBrush, sparkX - 5, sparkY - 5, 10, 10);
                    // Spark rays
                    using (var rayPen = new Pen(Color.FromArgb((int)(200 * pulse2), Color.OrangeRed), 2))
                    {
                        for (int ray = 0; ray < 6; ray++)
                        {
                            double a = ray * Math.PI / 3;
                            int rLen = (int)(6 + 6 * pulse2);
                            g.DrawLine(rayPen, sparkX, sparkY,
                                sparkX + (int)(Math.Cos(a) * rLen),
                                sparkY + (int)(Math.Sin(a) * rLen));
                        }
                    }

                    // === "BOMB" TEXT label ===
                    using (var sfmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var bombFont = new Font("Impact", 14, FontStyle.Bold))
                    using (var textBrush = new SolidBrush(Color.FromArgb(255, Color.OrangeRed)))
                    {
                        g.DrawString("BOOM!", bombFont, Brushes.Black, cx + 2, cy + 38, sfmt);
                        g.DrawString("BOOM!", bombFont, textBrush, cx, cy + 36, sfmt);
                    }
                }
                else if (_selectedCharacter == 7) // LARRY GADON - BOBO NUCLEAR BOMB
                {
                    float pulse = (float)(Math.Sin(_blinkCounter * 0.5f) + 1f) / 2f;  // 0-1 slow pulse
                    float pulse2 = (float)(Math.Sin(_blinkCounter * 0.9f) + 1f) / 2f;  // faster inner pulse
                    int cx = (int)_projX;
                    int cy = (int)_projY;

                    // === FIRE TRAIL (streaks behind the projectile) ===
                    int trailDir = _projDir < 0 ? 1 : -1; // opposite to travel direction
                    for (int t = 1; t <= 5; t++)
                    {
                        int tx = cx + trailDir * t * 28;
                        int alpha = (int)(180 - t * 30);
                        int sz = (int)(90 - t * 12);
                        using (var trailBrush = new SolidBrush(Color.FromArgb(alpha, t % 2 == 0 ? Color.OrangeRed : Color.Yellow)))
                            g.FillEllipse(trailBrush, tx - sz / 2, cy - sz / 2, sz, sz);
                    }

                    // === MUSHROOM CLOUD / NUKE AURA (giant outer glow) ===
                    for (int r = 5; r >= 1; r--)
                    {
                        int auraSize = (int)(180 + r * 28 + 30 * pulse);
                        int auraAlpha = (int)(15 + r * 8);
                        Color auraColor = r % 2 == 0 ? Color.OrangeRed : Color.Yellow;
                        using (var auraBrush = new SolidBrush(Color.FromArgb(auraAlpha, auraColor)))
                            g.FillEllipse(auraBrush, cx - auraSize / 2, cy - auraSize / 2, auraSize, auraSize);
                    }

                    // === 5 CONCENTRIC SHOCKWAVE RINGS ===
                    int[] ringSizes = { 220, 170, 130, 90, 55 };
                    int[] ringWidths = { 6, 5, 4, 3, 2 };
                    Color[] ringColors = { Color.OrangeRed, Color.Orange, Color.Yellow, Color.White, Color.Red };
                    for (int i = 0; i < 5; i++)
                    {
                        float ph = (float)(Math.Sin(_blinkCounter * 0.5f + i * 0.7f) + 1f) / 2f;
                        int rs = (int)(ringSizes[i] + 20 * ph);
                        int ra = (int)(120 + 135 * ph);
                        using (var rp = new Pen(Color.FromArgb(ra, ringColors[i]), ringWidths[i]))
                            g.DrawEllipse(rp, cx - rs / 2, cy - rs / 2, rs, rs);
                    }

                    // === WHITE-HOT INNER CORE ===
                    int core = (int)(60 + 20 * pulse2);
                    using (var coreBrush = new SolidBrush(Color.FromArgb(220, Color.White)))
                        g.FillEllipse(coreBrush, cx - core / 2, cy - core / 2, core, core);
                    using (var innerBrush = new SolidBrush(Color.FromArgb(255, Color.Yellow)))
                        g.FillEllipse(innerBrush, cx - 20, cy - 15, 40, 30);

                    // === "BOBO" — MASSIVE NUCLEAR TEXT ===
                    using (var sfmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        // Outer glow shadow layers
                        for (int shadow = 6; shadow >= 1; shadow--)
                        {
                            int sAlpha = (int)(60 + shadow * 20 * pulse);
                            using (var shadowFont = new Font("Impact", 120, FontStyle.Bold))
                            using (var shadowBrush = new SolidBrush(Color.FromArgb(sAlpha, Color.DarkRed)))
                                g.DrawString("BOBO", shadowFont, shadowBrush, cx + shadow * 3, cy + shadow * 3, sfmt);
                        }
                        // Main BOBO text — bright red, 120pt Impact
                        using (var boboFont = new Font("Impact", 120, FontStyle.Bold))
                        {
                            using (var outlineBrush = new SolidBrush(Color.FromArgb(255, Color.Black)))
                                g.DrawString("BOBO", boboFont, outlineBrush, cx + 4, cy + 4, sfmt);
                            using (var textBrush = new SolidBrush(Color.FromArgb(255, Color.Red)))
                                g.DrawString("BOBO", boboFont, textBrush, cx, cy, sfmt);
                            // Bright pulse overlay
                            using (var flashBrush = new SolidBrush(Color.FromArgb((int)(120 * pulse2), Color.White)))
                                g.DrawString("BOBO", boboFont, flashBrush, cx, cy, sfmt);
                        }
                    }
                }
                else if (_selectedCharacter == 6) // JANITOR - MASSIVE MCDONALDS HADOUKEN
                {
                    int s = 140; // Same size as massive normal Hadouken
                    int cx = (int)_projX;
                    int cy = (int)_projY - s / 2;
                    float pulse = (float)(Math.Sin(_blinkCounter * 0.8f) + 1f) / 2f;

                    // === The "Big Hadouken" Base (Fire energy ball) ===
                    for (int i = 0; i < 4; i++)
                    {
                        using (var glow = new SolidBrush(Color.FromArgb((int)(100 - i * 20 + 20 * pulse), Color.OrangeRed)))
                            g.FillEllipse(glow, cx - 20 + i * 10 + (_facingLeft ? 20 : -20), cy - 10 + i * 10, s + 40 - i * 20, s * 2 / 3 + 40 - i * 20);
                    }

                    Color projColor = Color.FromArgb(200, 255, 60, 0); // Deep Orange/Red
                    g.FillEllipse(new SolidBrush(projColor), cx, cy, s, s * 2 / 3);
                    g.DrawEllipse(new Pen(Color.FromArgb(200, Color.Yellow), 4), cx + 10, cy + 10, s - 20, s * 2 / 3 - 20);

                    // === The "Fire M Design like McDonalds" embedded inside ===
                    using (var sfmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (var mcFont = new Font("Arial Rounded MT Bold", 80, FontStyle.Regular)) // Scaled to fit perfectly inside
                    {
                        int textX = cx + s / 2;
                        int textY = cy + (s * 2 / 3) / 2;

                        // Drop shadow
                        using (var shadowBrush = new SolidBrush(Color.FromArgb(150, Color.DarkRed)))
                            g.DrawString("M", mcFont, shadowBrush, textX + 6, textY + 6, sfmt);

                        // Fire inner glow offset for the M
                        using (var textBrush = new SolidBrush(Color.FromArgb(255, Color.Orange)))
                            g.DrawString("M", mcFont, textBrush, textX + 3, textY + 3, sfmt);

                        // Main Golden M inside the fiery Hadouken
                        g.DrawString("M", mcFont, Brushes.Gold, textX, textY, sfmt);

                        // White hot core pulse on the text
                        using (var coreBrush = new SolidBrush(Color.FromArgb((int)(150 * pulse), Color.White)))
                            g.DrawString("M", mcFont, coreBrush, textX - 1, textY - 1, sfmt);
                    }
                }
                else if (_selectedCharacter == 0) // GRIMACE - BIG VIOLET SLURPEE
                {
                    int cx = (int)_projX + 40;
                    int cy = (int)_projY - 60;
                    float pulse = (float)(Math.Sin(_blinkCounter * 0.8f) + 1f) / 2f;

                    // Slurpee Purple Energy Splash
                    for (int i = 0; i < 5; i++)
                    {
                        using (var glow = new SolidBrush(Color.FromArgb((int)(70 - i * 10 + 15 * pulse), Color.DarkViolet)))
                            g.FillEllipse(glow, cx - 60 + i * 15 + (_facingLeft ? 40 : -40), cy - 30 + i * 8, 160 - i * 20, 130 - i * 15);
                    }

                    // Draw Cup (Trapezoid) leaning slightly
                    Point[] cup = {
                        new Point(cx, cy + 80), new Point(cx + 60, cy + 80),
                        new Point(cx + 80, cy), new Point(cx - 20, cy)
                    };
                    using (var cupBrush = new SolidBrush(Color.FromArgb(220, 138, 43, 226))) // Violet Cup
                        g.FillPolygon(cupBrush, cup);
                    g.DrawPolygon(new Pen(Color.FromArgb(200, Color.White), 4), cup);

                    // Draw Classic Dome Lid
                    g.FillEllipse(new SolidBrush(Color.FromArgb(140, 255, 255, 255)), cx - 20, cy - 30, 100, 60);
                    g.DrawEllipse(new Pen(Color.White, 4), cx - 20, cy - 30, 100, 60);

                    // Draw Thick Red Straw poking out of dome
                    g.DrawLine(new Pen(Color.Red, 8), cx + 30, cy - 10, cx + (_facingLeft ? -10 : 70), cy - 70);
                }
                else if (_selectedCharacter == 1) // WILLIE - FLYING BUNDLES OF CASH
                {
                    int cx = (int)_projX + 20;
                    int cy = (int)_projY - 30;
                    float pulse = (float)(Math.Sin(_blinkCounter * 0.8f) + 1f) / 2f;

                    // Golden energy trail
                    for (int i = 0; i < 4; i++)
                    {
                        using (var glow = new SolidBrush(Color.FromArgb((int)(80 - i * 15 + 20 * pulse), Color.Gold)))
                            g.FillEllipse(glow, cx - 40 + i * 20 + (_facingLeft ? 40 : -40), cy - 20 + i * 10, 160 - i * 20, 130 - i * 20);
                    }

                    // Stack of Cash
                    using (var billBrush = new SolidBrush(Color.FromArgb(255, 133, 187, 101))) // Cash Green
                    using (var edgePen = new Pen(Color.DarkGreen, 3))
                    using (var bandBrush = new SolidBrush(Color.Goldenrod))
                    {
                        for (int s = 0; s < 3; s++) // 3 thick stacks
                        {
                            int stackX = cx + (s * -10);
                            int stackY = cy - (s * 15);
                            Rectangle billRect = new Rectangle(stackX, stackY, 110, 55);
                            g.FillRectangle(billBrush, billRect);
                            g.DrawRectangle(edgePen, billRect);

                            // Paper band around the cash
                            g.FillRectangle(bandBrush, stackX + 45, stackY, 20, 55);
                            g.DrawRectangle(Pens.Gold, stackX + 45, stackY, 20, 55);

                            // Value Imprint
                            using (Font cashFont = new Font("Impact", 18, FontStyle.Italic))
                                g.DrawString("10K", cashFont, Brushes.DarkGreen, stackX + 5, stackY + 12);
                        }
                    }

                    // Loose trailing bills flying off behind the stack!
                    for (int k = 0; k < 5; k++)
                    {
                        int driftX = cx + (_facingLeft ? 80 + k * 40 : -40 - k * 40);
                        int driftY = cy - 20 + (int)(Math.Sin(_blinkCounter + k) * 50);
                        g.FillRectangle(Brushes.LightGreen, driftX, driftY, 40, 20);
                        g.DrawRectangle(Pens.DarkGreen, driftX, driftY, 40, 20);
                        using (Font miniFont = new Font("Arial", 8, FontStyle.Bold))
                            g.DrawString("$", miniFont, Brushes.DarkGreen, driftX + 12, driftY + 3);
                    }
                }
                else
                {
                    Color projColor = Color.FromArgb(180, 148, 0, 211); // Purple default
                    if (_selectedCharacter == 2) projColor = Color.FromArgb(180, 50, 200, 50); // Ramon Green
                    else if (_selectedCharacter == 8) projColor = Color.FromArgb(180, 0, 255, 0); // Matrix Green
                    else if (_selectedCharacter == 9) projColor = Color.FromArgb(180, 255, 50, 50); // Dino Red

                    int s = 140; // MASSIVE normal Hadouken
                    g.FillEllipse(new SolidBrush(projColor), (int)_projX, (int)_projY - s / 2, s, s * 2 / 3);
                    g.DrawEllipse(new Pen(Color.White, 5), (int)_projX + 15, (int)_projY - s / 2 + 15, s - 30, s * 2 / 3 - 30);
                }
            }

            // P2 PROJECTILE
            if (_p2ProjActive)
            {
                int s = 140;
                g.FillEllipse(new SolidBrush(Color.FromArgb(180, 255, 60, 20)), _p2ProjX, _p2ProjY - s / 2, s, s * 2 / 3);
                g.DrawEllipse(new Pen(Color.White, 5), _p2ProjX + 15, _p2ProjY - s / 2 + 15, s - 30, s * 2 / 3 - 30);
            }

            // CASTING AURA (Flashing ring on Grimace's hands when throwing Hadouken)
            if (_currentAttack == "R" && _attackTimer > 2)
            {
                int castX = p1X + (_facingLeft ? -40 : 180);
                g.DrawEllipse(new Pen(Color.MediumPurple, 6), castX, p1Y + 100, 60, 60);
                g.DrawEllipse(new Pen(Color.White, 3), castX + 10, p1Y + 110, 40, 40);
            }

            // EXPLOSIVE IMPACT SPARK ON HIT
            if (_impactTimer > 0)
            {
                g.FillEllipse(Brushes.Yellow, _impactX - 40, _impactY - 40, 80, 80);
                g.FillEllipse(Brushes.White, _impactX - 20, _impactY - 20, 40, 40);
                // Draw some explosive spikes
                g.DrawLine(new Pen(Color.Orange, 4), _impactX - 60, _impactY, _impactX + 60, _impactY);
                g.DrawLine(new Pen(Color.Orange, 4), _impactX, _impactY - 60, _impactX, _impactY + 60);
                g.DrawLine(new Pen(Color.Orange, 4), _impactX - 40, _impactY - 40, _impactX + 40, _impactY + 40);
                g.DrawLine(new Pen(Color.Orange, 4), _impactX - 40, _impactY + 40, _impactX + 40, _impactY - 40);
            }

            // DRAW WRECKAGE PARTICLES
            foreach (var p in _wreckParticles)
            {
                using (var b = new SolidBrush(p.Color))
                    g.FillRectangle(b, p.X, p.Y, 8, 8);
            }

            // DRAW FULL-SCREEN FLASH OVERLAY
            if (_flashTimer > 0)
            {
                float alphaPct = (float)_flashTimer / 25f; // rough max flash duration scaling
                int alpha = (int)(Math.Min(120, _flashColor.A * alphaPct));
                using (var flashBrush = new SolidBrush(Color.FromArgb(alpha, _flashColor)))
                {
                    g.ResetTransform(); // Flash covers everything including HUD
                    g.FillRectangle(flashBrush, 0, 0, 1280, 720);

                    // Announcement for Demolition
                    if (_flashTimer > 5)
                    {
                        using (Font announceFont = new Font("Impact", 80, FontStyle.Bold))
                        using (StringFormat sfmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            g.DrawString("STAGE DEMOLISHED!", announceFont, Brushes.Black, 642, 362, sfmt);
                            g.DrawString("STAGE DEMOLISHED!", announceFont, Brushes.Gold, 640, 360, sfmt);
                        }
                    }
                }
            }

            DrawArcadeUI(g);
        }
        else if (_currentState == GameState.VictoryScreen)
        {
            _victoryTimer++;
            
            // Draw dimmed background stage underneath
            if (_stages.Count > _currentStageIndex)
                g.DrawImage(_stages[_currentStageIndex], 0, 0, 1280, 720);
            
            using (SolidBrush dark = new SolidBrush(Color.FromArgb(Math.Min(220, _victoryTimer * 2), 0, 0, 0)))
                g.FillRectangle(dark, 0, 0, 1280, 720);

            // Fetch winner identity
            int winnerId = _matchWinner == 0 ? _selectedCharacter : _p2SelectedCharacter;
            string champName = GetCharacterName(winnerId);
            
            // Default to stance pose (very dynamic)
            Image? champSprite = _matchWinner == 0 ? (_imgStance ?? _imgIdle) : (_p2ImgStance ?? _p2ImgIdle);
            
            if (champSprite != null)
            {
                // Cinematic Slide-In from Edge
                float destX = 640;
                if (_victoryTimer < 80)
                {
                    float t = _victoryTimer / 80f; // 0 to 1
                    float easeOut = (float)Math.Sin(t * Math.PI / 2); // Quadratic easing out
                    
                    if (_matchWinner == 0) // P1 slides from left
                        destX = -300 + (640 - -300) * easeOut;
                    else // P2 slides from right
                        destX = 1580 - (1580 - 640) * easeOut;
                }

                float scale = 4.5f; // MASSIVE TEKKEN ZOOM IN!
                if (winnerId == 4) scale = 5.2f; // Scale bump for Du30 since his sprite is naturally smaller
                
                int drawW = (int)(champSprite.Width * scale);
                int drawH = (int)(champSprite.Height * scale);
                
                int drawX = (int)destX - drawW / 2;
                int drawY = 720 - drawH + 100; // Pin bottom (lower than screen bounds so it crops perfectly)

                Rectangle src = new Rectangle(0, 0, champSprite.Width, champSprite.Height);
                g.InterpolationMode = InterpolationMode.NearestNeighbor; // Keep pixel perfection!
                if (_matchWinner == 1)
                {
                    // P2 is mirrored normally, flip it back so they always look glorious facing center
                    g.DrawImage(champSprite, new Rectangle(drawX + drawW, drawY, -drawW, drawH), src, GraphicsUnit.Pixel);
                }
                else
                {
                    g.DrawImage(champSprite, new Rectangle(drawX, drawY, drawW, drawH), src, GraphicsUnit.Pixel);
                }
                g.InterpolationMode = InterpolationMode.Default;
            }

            // Draw Epic Bold Win Sequence Text
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center })
            {
                if (_victoryTimer > 40)
                {
                    int alpha = Math.Min(255, (_victoryTimer - 40) * 8);
                    
                    // Shadow glow
                    using (SolidBrush textShadow = new SolidBrush(Color.FromArgb(alpha, Color.DarkRed)))
                    using (Font champFont = new Font("Impact", 96, FontStyle.Italic))
                        g.DrawString("CHAMPION", champFont, textShadow, 640, 60, format);
                        
                    using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(alpha, Color.Gold)))
                    using (Font champFont = new Font("Impact", 90, FontStyle.Italic))
                        g.DrawString("CHAMPION", champFont, textBrush, 640, 60, format);
                    
                    using (SolidBrush nameBrush = new SolidBrush(Color.FromArgb(alpha, Color.White)))
                    using (Font nameFont = new Font("Impact", 130, FontStyle.Bold))
                    {
                        g.DrawString(champName, nameFont, Brushes.Black, 645, 175, format); // drop shadow
                        g.DrawString(champName, nameFont, nameBrush, 640, 170, format);
                    }
                }

                if (_victoryTimer > 200)
                {
                    int bLink = (_victoryTimer / 25) % 2 == 0 ? 255 : 0;
                    using (SolidBrush bBrush = new SolidBrush(Color.FromArgb(bLink, Color.Gray)))
                    using (Font bFont = new Font("Courier New", 22, FontStyle.Bold))
                        g.DrawString("PRESS ANY BUTTON TO CONTINUE...", bFont, bBrush, 640, 650, format);
                }
            }
        }
    }

    private void DrawArcadeUI(Graphics g)
    {
        using (Font font = new Font("Impact", 24, FontStyle.Italic))
        {
            g.DrawString(_playerName, font, Brushes.White, 50, 30);
            g.FillRectangle(Brushes.DarkRed, 50, 70, 400, 30);
            g.FillRectangle(Brushes.LimeGreen, 50, 70, 400 * (p1Health / 200f), 30);
            string p2Name = GetCharacterName(_p2SelectedCharacter);
            g.DrawString(p2Name, font, Brushes.White, 830, 30);

            g.FillRectangle(Brushes.DarkRed, 830, 70, 400, 30);
            g.FillRectangle(Brushes.Yellow, 830, 70, 400 * (p2Health / 200f), 30);

            // DISPLAY ROUNDS WON
            for (int i = 0; i < _maxRounds; i++)
            {
                g.FillEllipse(i < _p1RoundsWon ? Brushes.Gold : Brushes.Gray, 50 + (i * 30), 110, 20, 20);
                g.FillEllipse(i < _p2RoundsWon ? Brushes.Gold : Brushes.Gray, 1210 - (i * 30), 110, 20, 20);
            }

            // DYNAMIC TIMER
            if (_isTimeInfinite) g.DrawString("∞", new Font("Impact", 48, FontStyle.Bold), Brushes.Gold, 610, 25);
            else g.DrawString(_matchTimer.ToString("00"), new Font("Impact", 36, FontStyle.Bold), Brushes.Gold, 610, 40);

            // VISIBLE PAUSE BUTTON (Strong bordered box icon with two bars)
            g.FillRectangle(Brushes.DarkRed, 615, 5, 50, 30);
            g.DrawRectangle(Pens.White, 615, 5, 50, 30);
            g.FillRectangle(_isPaused ? Brushes.Gold : Brushes.White, 625, 10, 10, 20);
            g.FillRectangle(_isPaused ? Brushes.Gold : Brushes.White, 645, 10, 10, 20);

            using (StringFormat format = new StringFormat() { Alignment = StringAlignment.Center })
            {
                // KO SIGN AND LOGIC
                if (_isKO)
                {
                    g.DrawString("K.O.", new Font("Impact", 120, FontStyle.Bold), Brushes.Red, 640, 220, format);
                    if (_koTimer < 80)
                    {
                        int roundNum = _p1RoundsWon + _p2RoundsWon;
                        if (roundNum == 0) roundNum = 1;
                        string winText = (p1Health > p2Health ? "YOU WIN" : GetCharacterName(_p2SelectedCharacter) + " WINS") + " ROUND " + roundNum;
                        g.DrawString(winText, new Font("Courier New", 36, FontStyle.Bold), Brushes.Gold, 640, 390, format);
                    }
                }

                // IN-GAME PAUSE MENU
                if (_isPaused)
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(200, 0, 0, 0)), -640, -100, 2560, 1440); // Dark overlay
                    g.DrawString("PAUSED", new Font("Impact", 80, FontStyle.Bold), Brushes.Red, 640, 200, format);

                    using (Font menuFont = new Font("Courier New", 32, FontStyle.Bold))
                    {
                        g.DrawString("> RESUME <", menuFont, Brushes.White, 640, 360, format);
                        g.DrawString("> QUIT TO MENU <", menuFont, Brushes.White, 640, 440, format);
                    }
                }

                // COMBO TEXT DISPLAY ("STREET COMBO!" / "POWER COMBO!")
                if (_comboDisplayTimer > 0 && _comboText != "")
                {
                    int alpha = Math.Min(255, _comboDisplayTimer * 7); // fade out
                    using (SolidBrush comboBrush = new SolidBrush(Color.FromArgb(alpha, Color.Gold)))
                    using (Font comboFont = new Font("Impact", 42, FontStyle.Bold))
                    {
                        // Shadow for readability
                        using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(alpha / 2, Color.DarkRed)))
                            g.DrawString(_comboText, comboFont, shadowBrush, 642, 392, format);
                        g.DrawString(_comboText, comboFont, comboBrush, 640, 390, format);
                    }
                }
            }
        }
    }
}