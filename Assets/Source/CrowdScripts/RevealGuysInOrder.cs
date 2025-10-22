using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Put this on the parent that holds your 12 "guy" sprites.
/// Place/scale all guys in the Scene, disable them, and this reveals them one-by-one.
internal class RevealGuysInOrder : MonoBehaviour {
    [Header("Targets")]
    [SerializeField] private bool autoCollectChildren = true;
    [SerializeField] private List<GameObject> guys = new List<GameObject>();

    [Header("Timing")]
    [SerializeField] private float initialDelay = 0f;
    [SerializeField] private float intervalSeconds = 3.5f;

    [Header("Behavior")]
    [SerializeField] private bool startOnEnable = true;
    [SerializeField] private bool randomizeOrder = false;

    [Header("Visuals")]
    [SerializeField] private bool fadeIn = true;
    [SerializeField] private float fadeDuration = 0.4f;

    // NEW — Drift settings
    [Header("Drift (dance)")]
    [SerializeField] private bool enableDrift = true;
    [SerializeField] private Vector2 swayXAmplitudeRange = new Vector2(0.06f, 0.16f);
    [SerializeField] private Vector2 swayXFrequencyRange = new Vector2(0.35f, 0.9f);
    [SerializeField] private Vector2 swayYAmplitudeRange = new Vector2(0.00f, 0.06f);
    [SerializeField] private Vector2 swayYFrequencyRange = new Vector2(0.25f, 0.6f);

    Coroutine loop;

    void Reset() {
        autoCollectChildren = true;
        CollectChildren();
    }

    void OnEnable() {
        if (startOnEnable) Begin();
        if (GameController.Instance != null)
            GameController.Instance.OnGameOver += StopRevealing;
    }

    void OnDisable() {
        StopRevealing();
        if (GameController.Instance != null)
            GameController.Instance.OnGameOver -= StopRevealing;
    }

    public void Begin() {
        if (autoCollectChildren) CollectChildren();
        HideAll();
        StopRevealing();
        loop = StartCoroutine(RevealLoop());
    }

    public void StopRevealing() {
        if (loop != null) { StopCoroutine(loop); loop = null; }
    }

    void CollectChildren() {
        guys.Clear();
        for (int i = 0; i < transform.childCount; i++)
            guys.Add(transform.GetChild(i).gameObject);
    }

    void HideAll() {
        foreach (var g in guys) {
            if (!g) continue;
            if (fadeIn) SetAlpha(g, 0f);
            g.SetActive(false);
        }
    }

    IEnumerator RevealLoop() {
        if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);

        var order = new List<GameObject>(guys);
        order.RemoveAll(g => g == null);
        if (randomizeOrder) Shuffle(order);

        foreach (var go in order) {
            if (go == null) continue;

            // Activate
            go.SetActive(true);

            // NEW — add/configure drift per guy at activation
            if (enableDrift) {
                var drift = go.GetComponent<SilhouetteDrift>();
                if (!drift) drift = go.AddComponent<SilhouetteDrift>();
                drift.swayXAmplitude = Random.Range(swayXAmplitudeRange.x, swayXAmplitudeRange.y);
                drift.swayXFrequency = Random.Range(swayXFrequencyRange.x, swayXFrequencyRange.y);
                drift.swayYAmplitude = Random.Range(swayYAmplitudeRange.x, swayYAmplitudeRange.y);
                drift.swayYFrequency = Random.Range(swayYFrequencyRange.x, swayYFrequencyRange.y);
                // OnEnable() in SilhouetteDrift captures the current position as its base point
            }

            if (fadeIn) {
                yield return FadeTo(go, 1f, fadeDuration);
            }

            yield return new WaitForSeconds(intervalSeconds);
        }
    }

    // --- Helpers ---
    void SetAlpha(GameObject go, float a) {
        var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers) {
            var c = r.color; c.a = a; r.color = c;
        }
    }

    IEnumerator FadeTo(GameObject go, float targetA, float duration) {
        var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
        float t = 0f;
        var starts = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) starts[i] = renderers[i].color.a;

        while (t < duration) {
            t += Time.deltaTime;
            float p = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            for (int i = 0; i < renderers.Length; i++) {
                var c = renderers[i].color;
                c.a = Mathf.Lerp(starts[i], targetA, p);
                renderers[i].color = c;
            }
            yield return null;
        }
        for (int i = 0; i < renderers.Length; i++) {
            var c = renderers[i].color; c.a = targetA; renderers[i].color = c;
        }
    }

    static void Shuffle<T>(IList<T> list) {
        for (int i = list.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
