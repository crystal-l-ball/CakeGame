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
    [SerializeField] private Color hotFlame  = new Color(1f, 0.35f, 0.15f, 1f);  // reddish
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

    protected float BurnTime
    {
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
        if (playerControls == null)
        {
            playerControls = new PlayerControls();
            playerControls.Enable();
            playerControls.Player.Enable();
        }

        crowdExcitement = 100;

        startHeight = candleTop.localPosition.y;

        flameObject.SetActive(true);
        blowObject.SetActive(false);
        igniteObject.SetActive(false);

        RandomizeBurnSettings();
        Ignite();

        GameController.Instance.OnGameOver += StopTween;
    }

    private void OnDisable()
    {
        GameController.Instance.OnGameOver -= StopTween;
    }

    private void Update()
    {
        if (GameController.Instance.GameOver) return;

        if (!isLit)
        {
            if (isSparking) return;

            reigniteTimer -= Time.deltaTime;
            if (reigniteTimer <= 0) Spark();
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

    protected void Spark()
    {
        isSparking = true;
        igniteObject.SetActive(true);

        Tween.Delay(igniteDuration).OnComplete(() =>
        {
            igniteObject.SetActive(false);
            Tween.Delay(igniteDuration).OnComplete(() =>
            {
                igniteObject.SetActive(true);
                Tween.Delay(igniteDuration).OnComplete(() => igniteObject.SetActive(false));
            });
        });

        Tween.Delay(igniteDuration * 4).OnComplete(Ignite);
    }

    protected void Ignite()
    {
        if (isLit || GameController.Instance.GameOver) return;

        RandomizeBurnSettings();

        isLit = true;
        isSparking = false;

        blowObject.SetActive(false);
        igniteObject.SetActive(false);
        flameObject.SetActive(true);

        currentHeight = candleTop.localPosition.y;

        burnTween.Stop();
        burnTween = Tween.LocalPositionY(candleTop, currentHeight, 0, BurnTime).OnComplete(BurnOut);

        // CALL WARNING UPDATE immediately so color/scale match current height
        UpdateFlameWarning();
    }

    protected void Extinguish()
    {
        isLit = false;

        flameObject.SetActive(false);
        igniteObject.SetActive(false);
        blowObject.SetActive(true);

        burnTween.Stop();

        Tween.Delay(blowDuration).OnComplete(() => blowObject.SetActive(false));

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
    }

    // ---------- warning visuals ----------
    private void UpdateFlameWarning()
    {
        if (!isLit || candleTop == null || flameObject == null) return;

        float remain01 = 0f;
        float y = candleTop.localPosition.y;      // moves down to 0 via tween
        if (startHeight > 0f) remain01 = Mathf.Clamp01(y / startHeight);
        float warn01 = 1f - remain01;             // 0 → 1 as candle shrinks

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
