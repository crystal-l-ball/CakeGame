using UnityEngine;

internal class SilhouetteFace : MonoBehaviour {
    [SerializeField] private Sprite[] moodSprites = new Sprite[5]; // 5 faces, least→most happy
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.60f, 0f);
    [SerializeField] private int sortingOrderOffset = 5;

    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer faceRenderer;

    void Awake() {
        if (!bodyRenderer) bodyRenderer = GetComponent<SpriteRenderer>();
        EnsureFaceObject();
    }

    void OnEnable()  { CrowdMoodManager.Instance?.Register(this); }
    void OnDisable() { CrowdMoodManager.Instance?.Unregister(this); }

    public void Setup(SpriteRenderer body, Sprite[] faceSet, Vector3 offset, int orderOffset) {
        bodyRenderer = body;
        if (faceSet != null && faceSet.Length >= 5) moodSprites = faceSet;
        localOffset = offset;
        sortingOrderOffset = orderOffset;
        EnsureFaceObject();
    }

    public void ApplyLevel(int level) {
        if (!faceRenderer) return;
        int idx = Mathf.Clamp(level, 0, 4);
        if (moodSprites != null && moodSprites.Length > idx)
            faceRenderer.sprite = moodSprites[idx];

        // above body & at the head
        if (bodyRenderer) faceRenderer.sortingOrder = bodyRenderer.sortingOrder + sortingOrderOffset;
        faceRenderer.transform.localPosition = localOffset;
        faceRenderer.color = Color.white;
    }

    void EnsureFaceObject() {
        if (!faceRenderer) {
            var t = transform.Find("Face");
            if (!t) {
                t = new GameObject("Face").transform;
                t.SetParent(transform, false);
            }
            faceRenderer = t.GetComponent<SpriteRenderer>() ?? t.gameObject.AddComponent<SpriteRenderer>();
        }
        faceRenderer.transform.localPosition = localOffset;
        if (bodyRenderer) faceRenderer.sortingOrder = bodyRenderer.sortingOrder + sortingOrderOffset;
    }
}
