using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//
// This UI should show up when you achieve bonuses. Not working so lekker at the moment.
//
public enum BonusType { PerfectBlow, CloseSave, TallBoy, Bestie }

public class BonusUI : MonoBehaviour {
    public static BonusUI Instance { get; private set; }

    [Header("Pop Text (spawn TMP text near candles)")]
    [SerializeField] private RectTransform popTextParent;       
    [SerializeField] private TextMeshProUGUI popTextPrefab;     

    [Tooltip("How long a small/normal pop lasts (seconds).")]
    [SerializeField] private float popTime = 0.9f;
    [Tooltip("How much the text scales when 'big'.")]
    [SerializeField] private float bigScale = 1.25f;
    [Tooltip("How much the text scales when 'small'.")]
    [SerializeField] private float smallScale = 1.0f;
    [Tooltip("How far the text drifts upward in UI units.")]
    [SerializeField] private float driftUp = 60f;

    [Header("Stacked Bonus Icons (optional, can be unused)")]
    [SerializeField] private Transform iconBar;        
    [SerializeField] private Image iconPrefab;          

    [Header("Sprites per bonus type")]
    [SerializeField] private Sprite perfectIcon;
    [SerializeField] private Sprite closeSaveIcon;
    [SerializeField] private Sprite tallBoyIcon;
    [SerializeField] private Sprite bestieIcon;

    [Header("Flash-in-place (bottom-left)")]
    [SerializeField] private RectTransform flashAnchor;     
    [SerializeField] private Vector2 flashSize = new Vector2(160, 67);
    [SerializeField] private float flashDuration = 1.1f;
    [SerializeField] private float flashFadeIn = 0.18f;
    [SerializeField] private float flashHold = 0.45f;
    [SerializeField] private float flashStartScale = 0.8f;
    [SerializeField] private float flashPopScale = 1.15f;

    [SerializeField] private float bottomY = 60f; 
    
    [Header("Bonus Text (bottom-left pop)")]
[SerializeField] private float bonusMarginX = 40f;   // number of pixels from the left
[SerializeField] private float bonusMarginY = 40f;   // number of pixels from the bottom
[SerializeField] private bool  bonusBigByDefault = true;


    private Canvas rootCanvas;

    /// All sprinkles are supposed to be visible on screen when the winning confetti comes up. But not when you lose!!

    public void PopBonusBottomLeft(string text, bool? bigOverride = null, Color? colorOverride = null)
{
    if (popTextParent == null || popTextPrefab == null) return;

    
    var tmp  = Instantiate(popTextPrefab, popTextParent); 
    var go   = tmp.gameObject;
    var rect = tmp.rectTransform;

   
    var cg = go.GetComponent<CanvasGroup>();
    if (cg == null) cg = go.AddComponent<CanvasGroup>();

    
    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
    rect.pivot     = new Vector2(0f, 0f);
    rect.anchoredPosition = new Vector2(bonusMarginX, bonusMarginY);

   
    tmp.text = text;
    if (colorOverride.HasValue) tmp.color = colorOverride.Value;

    bool big = bigOverride ?? bonusBigByDefault;
    float baseScale = big ? 1.35f : 1f;

    rect.localScale = Vector3.one * baseScale;
    cg.alpha = 0f;

    
    StartCoroutine(FadeAndPop(tmp, cg, baseScale));
}

    
    public void KeepLastPop()
    {
        keepLast = true;

        
        if (lastRoutine != null)
        {
            StopCoroutine(lastRoutine);
            lastRoutine = null;
        }
        if (lastCg != null) lastCg.alpha = 1f;
        if (lastPop != null) lastPop.rectTransform.localScale = Vector3.one; 
    }


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
    rect.anchoredPosition = new Vector2(0f, bottomY); 

    tmp.text = text;

    var baseScale = big ? 1.35f : 1f;
    rect.localScale = Vector3.one * baseScale;
    cg.alpha = 0f;

  
    lastPop = tmp;
    lastCg  = cg;

   
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
        Instance = this;
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) {
            rootCanvas = Object.FindFirstObjectByType<Canvas>();
        }
    }


    public void PopSprinkles(string msg, Vector3 worldPos, bool big = true, Color? colorOverride = null) {
        if (popTextPrefab == null || popTextParent == null || rootCanvas == null) return;
        var screen = Camera.main ? Camera.main.WorldToScreenPoint(worldPos) : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(popTextParent, screen, rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main, out local);
        SpawnPopText(msg, local, big, colorOverride);
    }


    public void PopSprinklesCenter(string msg, bool big = true, Color? colorOverride = null) {
        if (popTextPrefab == null || popTextParent == null) return;
        SpawnPopText(msg, Vector2.zero, big, colorOverride);
    }

      public void AddBonusIcon(BonusType type, Sprite overrideSprite = null) {
        if (iconBar == null || iconPrefab == null) return;
        var sprite = overrideSprite ?? GetIcon(type);
        if (sprite == null) return;

        var img = Instantiate(iconPrefab, iconBar);
        img.sprite = sprite;
        img.enabled = true;
        img.gameObject.SetActive(true);
    }


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

     
        cg.alpha = 0f;
        img.transform.localScale = Vector3.one * flashStartScale;

        StartCoroutine(FlashRoutine(img, cg));
    }

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

        // hold but drift a bit
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
private bool keepLast; 

    private IEnumerator FlashRoutine(Image img, CanvasGroup cg) {
        float tIn = flashFadeIn;
        float tHold = flashHold;
        float tOut = Mathf.Max(0.05f, flashDuration - (tIn + tHold));

        float t = 0f;
       
        while (t < tIn) {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / tIn);
            cg.alpha = a;
            img.transform.localScale = Vector3.Lerp(Vector3.one * flashStartScale, Vector3.one * flashPopScale, a);
            yield return null;
        }
        cg.alpha = 1f;
        img.transform.localScale = Vector3.one * flashPopScale;

        t = 0f;
        while (t < tHold) {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        
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
