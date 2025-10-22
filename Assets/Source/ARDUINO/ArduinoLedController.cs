using System.IO.Ports;
using UnityEngine;

public class ArduinoLedController : MonoBehaviour {
    public static ArduinoLedController Instance { get; private set; }

    [Header("Serial settings")]
    [Tooltip("Windows: COM3, COM6, etc.  macOS: /dev/tty.usbmodemXXXX  Linux: /dev/ttyACM0")]
    [SerializeField] private string portName = "COM6";
    [SerializeField] private int baud = 115200;

    [Header("Optional debug keys (Play Mode)")]
    [SerializeField] private bool enableDebugKeys = true;

    private SerialPort port;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // optional
    }

    private void Start() {
        TryOpen();
    }

    private void OnApplicationQuit() {
        TryClose();
    }

    private void TryOpen() {
        try {
            if (port != null && port.IsOpen) return;
            port = new SerialPort(portName, baud) {
                NewLine = "\n",
                DtrEnable = true
            };
            port.Open();
            Debug.Log($"ArduinoLedController: Opened {portName} @ {baud}");
        } catch (System.Exception e) {
            Debug.LogWarning($"ArduinoLedController: can't open {portName}: {e.Message}");
        }
    }

    private void TryClose() {
        try {
            if (port != null) {
                if (port.IsOpen) port.Close();
                port.Dispose();
                port = null;
            }
        } catch { /* ignore */ }
    }

    private void Send(int candleIndex1to5, char cmd) {
        if (port == null || !port.IsOpen) return;
        if (candleIndex1to5 < 1 || candleIndex1to5 > 5) return;

        try {
            port.Write(candleIndex1to5.ToString());
            port.Write(cmd.ToString());
            port.Write("\n");
        } catch (System.Exception e) {
            Debug.LogWarning($"ArduinoLedController: send failed: {e.Message}");
        }
    }

    // Public API used by CandleBehaviour
    public void Flicker(int candleIndex1to5) => Send(candleIndex1to5, 'F');
    public void OnSteady(int candleIndex1to5) => Send(candleIndex1to5, 'N');
    public void Off(int candleIndex1to5)      => Send(candleIndex1to5, 'O');

    // Optional keyboard pokes to test LEDs without candles
    private void Update() {
        if (!enableDebugKeys) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) Flicker(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Flicker(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Flicker(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) Flicker(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) Flicker(5);

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
            if (Input.GetKeyDown(KeyCode.Alpha1)) OnSteady(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) OnSteady(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) OnSteady(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) OnSteady(4);
            if (Input.GetKeyDown(KeyCode.Alpha5)) OnSteady(5);
        }
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Off(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Off(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Off(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) Off(4);
            if (Input.GetKeyDown(KeyCode.Alpha5)) Off(5);
        }
    }
}
