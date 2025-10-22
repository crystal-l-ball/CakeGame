using PrimeTween;
using UnityEngine;

internal class CandleBehaviour : MonoBehaviour
{
    [Header("Burn Settings")]
    [SerializeField] private float minBurnTime = 14.0f;
    [SerializeField] private float maxBurnTime = 20.0f;
    [SerializeField] private float minBurnSpeed = 0.15f;
    [SerializeField] private float maxBurnSpeed = 0.25f;

    public const float MIN_IGNITE_DELAY = 1.0f;
    public const float MAX_IGNITE_DELAY = 4.5f;

    protected Tween burnTween;
    protected static PlayerControls playerControls;

    [SerializeField, Range(1, 5)]
    protected int candleIndex;

    protected float startHeight;
    protected float currentHeight;

    protected float crowdExcitement;

    // --- Flame warning (size & color as candle shrinks) ---
    [Header("Flame Warning")]
    [SerializeField] private SpriteRenderer flameSprite;          // drag SpriteRenderer from Flame_01
    [SerializeField] private Color coolFlame = new Color(1f, 0.95f, 0.85f, 1f); // warm white
    [SerializeField] private Color hotFlame  = new Color(1f, 0.35f, 0.15f, 1f); // reddish
    [SerializeField] private Vector3 flameScaleMin = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 flameScaleMax = new Vector3(1.25f, 1.25f, 1f);

    [Header("Scene Refs")]
    [SerializeField] protected Transform candleTop;
    [SerializeField] protected GameObject flameObject;
    [SerializeField] protected GameObject blowObject;
    [SerializeField] protected GameObject igniteObject;

    [Header("Timings")]
    [SerializeField] protected float blowDuration = 0.167f;
    [SerializeField] protected float igniteDuration = 0.25f;

    protected float reigniteTimer;

    protected bool isLit;
    protected bool isSparking;

    // Randomized values for each burn cycle
    private float baseBurnTime; // between minBurnTime/maxBurnTime
    private float burnSpeed;    // between minBurnSpeed/maxBurnSpeed

    // Gate so candles don't auto-spark before OnBeginIgnition
    private bool ignitionEnabled = false;

    protected float BurnTime {
        get {
            float currentPercent = currentHeight / startHeight * 100f;
            float burnTime = currentPercent / 100f * baseBurnTime;
            return burnTime / burnSpeed; // smaller speed = longer duration
        }
    }

    private void RandomizeBurnSettings() {
        baseBurnTime = Random.Range(minBurnTime, maxBurnTime);
        burnSpeed    = Random.Range(minBurnSpeed, maxBurnSpeed);
    }

    private void OnEnable()
    {
        if (playerControls == null) {
            playerControls = new PlayerControls();
            playerControls.Enable();
            playerControls.Player.Enable();
        }

        ignitionEnabled = false; // wait for GameController signal

        crowdExcitement = 100f;
        startHeight = candleTop.localPosition.y;

        if (flameObject)  flameObject.SetActive(true);
        if (blowObject)   blowObject.SetActive(false);
        if (igniteObject) igniteObject.SetActive(false);

        RandomizeBurnSettings();

        // DO NOT call Ignite() here — wait for controller's signal
        if (GameController.Instance != null) {
            GameController.Instance.OnGameOver       += StopTween;
            GameController.Instance.OnBeginIgnition  += BeginIgnition; // subscribe to delayed start
        }
    }

private void OnDisable() {
    if (GameController.Instance != null) {
        GameController.Instance.OnGameOver      -= StopTween;
        GameController.Instance.OnBeginIgnition -= BeginIgnition;
    }
    // 👉 LED safety: ensure OFF if this candle is disabled/destroyed
    ArduinoLedController.Instance?.Off(candleIndex);
}


