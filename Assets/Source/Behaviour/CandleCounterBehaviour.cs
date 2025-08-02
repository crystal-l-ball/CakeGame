using TMPro;

internal class CandleCounterBehaviour : Singleton<CandleCounterBehaviour>
{
    protected TextMeshProUGUI counterText;

    private void OnEnable()
    {
        counterText = GetComponent<TextMeshProUGUI>();
    }

    public void UpdateCount(int count)
    {
        counterText.text = count.ToString();
    }
}
