using System.Collections.Generic;
using UnityEngine;

internal class CrowdMoodManager : Singleton<CrowdMoodManager> {
    [SerializeField, Range(0,4)] private int currentLevel = 0; // 0..4
    [SerializeField] private bool autoClamp = true;

    private readonly List<SilhouetteFace> listeners = new List<SilhouetteFace>();

    public int CurrentLevel => currentLevel;

    public void Register(SilhouetteFace face) {
        if (face != null && !listeners.Contains(face)) {
            listeners.Add(face);
            face.ApplyLevel(currentLevel);
        }
    }

    public void Unregister(SilhouetteFace face) {
        if (face != null) listeners.Remove(face);
    }

    public void IncrementMood() {
        SetMood(currentLevel + 1);
    }

    public void SetMood(int level) {
        int newLevel = autoClamp ? Mathf.Clamp(level, 0, 4) : level;
        if (newLevel == currentLevel) return;
        currentLevel = newLevel;
        // update all
        for (int i = 0; i < listeners.Count; i++) {
            var l = listeners[i];
            if (l != null) l.ApplyLevel(currentLevel);
        }
    }

    public void ResetMood() {
        SetMood(0);
    }
}
