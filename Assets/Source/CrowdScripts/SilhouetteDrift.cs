using UnityEngine;

internal class SilhouetteDrift : MonoBehaviour {
    [Header("Horizontal sway")]
    public float swayXAmplitude = 0.12f;   // world units
    public float swayXFrequency = 0.6f;    // cycles per second

    [Header("Vertical bob (optional)")]
    public float swayYAmplitude = 0.04f;   // world units
    public float swayYFrequency = 0.45f;   // cycles per second

    Vector3 basePos;
    float phaseX, phaseY;

    void OnEnable() {
        basePos = transform.position;
        // per-instance random phases so they don't sync
        phaseX = Random.value * Mathf.PI * 2f;
        phaseY = Random.value * Mathf.PI * 2f;
    }

    void Update() {
        float t = Time.time;
        float x = basePos.x + Mathf.Sin((t * swayXFrequency * 2f * Mathf.PI) + phaseX) * swayXAmplitude;
        float y = basePos.y + Mathf.Sin((t * swayYFrequency * 2f * Mathf.PI) + phaseY) * swayYAmplitude;
        transform.position = new Vector3(x, y, basePos.z);
    }
}
