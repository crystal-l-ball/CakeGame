using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//
// If you already defined this enum in another file, delete this block.
//
public enum BonusType { PerfectBlow, CloseSave, TallBoy, Bestie }

public class BonusUI : MonoBehaviour {
    public static BonusUI Instance { get; private set; }

    [Header("Pop Text (spawn TMP text near candles)")]
    [SerializeField] private RectTransform popTextParent;       // full-screen rect under your UI_Bonus canvas
    [SerializeField] private TextMeshProUGUI popTextPrefab;     // prefab with TMP text + (optionally) CanvasGroup

    [Tooltip("How long a small/normal pop lasts (seconds).")]
    [SerializeField] private float popTime = 0.9f;
    [Tooltip("How much the text scales when 'big'.")]
    [SerializeField] private float bigScale = 1.25f;
    [Tooltip("How much the text scales when 'small'.")]
    [SerializeField] private float smallScale = 1.0f;
    [Tooltip("How far the text drifts upward in UI units.")]
    [SerializeField] private float driftUp = 60f;

    [Header("Stacked Bonus Icons (optional, can be unused)")]
    [SerializeField] private Transform iconBar;         // e.g., a HorizontalLayoutGroup
    [SerializeField] private Image iconPrefab;          // small Image prefab (64x64 etc.)

    [Header("Sprites per bonus type")]
    [SerializeField] private Sprite perfectIcon;
    [SerializeField] private Sprite closeSaveIcon;
    [SerializeField] private Sprite tallBoyIcon;
    [SerializeField] private Sprite bestieIcon;

    [Header("Flash-in-place (bottom-left)")]
    [SerializeField] private RectTransform flashAnchor;     // ← assign BonusFlashAnchor
    [SerializeField] private Vector2 flashSize = new Vector2(160, 67);
    [SerializeField] private float flashDuration = 1.1f;
    [SerializeField] private float flashFadeIn = 0.18f;
    [SerializeField] private float flashHold = 0.45f;
    [SerializeField] private float flashStartScale = 0.8f;
    [SerializeField] private float flashPopScale = 1.15f;

    [SerializeField] private float bottomY = 60f; // tweak in Inspector
    
    [Header("Bonus Text (bottom-left pop)")]
[SerializeField] private float bonusMarginX = 40f;   // pixels from left
[SerializeField] private float bonusMarginY = 40f;   // pixels from bottom
[SerializeField] private bool  bonusBigByDefault = true;


    private Canvas rootCanvas;

    /// Call when entering Win to keep the latest Sprinkles popup on screen.

