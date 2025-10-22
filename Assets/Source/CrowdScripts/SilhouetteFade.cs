using System.Collections;
using UnityEngine;

internal class SilhouetteFade : MonoBehaviour {
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private float fadeInDuration = 0.5f;

    void Reset() { sr = GetComponent<SpriteRenderer>(); }
    void Awake() { if (!sr) sr = GetComponent<SpriteRenderer>(); }

    public void SetSprite(Sprite s) {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (sr) sr.sprite = s;
    }

    public void HideInstant() {
        if (!sr) return;
        var c = sr.color; c.a = 0f; sr.color = c;
        gameObject.SetActive(false);
    }

    public void Show() {
        if (!sr) return;
        StopAllCoroutines();
        gameObject.SetActive(true);
        StartCoroutine(FadeInCo());
    }

    IEnumerator FadeInCo() {
        float t = 0f;
        var c = sr.color; c.a = 0f; sr.color = c;
        while (t < fadeInDuration) {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeInDuration);
            c.a = p; sr.color = c;
            yield return null;
        }
        c.a = 1f; sr.color = c;
    }
}
