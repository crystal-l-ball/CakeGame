using UnityEngine;
using UnityEngine.SceneManagement;

internal class GameController : Singleton<GameController>
{
    
    public int  CandlesBlownOut => candlesBlownOut;
    public bool GameOver        => gameOver;

    // Screenz
    [Header("Screen Groups")]
    [SerializeField] private GameObject idleGroup;
    [SerializeField] private GameObject playGroup;
    [SerializeField] private GameObject winGroup;
    [SerializeField] private GameObject loseGroup;

    [Header("Balloon as timer")]
    [SerializeField] private BalloonTimer balloonTimer;

    [Header("Start Overlay")]
    [SerializeField] private GameObject startOverlay; 


    public event System.Action OnBeginIgnition; // need to delay candle burn for start/instruction screen
    public event System.Action OnLose;          
    public event GameOver      OnGameOver;

    // Sound
    [Header("Audio")]
    [SerializeField] private AudioSource idleMusic;  // loops during Idle
    [SerializeField] private AudioSource gameMusic;  // main song (NO loop)

    // UI (not done yet -- don't want to show score? Now using sprinkle count instead??)
    [Header("Score")]
    [SerializeField] private ScoreTextBehaviour scoreText;
    [SerializeField] private int pointsPerCandle = 10;

    // =================== LOSE VFX ===========================
    [Header("Lose VFX (optional)")]
    [SerializeField] private ParticleSystem loseSmoke;

    // =================== COLOURS =============================
    [Header("Colours")]
    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private Color loseColor   = Color.black;

    // =================== EXISTING OBJECTS (optional) ========
    [Header("Existing Music Parents (optional)")]
    [SerializeField] private GameObject musicParent;
    [SerializeField] private GameObject victoryMusicParent;

    // Timing
    [Header("Game Timing (auto set from song)")]
    [SerializeField] private float levelDuration; // set to one song length on StartGame
    private float currentTimer;


    private int  candlesBlownOut;
    private int  score;
    private bool gameOver;

    private enum State { Idle, Playing, Win, Lose }
    private State state = State.Idle;

    private static PlayerControls playerControls; 

    // Can't remember what this bit is for?
    [Header("Bonus Plumbing (optional)")]
    [SerializeField] private CandleBehaviour[] candleRefs; // for later
    private float[] lastExtinguishTime = new float[5];
    [SerializeField] private float bestieWindow = 0.25f;

    

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
                // Game starts when player blows any candle
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

                // Win when song is finished - maybe refine? If candle burn is too slow, its almost impossible to lose 
                if (currentTimer >= levelDuration || (gameMusic && !gameMusic.isPlaying))
                {
                    WinGame();
                }
                break;

            case State.Win:
            case State.Lose:
               
                break;
        }
    }

    // State starts
    private void EnterIdle()
    {
        state = State.Idle;
        gameOver = false;
        candlesBlownOut = 0;
        score = 0;
        currentTimer = 0f;

            BonusUI.Instance?.ClearPinnedPop();


        if (Camera.main) Camera.main.backgroundColor = normalColor;

        // what's shown
        if (idleGroup) idleGroup.SetActive(true);
        if (playGroup) playGroup.SetActive(false);
        if (winGroup)  winGroup.SetActive(false);
        if (loseGroup) loseGroup.SetActive(false);

        // Audio and music
        if (victoryMusicParent) victoryMusicParent.SetActive(false);
        if (musicParent)        musicParent.SetActive(false);

        if (idleMusic)
        {
            idleMusic.loop = true;
            if (!idleMusic.isPlaying) idleMusic.Play();
        }
        if (gameMusic) gameMusic.Stop();

        // Reset the smoke
        ResetLoseSmoke();

        // Reset the balloon timer
        balloonTimer?.ResetBalloon();

        // Reset the score
        scoreText?.UpdateScore(0, 1);

        // Reset crowd darkening/tint
        var crowd = Object.FindFirstObjectByType<CrowdTintOnLose>();
        crowd?.ResetToOriginal();
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
            // One playthrough of birthday song -->
            levelDuration = (gameMusic.clip != null) ? gameMusic.clip.length : 44f;
        }
        else
        {
            levelDuration = 44f; // This will be used if the music clip isn't assigned
        }

        // Start the balloon timer
        balloonTimer?.Begin(levelDuration);

        // Show instructions overlay and 5s delay before candles ignite 
        if (startOverlay) startOverlay.SetActive(true);
        StartCoroutine(BeginIgnitionAfterDelay(5f));

        // reset score
        scoreText?.UpdateScore(0, 1);
    }

    private System.Collections.IEnumerator BeginIgnitionAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        // hide instructions overlay
        if (startOverlay) startOverlay.SetActive(false);

        // All candles can now ignite
        OnBeginIgnition?.Invoke();
    }

    
    public void BlowOutCandle()
    {
        candlesBlownOut++;

        // The counting UI isn't used at the moment. Sprinkles represent "score" or a candle blown out
        var counter = CandleCounterBehaviour.Instance;
        if (counter != null)
            counter.UpdateCount(candlesBlownOut);
    }

    // Candle behavious references this for adding sprinkles and scoring
    public void BlowOutCandle(int candleIndex, Transform candleRoot)
    {
        BlowOutCandle(); 

        DecorationManager.Instance?.SpawnAt(candleRoot);

        // scoring stuff - still a bit of a shambles. 1 blow should = 1 sprinkle, unless there's a bonus
        score += pointsPerCandle;
        scoreText?.UpdateScore(score, 1); 
    }

    // For bonus
    public void ReportExtinguish(int candleIndex, Transform candleRoot, float remaining01)
    {
        int i = Mathf.Clamp(candleIndex - 1, 0, lastExtinguishTime.Length - 1);
        lastExtinguishTime[i] = Time.time;

       
    }

    public void WinGame()
    {
        if (state == State.Win) return;
        state = State.Win;
        gameOver = true;

            BonusUI.Instance?.KeepLastPop();


        if (musicParent)        musicParent.SetActive(false);
        if (victoryMusicParent) victoryMusicParent.SetActive(true);

        if (playGroup) playGroup.SetActive(false);
        if (winGroup)  winGroup.SetActive(true);

        OnGameOver?.Invoke();

       
        Invoke(nameof(ReloadScene), 15f);
    }

    public void LoseGame()
    {
        if (state == State.Lose) return;
        state = State.Lose;
        gameOver = true;

        if (musicParent) musicParent.SetActive(false);
        if (Camera.main) Camera.main.backgroundColor = loseColor;
        if (loseGroup)   loseGroup.SetActive(true);

        if (loseSmoke != null)
        {
            loseSmoke.gameObject.SetActive(true);
            var em = loseSmoke.emission;
            em.rateOverTime = 80f;
            loseSmoke.Play();
        }

        
        OnLose?.Invoke();

        OnGameOver?.Invoke();

        
        Invoke(nameof(ReloadScene), 15f);
    }

    private void ResetLoseSmoke()
    {
        if (!loseSmoke) return;
        var em = loseSmoke.emission;
        em.rateOverTime = 0f;
        loseSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        loseSmoke.gameObject.SetActive(false);
    }

    private void ReloadScene()
    {
        var current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}

public delegate void GameOver();
