using UnityEngine;
using UnityEngine.InputSystem;   // NEW input system
using System.IO.Ports;

public class ArduinoLedController : MonoBehaviour {
    public static ArduinoLedController Instance { get; private set; }

    [Header("Serial settings")]
    [Tooltip("Windows: COM3, COM6, etc.")]
    [SerializeField] private string portName = "COM6";
    [SerializeField] private int baud = 115200;

    [Header("Debug / bring-up")]
    [SerializeField] private bool enableDebugKeys = true;
    [Tooltip("Send a quick test sequence on Start so you see the LEDs react.")]
    [SerializeField] private bool pingOnStart = true;

    private SerialPort port;
    private bool readyToSend = false;    // Leonardo needs a short moment after open

    private void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        TryOpen();
        // Give Leonardo a moment to come out of reset after opening the port
        Invoke(nameof(MarkReady), 1.0f);   // 1s is safe
        if (pingOnStart) Invoke(nameof(PingTest), 1.2f);
    }

    private void MarkReady() => readyToSend = true;

    private void OnApplicationQuit() { TryClose(); }

private void TryOpen() {
    // First try the configured port
    if (OpenAndPing(portName)) return;

    // Fallback: scan all ports
    foreach (var pn in System.IO.Ports.SerialPort.GetPortNames()) {
        if (pn == portName) continue;
        if (OpenAndPing(pn)) { portName = pn; return; }
    }
    Debug.LogWarning("ArduinoLedController: no responding COM port found.");
}

private bool OpenAndPing(string pn) {
    try {
        var p = new System.IO.Ports.SerialPort(pn, baud) { NewLine = "\n", DtrEnable = true, ReadTimeout = 250 };
        p.Open();
        // small delay for Leonardo reset
        System.Threading.Thread.Sleep(400);
        // ping
        p.Write("P\n");
        string reply = p.ReadLine(); // expect "OK"
        if (reply.Trim() == "OK") {
            // success: keep this port
            if (port != null && port.IsOpen) port.Close();
            port = p;
            Debug.Log($"ArduinoLedController: Connected to {pn} @ {baud}");
            return true;
        }
        p.Close();
    } catch { /* ignore & try next */ }
    return false;
}


    private void TryClose() {
        try {
            if (port != null) {
                if (port.IsOpen) port.Close();
                port.Dispose();
                port = null;
            }
        } catch { }
    }

    private void Send(int candleIndex1to5, char cmd) {
        if (!readyToSend || port == null || !port.IsOpen) return;
        if (candleIndex1to5 < 1 || candleIndex1to5 > 5) return;
        try {
            port.Write(candleIndex1to5.ToString());
            port.Write(cmd.ToString());
            port.Write("\n");
            // uncomment to see every command
            // Debug.Log($"TX -> {candleIndex1to5}{cmd}");
        } catch (System.Exception e) {
            Debug.LogWarning($"ArduinoLedController: send failed: {e.Message}");
        }
    }

    // Public API for your candles
    public void Flicker(int candleIndex1to5) => Send(candleIndex1to5, 'F');
    public void OnSteady(int candleIndex1to5) => Send(candleIndex1to5, 'N');
    public void Off(int candleIndex1to5)      => Send(candleIndex1to5, 'O');

    // Built-in bring-up test (turns LED1: flicker→on→off)
    private void PingTest() {
        Flicker(1);
        Invoke(nameof(_on1), 0.6f);
        Invoke(nameof(_off1), 1.2f);
    }
    private void _on1()  => OnSteady(1);
    private void _off1() => Off(1);

    // NEW INPUT SYSTEM hotkeys (optional)
    private void Update() {
        if (!enableDebugKeys) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) Flicker(1);
        if (kb.digit2Key.wasPressedThisFrame) Flicker(2);
        if (kb.digit3Key.wasPressedThisFrame) Flicker(3);
        if (kb.digit4Key.wasPressedThisFrame) Flicker(4);
        if (kb.digit5Key.wasPressedThisFrame) Flicker(5);

        bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        bool ctrl  = kb.leftCtrlKey.isPressed  || kb.rightCtrlKey.isPressed;

        if (shift && kb.digit1Key.wasPressedThisFrame) OnSteady(1);
        if (shift && kb.digit2Key.wasPressedThisFrame) OnSteady(2);
        if (shift && kb.digit3Key.wasPressedThisFrame) OnSteady(3);
        if (shift && kb.digit4Key.wasPressedThisFrame) OnSteady(4);
        if (shift && kb.digit5Key.wasPressedThisFrame) OnSteady(5);

        if (ctrl && kb.digit1Key.wasPressedThisFrame) Off(1);
        if (ctrl && kb.digit2Key.wasPressedThisFrame) Off(2);
        if (ctrl && kb.digit3Key.wasPressedThisFrame) Off(3);
        if (ctrl && kb.digit4Key.wasPressedThisFrame) Off(4);
        if (ctrl && kb.digit5Key.wasPressedThisFrame) Off(5);
    }
}
