using UnityEngine;

internal class GameController : Singleton<GameController>
{
    // =================== READ-ONLY PROPERTIES ===================
    public int CandlesBlownOut => candlesBlownOut;
    public bool GameOver => gameOver;

    // =================== SCREEN GROUPS ==========================
    [Header("Screen Groups (drag scene parents here)")]
    [Tooltip("Idle screen parent: unlit cake, invite card, idle music, etc.")]
    [SerializeField] private GameObject idleGroup;
    [Tooltip("Gameplay parent: lit cake, silhouettes, HUD (timer/score), etc.")]
    [SerializeField] private GameObject playGroup;
    [Tooltip("Win screen parent: close-up cake, hooray text, balloons, etc.")]
    [SerializeField] private GameObject winGroup;
    [Tooltip("Lose screen parent: your flipbook fire + smoke + fire VFX")]
    [SerializeField] private GameObject loseGroup;

    // =================== AUDIO ==============================
    [Header("Audio (drag AudioSources)")]
    [Tooltip("Loops during Idle")]
    [SerializeField] private AudioSource idleMusic;
    [Tooltip("Main game song (NON-loop). Round length = 2 × this clip length.")]
    [SerializeField] private AudioSource gameMusic;

    // =================== HUD / UI ===========================
    [Header("HUD (optional)")]
    [Tooltip("Timer bar controller (Image Fill). Leave empty if not made yet.")]
    [SerializeField] private TimerBarBehaviour timerBar;
    [Tooltip("Score text controller (optional). Leave empty if not made yet.")]
    [SerializeField] private ScoreTextBehaviour scoreText;

    [Header("Scoring (optional)")]
    [SerializeField] private int pointsPerCandle = 10;
    private int score;

    // =================== LOSE VFX ===========================
    [Header("Lose VFX (optional)")]
    [Tooltip("ParticleSystem for looping smoke on lose. Leave empty if not used.")]
    [SerializeField] private ParticleSystem loseSmoke;

    // =================== COLORS =============================
    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private Color loseColor   = Color.black;

    // =================== (Your existing objects) ============
    [Header("Existing Music Parents (optional)")]
    [Tooltip("Your scene's regular music parent. Safe to leave null.")]
    [SerializeField] private GameObject musicParent;
    [Tooltip("Your scene's victory music parent. Safe to leave null.")]
    [SerializeField] private GameObject victoryMusicParent;

    // =================== GAME TIMING ========================
    [Header("Game Timing (auto sets from song)")]
    [SerializeField] private float levelDuration; // set to 2× song length on StartGame
    private float currentTimer;

    // =================== INTERNAL STATE =====================
    private int  candlesBlownOut;
    private bool gameOver;

    public event GameOver OnGameOver;

    private enum State { Idle, Playing, Win, Lose }
    private State state = State.Idle;

    private static PlayerControls playerControls; // uses your Input Actions map

    // ------------------- Unity -------------------------------
    private void Start()
    {
        if (playerControls == null)
        {
            playerControls = new PlayerControls();
            playerControls.Enable();
            playerControls.Player.Enable();
        }

        EnterIdle();
    }

    private void Update()
    {
        switch (state)
        {
            case State.Idle:
                // Start on ANY blow key (A/S/D/F/G)
                if (playerControls.Player.Blow_Candle_A.WasPerformedThisFrame()
                 || playerControls.Player.Blow_Candle_S.WasPerformedThisFrame()
                 || playerControls.Player.Blow_Candle_D.WasPerformedThisFrame()
                 || playerControls.Player.Blow_Candle_F.WasPerformedThisFrame()
                 || playerControls.Player.Blow_Candle_G.WasPerformedThisFrame())
                {
                    StartGame();
                }
                break;

            case State.Playing:
                if (gameOver) return;

                currentTimer += Time.deltaTime;

                // Timer bar from 1 -> 0
                if (levelDuration > 0f)
                {
                    float p = Mathf.Clamp01(1f - (currentTimer / levelDuration));
                    timerBar?.SetFill01(p);
                }

                // Win when song finished (2 loops) or timer ran out
                if (currentTimer >= levelDuration || (gameMusic && !gameMusic.isPlaying))
                {
                    WinGame();
                }
                break;

            case State.Win:
            case State.Lose:
                // Wait for auto-return to Idle
                break;
        }
    }

