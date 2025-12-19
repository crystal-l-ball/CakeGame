using UnityEngine;
using PrimeTween;
using System.Collections.Generic;

internal class DecorationManager : Singleton<DecorationManager>
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] decorationPrefabs;

    [Header("Spawn Scatter (around anchor, local units)")]
    [SerializeField, Min(0f)] private float spawnRadius = 0.12f;
    
    [SerializeField, Min(0f)] private float minSeparation = 0.05f;
    [SerializeField, Range(1, 30)] private int maxTries = 12;

    [Header("Arrival Motion (world units / seconds)")]
    [SerializeField] private float flyDistance = 3.0f;
    [SerializeField] private Vector2 flyTimeRange = new Vector2(0.6f, 1.0f);
    [SerializeField] private float spinDegrees = 360f;
    [SerializeField] private bool fromAllDirections = true;
    [SerializeField] private Ease landEase = Ease.OutCubic;

    [SerializeField] private bool emitPopText = true; 
    private int totalSprinkles = 0;
    public int TotalSprinkles => totalSprinkles;


    public void SpawnAt(Transform candleRoot)
    {
        if (candleRoot == null) { Debug.LogWarning("SpawnAt: candleRoot is null"); return; }
        if (decorationPrefabs == null || decorationPrefabs.Length == 0)
        
        {
            Debug.LogWarning("SpawnAt: decorationPrefabs is empty on DecorationManager"); return;
        }

        
        Transform anchor = candleRoot.Find("DecorationAnchor");
        if (anchor == null)
        {
            var anchorGO = new GameObject("DecorationAnchor");
            anchor = anchorGO.transform;
            anchor.SetParent(candleRoot);
            anchor.localPosition = Vector3.zero;
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
        }

        
        var prefab = decorationPrefabs[Random.Range(0, decorationPrefabs.Length)];
        if (prefab == null) { Debug.LogWarning("SpawnAt: picked prefab is null (Missing entry?)"); return; }

        
        Vector3 localPos = Vector3.zero;
        bool found = false;
        for (int i = 0; i < maxTries; i++)
        {
            Vector2 r = Random.insideUnitCircle * spawnRadius;
            localPos = new Vector3(r.x, r.y, 0f);
            if (IsFarEnoughFromOthers(anchor, localPos)) { found = true; break; }
        }
        if (!found)
        {
            Vector2 r = Random.insideUnitCircle * (minSeparation * 0.5f);
            localPos = new Vector3(r.x, r.y, 0f);
        }

        
        var go = Instantiate(prefab, anchor);
        var targetWorld = anchor.TransformPoint(localPos);

        // I actually want sprinkles to fly in more dramatically than they are now
        Vector3 dir = fromAllDirections
            ? (Vector3)(Random.insideUnitCircle.normalized)
            : Vector3.up;
        Vector3 startWorld = targetWorld + dir * flyDistance;

        
        go.transform.position = startWorld;
        SetAlpha(go, 0f);

       
        float s = Random.Range(0.9f, 1.15f);
        go.transform.localScale = new Vector3(s, s, 1f);
        float startZ = go.transform.eulerAngles.z;
        float spin = spinDegrees * (Random.value < 0.5f ? -1f : 1f);

       
        var srMain = go.GetComponentInChildren<SpriteRenderer>();
        if (srMain) srMain.sortingOrder += 2;

        float t = Random.Range(flyTimeRange.x, flyTimeRange.y);

        Tween.Position(go.transform, targetWorld, t, landEase);

        Tween.LocalRotation(go.transform, Quaternion.Euler(0f, 0f, startZ + spin), t, landEase);

        var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var c = renderers[i].color; c.a = 0f; renderers[i].color = c;
            Tween.Alpha(renderers[i], 1f, t, landEase);
        }
        
        Tween.Delay(t).OnComplete(() =>
        {
            go.transform.localPosition = localPos;
        });
totalSprinkles++;

if (emitPopText) {
    Vector3 worldPos = anchor.TransformPoint(localPos);
    BonusUI.Instance?.PopSprinklesBottomCenter($"{totalSprinkles} Sprinkles!", big: false);
}

    }

    private bool IsFarEnoughFromOthers(Transform anchor, Vector3 candidateLocal)
    {
        for (int i = 0; i < anchor.childCount; i++)
        {
            var child = anchor.GetChild(i);
            if (child == null) continue;
            float d = Vector2.Distance((Vector2)child.localPosition, (Vector2)candidateLocal);
            if (d < minSeparation) return false;
        }
        return true;
    }

    static readonly List<SpriteRenderer> _cache = new List<SpriteRenderer>(8);
    private void SetAlpha(GameObject go, float a)
    {
        _cache.Clear();
        go.GetComponentsInChildren(_cache);
        for (int i = 0; i < _cache.Count; i++)
        {
            var r = _cache[i];
            var c = r.color; c.a = a; r.color = c;
        }
    }
public void SpawnManyAt(Transform candleRoot, int count) {
    if (candleRoot == null) return;
    for (int i = 0; i < count; i++) SpawnAt(candleRoot);
}

public void SpawnManyOnAll(Transform[] candleRoots, int count) {
    if (candleRoots == null) return;
    foreach (var t in candleRoots) SpawnManyAt(t, count);
}

}

