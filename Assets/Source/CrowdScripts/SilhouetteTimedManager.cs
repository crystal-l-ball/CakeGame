using System.Collections;
using UnityEngine;

/// Place this on the Crowd parent. It reveals child silhouettes on a timer.
public class SilhouetteTimedManager : MonoBehaviour {
    [Header("Who to reveal (order matters)")]
    [SerializeField] private SilhouetteFade[] silhouettes; // assign your Crowd children in order

    [Header("Sprites to pick from (variants)")]
    [SerializeField] private Sprite[] variantSprites; // your 3 silhouettes

    [Header("Timing")]
    [SerializeField] private float initialDelay = 1.0f;   // wait before first reveal
    [SerializeField] private float interval = 4.0f;       // seconds between reveals
    [SerializeField] private bool stopOnGameOver = true;  // stop when Lose/Win happens

    private bool running;
    private Coroutine loop;

    // Auto-fill silhouettes from children if you hit Reset in the Inspector
    void Reset() {
        silhouettes = GetComponentsInChildren<SilhouetteFade>(true);
    }

    void OnEnable() {
        // Hide them all first
        foreach (var s in silhouettes) if (s) s.HideInstant();

        running = true;
        loop = StartCoroutine(RevealLoop());

        // Stop when the round ends
        if (stopOnGameOver && GameController.Instance != null) {
            GameController.Instance.OnGameOver += HandleGameOver;
        }
    }

    void OnDisable() {
        running = false;
        if (loop != null) StopCoroutine(loop);
        if (stopOnGameOver && GameController.Instance != null) {
            GameController.Instance.OnGameOver -= HandleGameOver;
        }
    }

    void HandleGameOver() {
        running = false;
    }

    IEnumerator RevealLoop() {
        // wait before the first one
        yield return new WaitForSeconds(initialDelay);

        for (int i = 0; running && i < silhouettes.Length; i++) {
            var s = silhouettes[i];
            if (s != null) {
                // pick a random variant sprite if provided
                if (variantSprites != null && variantSprites.Length > 0) {
                    var spr = variantSprites[Random.Range(0, variantSprites.Length)];
                    s.SetSprite(spr);
                }
                s.Show(); 
            }

            // wait until next reveal
            float t = 0f;
            while (running && t < interval) {
                t += Time.deltaTime;
                yield return null;
            }
        }
    }
}