    private void Update()
    {
        if (GameController.Instance != null && GameController.Instance.GameOver)
            return;

        // Before the controller says "go", ignore ignition/spark logic
        if (!ignitionEnabled)
            return;

        if (!isLit)
        {
            if (isSparking) return;

            reigniteTimer -= Time.deltaTime;
            if (reigniteTimer <= 0f) Spark();
            return;
        }

        // CALL WARNING UPDATE while lit
        UpdateFlameWarning();

        if ((playerControls.Player.Blow_Candle_A.WasPerformedThisFrame() && candleIndex == 1) ||
            (playerControls.Player.Blow_Candle_S.WasPerformedThisFrame() && candleIndex == 2) ||
            (playerControls.Player.Blow_Candle_D.WasPerformedThisFrame() && candleIndex == 3) ||
            (playerControls.Player.Blow_Candle_F.WasPerformedThisFrame() && candleIndex == 4) ||
            (playerControls.Player.Blow_Candle_G.WasPerformedThisFrame() && candleIndex == 5))
        {
            Extinguish();
        }
    }

    // Called by GameController after the 5s overlay delay
    private void BeginIgnition() {
        ignitionEnabled = true;
        Ignite();
    }

    protected void Spark() {
    isSparking = true;
    if (igniteObject) igniteObject.SetActive(true);

    // 👉 LED: candle is trying to ignite → flicker this index
    ArduinoLedController.Instance?.Flicker(candleIndex);

    Tween.Delay(igniteDuration).OnComplete(() => {
        if (igniteObject) igniteObject.SetActive(false);
        Tween.Delay(igniteDuration).OnComplete(() => {
            if (igniteObject) igniteObject.SetActive(true);
            Tween.Delay(igniteDuration).OnComplete(() => { if (igniteObject) igniteObject.SetActive(false); });
        });
    });

    Tween.Delay(igniteDuration * 4).OnComplete(Ignite);
}


protected void Ignite() {
    if (isLit || (GameController.Instance != null && GameController.Instance.GameOver))
        return;

    RandomizeBurnSettings();

    isLit = true;
    isSparking = false;

    if (blowObject)   blowObject.SetActive(false);
    if (igniteObject) igniteObject.SetActive(false);
    if (flameObject)  flameObject.SetActive(true);

    // 👉 LED: candle is now lit → steady ON
    ArduinoLedController.Instance?.OnSteady(candleIndex);

    currentHeight = candleTop.localPosition.y;

    burnTween.Stop();
    burnTween = Tween.LocalPositionY(candleTop, currentHeight, 0f, BurnTime).OnComplete(BurnOut);

    UpdateFlameWarning();
}


protected void Extinguish() {
    isLit = false;

    if (flameObject)  flameObject.SetActive(false);
    if (igniteObject) igniteObject.SetActive(false);
    if (blowObject)   blowObject.SetActive(true);

    // 👉 LED: candle is not lit → OFF
    ArduinoLedController.Instance?.Off(candleIndex);

    burnTween.Stop();

    Tween.Delay(blowDuration).OnComplete(() => { if (blowObject) blowObject.SetActive(false); });

    currentHeight = candleTop.localPosition.y;
    reigniteTimer = Random.Range(MIN_IGNITE_DELAY, MAX_IGNITE_DELAY);

    GameController.Instance.BlowOutCandle(candleIndex, this.transform);
}



    protected void StopTween()
    {
        burnTween.Stop();
    }

    protected void BurnOut()
    {
        GameController.Instance.LoseGame();
        
         // If we've already won (or lost), ignore late completions
        if (GameController.Instance == null || GameController.Instance.GameOver) return;

    GameController.Instance.LoseGame();
    }

    // ---------- warning visuals ----------
    private void UpdateFlameWarning()
    {
        if (!isLit || candleTop == null || flameObject == null) return;

        float remain01 = 0f;
        float y = candleTop.localPosition.y; // moves down to 0 via tween
        if (startHeight > 0f) remain01 = Mathf.Clamp01(y / startHeight);
        float warn01 = 1f - remain01;        // 0 → 1 as candle shrinks

        // size
        flameObject.transform.localScale = Vector3.Lerp(flameScaleMin, flameScaleMax, warn01);

        // color
        if (flameSprite != null)
            flameSprite.color = Color.Lerp(coolFlame, hotFlame, warn01);
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-fill the sprite reference if possible
        if (flameSprite == null && flameObject != null)
        {
            flameSprite = flameObject.GetComponent<SpriteRenderer>()
                       ?? flameObject.GetComponentInChildren<SpriteRenderer>();
        }
    }
#endif
}
