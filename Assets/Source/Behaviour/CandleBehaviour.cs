using PrimeTween;
using UnityEngine;

internal class CandleBehaviour : MonoBehaviour
{
    public const float BASE_BURN_TIME = 6.0f;
    public const float BURN_SPEED = 1.0f;

    public const float MIN_IGNITE_DELAY = 1.0f;
    public const float MAX_IGNITE_DELAY = 4.5f;

    protected Tween burnTween;
    protected static PlayerControls playerControls;

    [SerializeField, Range(1, 5)]
    protected int candleIndex;

    protected float startHeight;
    protected float currentHeight;

    protected float crowdExcitement;

    [SerializeField]
    protected Transform candleTop;

    [SerializeField]
    protected GameObject flameObject;
    [SerializeField]
    protected GameObject blowObject;
    [SerializeField]
    protected GameObject igniteObject;

    [SerializeField]
    protected float blowDuration;
    [SerializeField]
    protected float igniteDuration;

    protected float reigniteTimer;

    protected bool isLit;
    protected bool isSparking;

    protected float BurnTime
    {
        get
        {
            float currentPercent = currentHeight / startHeight * 100;
            float burnTime = currentPercent / 100 * BASE_BURN_TIME;

            return burnTime;
        }
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

        Ignite();

        GameController.Instance.OnGameOver += StopTween;
    }

    private void OnDisable()
    {
        GameController.Instance.OnGameOver -= StopTween;
    }

    private void Update()
    {
        if (GameController.Instance.GameOver)
        {
            return;
        }

        if (!isLit)
        {
            if (isSparking)
            {
                return;
            }

            reigniteTimer -= Time.deltaTime;

            if (reigniteTimer <= 0)
            {
                Spark();
            }

            return;
        }

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
        if (isLit || GameController.Instance.GameOver)
        {
            return;
        }

        isLit = true;
        isSparking = false;

        blowObject.SetActive(false);
        igniteObject.SetActive(false);
        flameObject.SetActive(true);

        currentHeight = candleTop.localPosition.y;

        burnTween.Stop();

        burnTween = Tween.LocalPositionY(candleTop, currentHeight, 0, BurnTime).OnComplete(BurnOut);
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

        GameController.Instance.BlowOutCandle();
    }

    protected void StopTween()
    {
        burnTween.Stop();
    }

    protected void BurnOut()
    {
        GameController.Instance.LoseGame();
    }
}
