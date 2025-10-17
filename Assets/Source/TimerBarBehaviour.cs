using UnityEngine;
using UnityEngine.UI;

public class TimerBarBehaviour : MonoBehaviour
{
    [SerializeField] private Image fill; // optional; assign later if you make a UI bar

    // Called by GameController; safe to leave unassigned for now
    public void SetFill01(float t)
    {
        if (fill) fill.fillAmount = Mathf.Clamp01(t);
    }
}
