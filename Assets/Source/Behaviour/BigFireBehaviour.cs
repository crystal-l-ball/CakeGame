using PrimeTween;
using UnityEngine;

internal class BigFireBehaviour : MonoBehaviour
{
    [SerializeField]
    protected float ignitionDuration;

    protected SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.enabled = false;

        Tween.Delay(ignitionDuration).OnComplete(() => spriteRenderer.enabled = true);
    }
}
