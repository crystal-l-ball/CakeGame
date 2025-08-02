using UnityEngine;

internal class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    public static T Instance { get; private set; }

    private void Awake()
    {
        Instance = (T)this;
    }
}
