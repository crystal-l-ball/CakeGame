using PrimeTween;
using UnityEngine;

internal class FireBehaviour : MonoBehaviour
{
    [SerializeField]
    protected float ignitionDuration;

    [SerializeField]
    protected GameObject ignitionObject;
    [SerializeField]
    protected GameObject fireObject;

    private void OnEnable()
    {
        ignitionObject.SetActive(true);
        fireObject.SetActive(false);

        Tween.Delay(ignitionDuration).OnComplete(() =>
        {
            ignitionObject.SetActive(false);
            fireObject.SetActive(true);
        });
    }
}
