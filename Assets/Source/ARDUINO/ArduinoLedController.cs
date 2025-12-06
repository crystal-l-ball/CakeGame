#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using System;
using System.IO.Ports;
using System.Collections;
using UnityEngine;

public class ArduinoLedController : MonoBehaviour {
    public static ArduinoLedController Instance { get; private set; }
    [Header("Serial settings")]
    [SerializeField] private string preferredPort = "COM5";
    [SerializeField] private int baud = 115200;
    [SerializeField] private bool onlyUsePreferred = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugKeys = true;
    [SerializeField] private bool pingOnConnect = false;   // keep OFF for Leonardo stability
    [SerializeField] private int  postOpenDelayMs = 1200;  // Leonardo needs a longer wait after DTR

    private SerialPort port;
    private bool connected;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        StartCoroutine(OpenOnce());
    }

    private IEnumerator OpenOnce() {
        yield return null; // let one frame pass

        SafeClose();
        try {
            // Create then configure (safer across Unity/C# versions)
            var sp = new SerialPort(preferredPort, baud);
            sp.NewLine      = "\n";
            sp.DtrEnable    = true;   // toggles reset on Leonardo
            sp.RtsEnable    = false;
            sp.ReadTimeout  = 1000;
            sp.WriteTimeout = 300;

            sp.Open();
            port = sp;

            // Leonardo re-enumerates after DTR: wait a bit longer
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < (postOpenDelayMs / 1000f)) { /* busy wait */ }

            if (pingOnConnect) {
                try {
                    port.DiscardInBuffer();
                    port.Write("P\n");
                    string reply = port.ReadLine().Trim();
                    if (reply != "OK") Debug.LogWarning($"Arduino: ping got '{reply}'");
                } catch (Exception ex) {
                    Debug.LogWarning($"Arduino: ping failed: {ex.Message}");
                }
            }

            connected = true;
            Debug.Log($"Arduino: connected on {preferredPort}");
        }
        catch (Exception e) {
            connected = false;
            Debug.LogWarning($"Arduino: open {preferredPort} failed: {e.Message}");
            SafeClose();
        }
    }

    private void OnApplicationQuit() => SafeClose();
    private void OnDestroy()         => SafeClose();

    private void SafeClose() {
        try { if (port != null && port.IsOpen) port.Close(); } catch {}
        try { if (port != null) port.Dispose(); } catch {}
        port = null;
        connected = false;
    }

    // ------------ SEND ------------
    private void Send(int idx, char cmd)
    {
        if (!connected || port == null || !port.IsOpen) return;
        if (idx < 1 || idx > 5) return;
        try
        {
            port.Write(idx.ToString());
            port.Write(cmd.ToString());
            port.Write("\n");
            Debug.Log($"TX {idx}{cmd}");
        }
        catch (Exception)
        {
            SafeClose(); // drop connection if write failed
        }
    }
    
        public void LedFlicker(int idx) => Send(idx, 'F'); // “ignite” (flicker)
    public void LedOn(int idx)      => Send(idx, 'N'); // steady on
    public void LedOff(int idx)     => Send(idx, 'O'); // off

    // ------------ Debug keys (hold Shift/Ctrl, then tap number) ------------
    private void Update() {
    if (!enableDebugKeys) return;

#if ENABLE_INPUT_SYSTEM
    var kb = Keyboard.current;
    if (kb == null) return;

    // 1
    if (kb.digit1Key.wasPressedThisFrame) Send(1, 'F');
    if (kb.leftShiftKey.isPressed && kb.digit1Key.wasPressedThisFrame) Send(1, 'N');
    if (kb.leftCtrlKey .isPressed && kb.digit1Key.wasPressedThisFrame) Send(1, 'O');

    // 2
    if (kb.digit2Key.wasPressedThisFrame) Send(2, 'F');
    if (kb.leftShiftKey.isPressed && kb.digit2Key.wasPressedThisFrame) Send(2, 'N');
    if (kb.leftCtrlKey .isPressed && kb.digit2Key.wasPressedThisFrame) Send(2, 'O');

    // 3
    if (kb.digit3Key.wasPressedThisFrame) Send(3, 'F');
    if (kb.leftShiftKey.isPressed && kb.digit3Key.wasPressedThisFrame) Send(3, 'N');
    if (kb.leftCtrlKey .isPressed && kb.digit3Key.wasPressedThisFrame) Send(3, 'O');

    // 4
    if (kb.digit4Key.wasPressedThisFrame) Send(4, 'F');
    if (kb.leftShiftKey.isPressed && kb.digit4Key.wasPressedThisFrame) Send(4, 'N');
    if (kb.leftCtrlKey .isPressed && kb.digit4Key.wasPressedThisFrame) Send(4, 'O');

    // 5
    if (kb.digit5Key.wasPressedThisFrame) Send(5, 'F');
    if (kb.leftShiftKey.isPressed && kb.digit5Key.wasPressedThisFrame) Send(5, 'N');
    if (kb.leftCtrlKey .isPressed && kb.digit5Key.wasPressedThisFrame) Send(5, 'O');

#else
    // Legacy Input Manager (works if Active Input Handling = Both or Old)
    if (Input.GetKeyDown(KeyCode.Alpha1)) Send(1, 'F');
    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Alpha1)) Send(1, 'N');
    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha1)) Send(1, 'O');

    if (Input.GetKeyDown(KeyCode.Alpha2)) Send(2, 'F');
    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Alpha2)) Send(2, 'N');
    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha2)) Send(2, 'O');

    if (Input.GetKeyDown(KeyCode.Alpha3)) Send(3, 'F');
    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Alpha3)) Send(3, 'N');
    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha3)) Send(3, 'O');

    if (Input.GetKeyDown(KeyCode.Alpha4)) Send(4, 'F');
    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Alpha4)) Send(4, 'N');
    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha4)) Send(4, 'O');

    if (Input.GetKeyDown(KeyCode.Alpha5)) Send(5, 'F');
    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Alpha5)) Send(5, 'N');
    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha5)) Send(5, 'O');
#endif
}

}
