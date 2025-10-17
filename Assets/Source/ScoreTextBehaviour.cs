using UnityEngine;
using TMPro;

public class ScoreTextBehaviour : MonoBehaviour
{
    [SerializeField] private TMP_Text label; // optional; assign later if you add score UI

    // Called by GameController; safe to leave unassigned for now
    public void UpdateScore(int score, int multiplier)
    {
        if (label) label.text = $"Score: {score}  (x{multiplier})";
    }
}
