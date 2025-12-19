using UnityEngine;
using TMPro;

public class ScoreTextBehaviour : MonoBehaviour
{
    [SerializeField] private TMP_Text label; 
    public void UpdateScore(int score, int multiplier)
    {
        if (label) label.text = $"Score: {score}  (x{multiplier})";
    }
}