    // ------------------- State Entrances ---------------------
    private void EnterIdle()
    {
        state = State.Idle;
        gameOver = false;
        candlesBlownOut = 0;
        currentTimer = 0f;

        if (Camera.main) Camera.main.backgroundColor = normalColor;

        // Visibility
        if (idleGroup) idleGroup.SetActive(true);
        if (playGroup) playGroup.SetActive(false);
        if (winGroup)  winGroup.SetActive(false);
        if (loseGroup) loseGroup.SetActive(false);

        // Audio
        if (victoryMusicParent) victoryMusicParent.SetActive(false);
        if (musicParent)        musicParent.SetActive(false);

        if (idleMusic)
        {
            idleMusic.loop = true;
            if (!idleMusic.isPlaying) idleMusic.Play();
        }
        if (gameMusic) gameMusic.Stop();

        // VFX reset
        ResetLoseSmoke();

        // HUD reset
        timerBar?.SetFill01(1f);
        scoreText?.UpdateScore(0, 1);
    }

    private void StartGame()
    {
        state = State.Playing;
        gameOver = false;
        candlesBlownOut = 0;
        currentTimer = 0f;

        if (idleGroup) idleGroup.SetActive(false);
        if (playGroup) playGroup.SetActive(true);
        if (winGroup)  winGroup.SetActive(false);
        if (loseGroup) loseGroup.SetActive(false);

        // Audio switching
        if (idleMusic) idleMusic.Stop();
        if (musicParent)        musicParent.SetActive(true);
        if (victoryMusicParent) victoryMusicParent.SetActive(false);

        if (gameMusic)
        {
            gameMusic.loop = false;
            gameMusic.time = 0f;
            gameMusic.Play();

            if (gameMusic.clip != null)
                levelDuration = gameMusic.clip.length * 2f; // 2 loops
        }

        timerBar?.SetFill01(1f);
        scoreText?.UpdateScore(0, 1);
    }

    // ------------------- Existing hooks ----------------------

    // ← added back: no-argument version that older calls use
    public void BlowOutCandle()
    {
        candlesBlownOut++;
        CandleCounterBehaviour.Instance.UpdateCount(candlesBlownOut);
    }

    // New overload used by CandleBehaviour to spawn decorations
    public void BlowOutCandle(int candleIndex, Transform candleRoot)
    {
        // keep old behavior
        BlowOutCandle();

        // spawn 1 random decoration at this candle
        DecorationManager.Instance?.SpawnAt(candleRoot);

        // optional scoring
        score += pointsPerCandle;
        scoreText?.UpdateScore(score, 1); // multiplier=1 for now
    }

    public void WinGame()
    {
        if (state == State.Win) return;
        state = State.Win;
        gameOver = true;

        if (musicParent)        musicParent.SetActive(false);
        if (victoryMusicParent) victoryMusicParent.SetActive(true);

        if (playGroup) playGroup.SetActive(false);
        if (winGroup)  winGroup.SetActive(true);

        OnGameOver?.Invoke();

        // Return to Idle after 15 seconds
        Invoke(nameof(EnterIdle), 15f);
    }

    public void LoseGame()
    {
        if (state == State.Lose) return;
        state = State.Lose;
        gameOver = true;

        if (musicParent) musicParent.SetActive(false);

        if (Camera.main) Camera.main.backgroundColor = loseColor;

        if (loseGroup) loseGroup.SetActive(true);

        // Smoke VFX
        if (loseSmoke != null)
        {
            loseSmoke.gameObject.SetActive(true);
            var em = loseSmoke.emission;
            em.rateOverTime = 80f; // adjust thickness
            loseSmoke.Play();
        }

        OnGameOver?.Invoke();

        // Return to Idle after 15 seconds
        Invoke(nameof(EnterIdle), 15f);
    }

    // ------------------- Helpers -----------------------------
    private void ResetLoseSmoke()
    {
        if (!loseSmoke) return;
        var em = loseSmoke.emission;
        em.rateOverTime = 0f;
        loseSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        loseSmoke.gameObject.SetActive(false);
    }
}

public delegate void GameOver();
