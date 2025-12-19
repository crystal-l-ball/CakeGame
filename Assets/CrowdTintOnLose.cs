using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowdTintOnLose : MonoBehaviour
{
    [Header("What to tint")]
    [Tooltip("Leave empty to auto-find all SpriteRenderers under this GameObject.")]
    [SerializeField] private Transform crowdRoot;

    [Header("Wave settings")]
    [Tooltip("Centre point of the wave in world space")]
    [SerializeField] private Transform waveCenter;
    [Tooltip("How fast the wave travels across the screen.")]
    [SerializeField] private float spreadSpeed = 4f;
    [Tooltip("How long each sprite takes to fade once the wave reaches it.")]
    [SerializeField] private float fadeDuration = 0.6f;
    [Tooltip("Extra delay before the wave starts.")]
    [SerializeField] private float startDelay = 0.0f;

    [Header("Colours")]
    [Tooltip("Final charcoal colour.")]
    [SerializeField] private Color charcoal = new Color(0.08f, 0.08f, 0.10f, 1f);

    private struct Entry {
        public SpriteRenderer sr;
        public Color original;
        public float distance;
    }

    private List<Entry> entries = new List<Entry>();
    private bool prepared = false;
    private Coroutine waveCo;

    private void Awake()
    {
        if (crowdRoot == null) crowdRoot = transform;
    }

    private void OnEnable()
    {
        // Subscribe to Lose if GameController exposes it (see step B)
        if (GameController.Instance != null)
        {
            GameController.Instance.OnLose += StartWave;
        }
    }

    private void OnDisable()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.OnLose -= StartWave;
        }
    }

    private void Prepare()
    {
        entries.Clear();
        var srs = crowdRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: false);
        Vector3 center = (waveCenter ? waveCenter.position : transform.position);

        foreach (var sr in srs)
        {
            var e = new Entry {
                sr = sr,
                original = sr.color,
                distance = Vector3.Distance(sr.transform.position, center)
            };
            entries.Add(e);
        }
        prepared = true;
    }

    public void StartWave()
    {
        if (!prepared) Prepare();
        if (waveCo != null) StopCoroutine(waveCo);
        waveCo = StartCoroutine(CoWave());
    }

    private IEnumerator CoWave()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        
        foreach (var e in entries)
        {
            float delay = (spreadSpeed <= 0f) ? 0f : (e.distance / spreadSpeed);
            StartCoroutine(CoFadeOne(e.sr, e.original, charcoal, delay, fadeDuration));
        }
        
        yield return null;
    }

    private IEnumerator CoFadeOne(SpriteRenderer sr, Color from, Color to, float delay, float dur)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            if (sr) sr.color = Color.Lerp(from, to, p);
            yield return null;
        }
        if (sr) sr.color = to;
    }

    
    public void ResetToOriginal()
    {
        if (!prepared) return;
        foreach (var e in entries)
        {
            if (e.sr) e.sr.color = e.original;
        }
    }
}
