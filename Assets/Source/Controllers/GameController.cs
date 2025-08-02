using UnityEngine;

internal class GameController : Singleton<GameController>
{
    protected int candlesBlownOut;
    protected bool gameOver;

    public event GameOver OnGameOver;

    [SerializeField]
    protected float levelDuration;
    protected float currentTimer;

    [SerializeField]
    protected Color normalColor;
    [SerializeField]
    protected Color loseColor;

    [SerializeField]
    protected GameObject loseFire;

    [SerializeField]
    protected GameObject musicParent;
    [SerializeField]
    protected GameObject victoryMusicParent;

    public int CandlesBlownOut => candlesBlownOut;
    public bool GameOver => gameOver;

    private void Update()
    {
        if (gameOver)
        {
            return;
        }

        currentTimer += Time.deltaTime;

        if (currentTimer >= levelDuration)
        {
            WinGame();
        }
    }

    public void BlowOutCandle()
    {
        candlesBlownOut++;

        CandleCounterBehaviour.Instance.UpdateCount(candlesBlownOut);
    }

    public void WinGame()
    {
        gameOver = true;

        musicParent.SetActive(false);
        victoryMusicParent.SetActive(true);

        OnGameOver?.Invoke();
    }

    public void LoseGame()
    {
        gameOver = true;

        musicParent.SetActive(false);

        Camera.main.backgroundColor = loseColor;

        loseFire.SetActive(true);

        OnGameOver?.Invoke();
    }
}

public delegate void GameOver();