    public void PopBonusBottomLeft(string text, bool? bigOverride = null, Color? colorOverride = null)
{
    if (popTextParent == null || popTextPrefab == null) return;

    // Instantiate TMP object
    var tmp  = Instantiate(popTextPrefab, popTextParent); // TextMeshProUGUI
    var go   = tmp.gameObject;
    var rect = tmp.rectTransform;

    // Ensure CanvasGroup exists for fading
    var cg = go.GetComponent<CanvasGroup>();
    if (cg == null) cg = go.AddComponent<CanvasGroup>();

    // Anchor to bottom-left
    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
    rect.pivot     = new Vector2(0f, 0f);
    rect.anchoredPosition = new Vector2(bonusMarginX, bonusMarginY);

    // Set text and initial state
    tmp.text = text;
    if (colorOverride.HasValue) tmp.color = colorOverride.Value;

    bool big = bigOverride ?? bonusBigByDefault;
    float baseScale = big ? 1.35f : 1f;

    rect.localScale = Vector3.one * baseScale;
    cg.alpha = 0f;

    // Animate & auto-destroy (reuses the same animation style)
    StartCoroutine(FadeAndPop(tmp, cg, baseScale));
}

    
    public void KeepLastPop()
    {
        keepLast = true;

        // If the last popup is mid-animation, stop its coroutine and make it fully visible
        if (lastRoutine != null)
        {
            StopCoroutine(lastRoutine);
            lastRoutine = null;
        }
        if (lastCg != null) lastCg.alpha = 1f;
        if (lastPop != null) lastPop.rectTransform.localScale = Vector3.one; // or keep current scale
    }

/// Call when resetting (e.g., EnterIdle) to remove the pinned popup.
public void ClearPinnedPop()
{
    keepLast = false;
    if (lastRoutine != null) {
        StopCoroutine(lastRoutine);
        lastRoutine = null;
    }
    if (lastPop != null) Destroy(lastPop.gameObject);
    lastPop = null; lastCg = null;
}

public void PopSprinklesBottomCenter(string text, bool big = false)
{
    if (popTextParent == null || popTextPrefab == null) return;

    // If we’re not keeping the previous one, clean it up before spawning a new
    if (!keepLast && lastPop != null) {
        if (lastRoutine != null) { StopCoroutine(lastRoutine); lastRoutine = null; }
        if (lastPop != null) Destroy(lastPop.gameObject);
        lastPop = null; lastCg = null;
    }

    var tmp  = Instantiate(popTextPrefab, popTextParent);
    var go   = tmp.gameObject;
    var rect = tmp.rectTransform;

    var cg = go.GetComponent<CanvasGroup>();
    if (cg == null) cg = go.AddComponent<CanvasGroup>();

    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
    rect.pivot     = new Vector2(0.5f, 0.5f);
    rect.anchoredPosition = new Vector2(0f, bottomY); // or your hardcoded value

    tmp.text = text;

    var baseScale = big ? 1.35f : 1f;
    rect.localScale = Vector3.one * baseScale;
    cg.alpha = 0f;

    // Remember this as “last”
    lastPop = tmp;
    lastCg  = cg;

    // Store the coroutine so we can stop it when pinning
    lastRoutine = StartCoroutine(FadeAndPop(tmp, cg, baseScale));
}



private IEnumerator FadeAndPop(TextMeshProUGUI tmp, CanvasGroup cg, float baseScale)
{
    if (tmp == null || cg == null) yield break;
    var rect = tmp.rectTransform;

    const float tIn = 0.15f;
    const float tHold = 0.35f;
    const float tOut = 0.25f;

    float t = 0f;
    while (t < tIn) {
        t += Time.unscaledDeltaTime;
        float a = Mathf.Clamp01(t / tIn);
        cg.alpha = a;
        rect.localScale = Vector3.one * (baseScale * (1f + 0.08f * a));
        yield return null;
    }
    cg.alpha = 1f;
    rect.localScale = Vector3.one * (baseScale * 1.08f);

    yield return new WaitForSecondsRealtime(tHold);

    // If we decided to keep the last popup AND this is the last one, stop here (don’t fade out or destroy)
    if (keepLast && tmp == lastPop) {
        lastRoutine = null;
        yield break;
    }

    t = 0f;
    while (t < tOut) {
        t += Time.unscaledDeltaTime;
        float a = Mathf.Clamp01(t / tOut);
        cg.alpha = 1f - a;
        yield return null;
    }

    if (tmp != null) Destroy(tmp.gameObject);
    if (tmp == lastPop) { lastPop = null; lastCg = null; lastRoutine = null; }
}


