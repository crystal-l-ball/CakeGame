using UnityEngine;

internal class DecorationManager : Singleton<DecorationManager>
{
    [SerializeField] private GameObject[] decorationPrefabs;

    [Header("Spawn Scatter (WORLD units)")]
    [SerializeField, Min(0f)] private float spawnRadius = 0.12f;     // world radius around the anchor
    [SerializeField, Min(0f)] private float minSeparation = 0.05f;   // world min distance between toppings
    [SerializeField, Range(1, 30)] private int maxTries = 12;        // attempts to find a non-overlapping spot

    /// <summary>
    /// Spawn one random decoration around this candle's DecorationAnchor (creates the anchor if missing).
    /// Placement is done in WORLD SPACE so parent scale/rotation won't distort it.
    /// </summary>
    public void SpawnAt(Transform candleRoot)
    {
        if (candleRoot == null || decorationPrefabs == null || decorationPrefabs.Length == 0) return;

        // Find (or create) the anchor under this candle
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

        // Choose a prefab
        var prefab = decorationPrefabs[Random.Range(0, decorationPrefabs.Length)];
        if (prefab == null) return;

        // ---- WORLD-SPACE placement around the anchor ----
        Vector3 chosenWorld = anchor.position;
        bool found = false;
        for (int i = 0; i < maxTries; i++)
        {
            Vector2 r = Random.insideUnitCircle * spawnRadius;   // radius in world units
            Vector3 candidateWorld = anchor.position + new Vector3(r.x, r.y, 0f);

            if (IsFarEnoughFromOthersWorld(anchor, candidateWorld))
            {
                chosenWorld = candidateWorld;
                found = true;
                break;
            }
        }
        if (!found)
        {
            // fallback: tiny jitter near center (world units)
            Vector2 r = Random.insideUnitCircle * (minSeparation * 0.5f);
            chosenWorld = anchor.position + new Vector3(r.x, r.y, 0f);
        }

        // Instantiate unparented in world, then parent while keeping world position
        var decorGO = Instantiate(prefab);
        decorGO.transform.position = chosenWorld;
        decorGO.transform.rotation = Quaternion.identity;
        decorGO.transform.localScale = Vector3.one; // neutral
        decorGO.transform.SetParent(anchor, true);  // keep world pos/rot/scale

        // Small random variety
        float s = Random.Range(0.9f, 1.15f);
        decorGO.transform.localScale *= s;
        decorGO.transform.Rotate(0f, 0f, Random.Range(0f, 360f));

        // Ensure toppings render above cake
        var sr = decorGO.GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.sortingOrder += 2;
    }

    // Distance check in WORLD space (matches the new placement)
    private bool IsFarEnoughFromOthersWorld(Transform anchor, Vector3 candidateWorld)
    {
        for (int i = 0; i < anchor.childCount; i++)
        {
            var child = anchor.GetChild(i);
            if (!child) continue;
            float d = Vector2.Distance((Vector2)child.position, (Vector2)candidateWorld);
            if (d < minSeparation) return false;
        }
        return true;
    }
}
