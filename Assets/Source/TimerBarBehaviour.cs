using UnityEngine;
using UnityEngine.UI;

public class TimerBarBehaviour : MonoBehaviour
{
    [SerializeField] private Image fill; 
    public void SetFill01(float t)
    {
        if (fill) fill.fillAmount = Mathf.Clamp01(t);
    }
}
