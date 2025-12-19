using UnityEngine;

public class BalloonTimer : MonoBehaviour {
    [Header("Sprite")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private int sortingOrder = 200;      

    [Header("Screen padding (world units)")]
    [SerializeField] private float padX = 0.5f;
    [SerializeField] private float padY = 0.5f;

    [Header("Motion")]
    [SerializeField] private float swayAmplitude = 0.6f;  
    [SerializeField] private float swayFrequency = 0.25f; 
    [SerializeField]
    private AnimationCurve riseCurve =   
        AnimationCurve.Linear(0, 0, 1, 1);
        
        [Header("Horizontal placement")]
[SerializeField, Range(0f, 1f)] private float xFromLeft01 = 0.85f; 


    float duration;     
    float startTime;
    bool  running;

    Vector3 startPos;
    Vector3 endPos;
    float phase;        

    void Reset() {
        sr = GetComponent<SpriteRenderer>();
    }

    void Awake() {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (!sr) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        sr.color = new Color(1,1,1,0); 
    }

    void OnEnable() {
        if (GameController.Instance != null)
            GameController.Instance.OnGameOver += HandleGameOver;
    }

    void OnDisable() {
        if (GameController.Instance != null)
            GameController.Instance.OnGameOver -= HandleGameOver;
    }

    void HandleGameOver() {
        running = false;
    }

    public void ResetBalloon() {
        running = false;
        if (!Camera.main) return;
        ComputeStartEnd(Camera.main, out startPos, out endPos);
        transform.position = startPos;
        SetAlpha(0f);
        gameObject.SetActive(false);
    }

    public void Begin(float totalSeconds) {
        if (!Camera.main) return;
        duration  = Mathf.Max(0.1f, totalSeconds);
        ComputeStartEnd(Camera.main, out startPos, out endPos);
        phase     = Random.value * Mathf.PI * 2f;
        startTime = Time.time;
        running   = true;

        gameObject.SetActive(true);
        SetAlpha(1f);
    }

    void Update() {
        if (!running) return;

        float t = Mathf.Clamp01((Time.time - startTime) / duration);
        float p = riseCurve.Evaluate(t);

        Vector3 pos = Vector3.Lerp(startPos, endPos, p);

        float targetX = startPos.x + Mathf.Sin((Time.time * swayFrequency * 2f * Mathf.PI) + phase) * swayAmplitude;
        pos.x = Mathf.Lerp(pos.x, targetX, 0.75f);

        transform.position = pos;

        if (t >= 1f) running = false; 
    }

    void ComputeStartEnd(Camera cam, out Vector3 bottom, out Vector3 top) {
        if (cam.orthographic) {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            float minX = cam.transform.position.x - halfW + padX;
            float maxX = cam.transform.position.x + halfW - padX;
            float minY = cam.transform.position.y - halfH + padY;
            float maxY = cam.transform.position.y + halfH - padY;

            float x = Mathf.Lerp(minX, maxX, xFromLeft01);

            bottom = new Vector3(x, minY, 0f);
            top    = new Vector3(x, maxY, 0f);

        } else {
            float z = Mathf.Abs(cam.transform.position.z);

            Vector3 leftW  = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, z));
            Vector3 rightW = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, z));

            float minX = leftW.x + padX;
            float maxX = rightW.x - padX;
            float x    = Mathf.Lerp(minX, maxX, xFromLeft01);

            bottom = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, z));
            top    = cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, z));
            bottom.z = top.z = 0f;
            bottom.y += padY; top.y -= padY;

            bottom.x = x;
            top.x    = x;


            float left  = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, z)).x + padX;
            float right = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, z)).x - padX;
            bottom.x = top.x = Mathf.Lerp(left, right, 0.5f);
        }
    }

    void SetAlpha(float a) {
        if (!sr) return;
        var c = sr.color; c.a = a; sr.color = c;
    }
}