    private void Awake() {
        // Simple singleton (last one wins). Keep UI_Bonus enabled in all states.
        Instance = this;
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) {
            rootCanvas = Object.FindFirstObjectByType<Canvas>();
        }
    }

    // -------------------- PUBLIC API --------------------

    /// <summary>
    /// Pop a "Sprinkles!" text near a world position (e.g., at a candle).
    /// </summary>
    public void PopSprinkles(string msg, Vector3 worldPos, bool big = true, Color? colorOverride = null) {
        if (popTextPrefab == null || popTextParent == null || rootCanvas == null) return;
        var screen = Camera.main ? Camera.main.WorldToScreenPoint(worldPos) : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(popTextParent, screen, rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main, out local);
        SpawnPopText(msg, local, big, colorOverride);
    }

    /// <summary>
    /// Pop a text dead-center of the screen (handy for testing).
    /// </summary>
    public void PopSprinklesCenter(string msg, bool big = true, Color? colorOverride = null) {
        if (popTextPrefab == null || popTextParent == null) return;
        SpawnPopText(msg, Vector2.zero, big, colorOverride);
    }

    /// <summary>
    /// Optional legacy: add an icon to a stacked bar (top/right/etc).
    /// </summary>
    public void AddBonusIcon(BonusType type, Sprite overrideSprite = null) {
        if (iconBar == null || iconPrefab == null) return;
        var sprite = overrideSprite ?? GetIcon(type);
        if (sprite == null) return;

        var img = Instantiate(iconPrefab, iconBar);
        img.sprite = sprite;
        img.enabled = true;
        img.gameObject.SetActive(true);
    }

    /// <summary>
    /// Show a brief icon flash at the bottom-left anchor (same spot every time).
    /// </summary>
    public void FlashBonus(BonusType type, Sprite overrideSprite = null) {
        if (flashAnchor == null || iconPrefab == null) return;
        var sprite = overrideSprite ?? GetIcon(type);
        if (sprite == null) return;

        var img = Instantiate(iconPrefab, flashAnchor);
        var rt  = img.rectTransform;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = flashSize;
        img.sprite = sprite;
        img.enabled = true;

        var cg = img.GetComponent<CanvasGroup>();
        if (cg == null) cg = img.gameObject.AddComponent<CanvasGroup>();

        // start hidden & small
        cg.alpha = 0f;
        img.transform.localScale = Vector3.one * flashStartScale;

        StartCoroutine(FlashRoutine(img, cg));
    }

    // -------------------- INTERNALS --------------------

    private Sprite GetIcon(BonusType t) {
        switch (t) {
            case BonusType.PerfectBlow: return perfectIcon;
            case BonusType.CloseSave:   return closeSaveIcon;
            case BonusType.TallBoy:     return tallBoyIcon;
            case BonusType.Bestie:      return bestieIcon;
        }
        return null;
    }

    private void SpawnPopText(string msg, Vector2 anchoredPos, bool big, Color? colorOverride) {
        var go = Instantiate(popTextPrefab, popTextParent);
        var rt = go.rectTransform;
        rt.anchoredPosition = anchoredPos;

        go.text = msg;
        if (colorOverride.HasValue) go.color = colorOverride.Value;

        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.gameObject.AddComponent<CanvasGroup>();

        // Initial state
        cg.alpha = 0f;
        go.transform.localScale = Vector3.one * (big ? bigScale : smallScale);

        StartCoroutine(PopTextRoutine(go, cg, anchoredPos));
    }

    private IEnumerator PopTextRoutine(TextMeshProUGUI tmp, CanvasGroup cg, Vector2 startPos) {
        float tIn = Mathf.Clamp(popTime * 0.25f, 0.08f, 0.25f);   // quick fade in
        float tHold = Mathf.Clamp(popTime * 0.35f, 0.2f, 0.5f);
        float tOut = Mathf.Max(0.05f, popTime - (tIn + tHold));

        float t = 0f;
        // fade in
        while (t < tIn) {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / tIn);
            cg.alpha = a;
            tmp.rectTransform.anchoredPosition = startPos + Vector2.up * (driftUp * a * 0.25f);
            yield return null;
        }
        cg.alpha = 1f;

        // hold (drift a bit)
        t = 0f;
        while (t < tHold) {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / tHold);
            tmp.rectTransform.anchoredPosition = startPos + Vector2.up * (driftUp * (0.25f + 0.5f * a));
            yield return null;
        }

        // fade out (finish drift)
        t = 0f;
        while (t < tOut) {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / tOut);
            cg.alpha = 1f - a;
            tmp.rectTransform.anchoredPosition = startPos + Vector2.up * (driftUp * (0.75f + 0.25f * a));
            yield return null;
        }

        Destroy(tmp.gameObject);
    }

// Track the most recent popup so we can pin it on Win
private TextMeshProUGUI lastPop;
private CanvasGroup lastCg;
private Coroutine lastRoutine;
private bool keepLast; // when true, the last pop stays on screen

    private IEnumerator FlashRoutine(Image img, CanvasGroup cg) {
        float tIn = flashFadeIn;
        float tHold = flashHold;
        float tOut = Mathf.Max(0.05f, flashDuration - (tIn + tHold));

        float t = 0f;
        // fade in + pop scale
        while (t < tIn) {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / tIn);
            cg.alpha = a;
            img.transform.localScale = Vector3.Lerp(Vector3.one * flashStartScale, Vector3.one * flashPopScale, a);
            yield return null;
        }
        cg.alpha = 1f;
        img.transform.localScale = Vector3.one * flashPopScale;

        // hold
        t = 0f;
        while (t < tHold) {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // fade out and settle to 1.0 scale
        t = 0f;
        while (t < tOut) {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / tOut);
            cg.alpha = 1f - a;
            img.transform.localScale = Vector3.Lerp(Vector3.one * flashPopScale, Vector3.one, a);
            yield return null;
        }

        Destroy(img.gameObject);
    }
}
