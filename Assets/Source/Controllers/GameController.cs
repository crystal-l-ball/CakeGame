using UnityEngine;
using UnityEngine.SceneManagement;

internal class GameController : Singleton<GameController>
{
    // =================== READ-ONLY ===================
    public int  CandlesBlownOut => candlesBlownOut;
    public bool GameOver => gameOver;

    private System.Collections.IEnumerator BeginIgnitionAfterDelay(float seconds) {
    yield return new WaitForSeconds(seconds);

    // hide overlay
    if (startOverlay) startOverlay.SetActive(false);

    // tell all candles to ignite now
    OnBeginIgnition?.Invoke();
}


    // =================== SCREEN GROUPS ==========================
    [Header("Screen Groups (drag scene parents here)")]
    [SerializeField] private GameObject idleGroup;
    [SerializeField] private GameObject playGroup;
    [SerializeField] private GameObject winGroup;
    [SerializeField] private GameObject loseGroup;

    [Header("Round Timer (balloon)")]
    [SerializeField] private BalloonTimer balloonTimer;

    [SerializeField] private GameObject startOverlay; // drag StartOverlay here in Inspector

public event System.Action OnBeginIgnition; // candles will subscribe to this


    // =================== AUDIO ==============================
    [Header("Audio (drag AudioSources)")]
    [SerializeField] private AudioSource idleMusic;  // loops during Idle
    [SerializeField] private AudioSource gameMusic;  // main song (NO loop)

    // =================== OPTIONAL UI =========================
    [Header("Score (optional)")]
    [SerializeField] private ScoreTextBehaviour scoreText;
    [SerializeField] private int pointsPerCandle = 10;

    // =================== LOSE VFX ===========================
    [Header("Lose VFX (optional)")]
    [SerializeField] private ParticleSystem loseSmoke;

    // =================== COLORS =============================
    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private Color loseColor   = Color.black;

    // =================== EXISTING OBJECTS (optional) ========
    [Header("Existing Music Parents (optional)")]
    [SerializeField] private GameObject musicParent;
    [SerializeField] private GameObject victoryMusicParent;

    // =================== GAME TIMING ========================
    [Header("Game Timing (auto set from song)")]
    [SerializeField] private float levelDuration; // set to 1× song length on StartGame
    private float currentTimer;

    // =================== INTERNAL STATE =====================
    private int  candlesBlownOut;
    private int  score;
    private bool gameOver;

    public event GameOver OnGameOver;

    private enum State { Idle, Playing, Win, Lose }
    private State state = State.Idle;

    private static PlayerControls playerControls; // your Input Actions map

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

                // Win when song finished (backup: timer exceeded)
                if (currentTimer >= levelDuration || (gameMusic && !gameMusic.isPlaying))
                {
                    WinGame();
                }
                break;



            case State.Win:
            case State.Lose:
                // Waiting for auto-return to Idle (we reload scene instead)
                break;
        }
    }

    // ------------------- State Entrances ---------------------
    private void EnterIdle()
    {
        state = State.Idle;
        gameOver = false;
        candlesBlownOut = 0;
        score = 0;
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

        // Balloon back to bottom & hidden
        balloonTimer?.ResetBalloon();

        // Score reset
        scoreText?.UpdateScore(0, 1);
    }

    private void StartGame()
    {
        state = State.Playing;
        gameOver = false;
        candlesBlownOut = 0;
        score = 0;
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
            // ONE play of the song:
            levelDuration = (gameMusic.clip != null) ? gameMusic.clip.length : 44f;
        }
        else
        {
            levelDuration = 44f; // fallback if no clip assigned
        }

// start the balloon timer for the round length
balloonTimer?.Begin(levelDuration);

// >>> SHOW OVERLAY & START 5s DELAY <<<
if (startOverlay) {
    startOverlay.SetActive(true);
}
StartCoroutine(BeginIgnitionAfterDelay(5f));

// Score reset (keep if you want)
scoreText?.UpdateScore(0, 1);
    }

    // ------------------- Existing hooks ----------------------
    public void BlowOutCandle()
    {
        candlesBlownOut++;

        // NULL-SAFE: counter UI might not exist in this scene
        var counter = CandleCounterBehaviour.Instance;
        if (counter != null)
            counter.UpdateCount(candlesBlownOut);
        // else: no counter UI; skip
    }

    // Used by CandleBehaviour to spawn decorations + score
    public void BlowOutCandle(int candleIndex, Transform candleRoot)
    {
        BlowOutCandle(); // preserves your old behavior, now null-safe

        DecorationManager.Instance?.SpawnAt(candleRoot);

        // optional scoring
        score += pointsPerCandle;
        scoreText?.UpdateScore(score, 1); // multiplier = 1 for now
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

        // Full reset after delay
        Invoke(nameof(ReloadScene), 15f);
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

        // Full reset after delay
        Invoke(nameof(ReloadScene), 15f);
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

    private void ReloadScene() {
        var current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}

public delegate void GameOver();
