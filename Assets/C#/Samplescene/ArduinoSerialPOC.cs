using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

public class ArduinoSerialPOC : MonoBehaviour
{
    [Header("Serial Settings")]
    public string portName = "COM8";              // ⚠️ 記得改成你實際的 COM 號碼
    public int baudRate = 115200;                 // 🌟 配合 Arduino 改成 115200 降低延遲

    [Header("Button Mapping")]
    public string defaultButtonName = "BUTTON";

    private static ArduinoSerialPOC instance;

    private SerialPort serialPort;
    private Thread readThread;
    private bool isRunning;

    // ==========================================
    // 公開的靜態屬性，讓 HitManager 可以直接讀取
    // ==========================================
    public static float JoystickX { get; private set; }
    public static float JoystickY { get; private set; }
    public static string JoystickDirection { get; private set; } = "CENTER"; // 🌟 新增：字串方向

    private readonly object buttonStateLock = new object();
    private readonly Dictionary<string, bool> rawButtonStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> buttonStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> previousButtonStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    // (省略部分與原本相同的 GetButton 相關方法，維持不變)
    public static bool GetButton(string buttonName = null)
    {
        if (instance == null) return false;
        return instance.GetButtonState(instance.buttonStates, buttonName);
    }

    public static bool GetButtonDown(string buttonName = null)
    {
        if (instance == null) return false;
        string resolvedButtonName = instance.ResolveButtonName(buttonName);
        bool previous = instance.GetButtonState(instance.previousButtonStates, resolvedButtonName);
        bool current = instance.GetButtonState(instance.buttonStates, resolvedButtonName);
        return !previous && current;
    }

    public static bool GetButtonUp(string buttonName = null)
    {
        if (instance == null) return false;
        string resolvedButtonName = instance.ResolveButtonName(buttonName);
        bool previous = instance.GetButtonState(instance.previousButtonStates, resolvedButtonName);
        bool current = instance.GetButtonState(instance.buttonStates, resolvedButtonName);
        return previous && !current;
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 100;

        try
        {
            serialPort.Open();
            isRunning = true;
            readThread = new Thread(ReadSerialLoop);
            readThread.Start();
            Debug.Log("✅ Serial connected: " + portName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Serial open failed: " + e.Message);
        }
    }

    void Update()
    {
        SyncButtonStates();

        // 方便測試：在 Console 印出按下的按鈕
        foreach (var key in buttonStates.Keys)
        {
            if (GetButtonDown(key)) Debug.Log("🕹️ 實體按鈕按下: " + key);
            if (GetButtonUp(key)) Debug.Log("🕹️ 實體按鈕放開: " + key);
        }
    }

    void ReadSerialLoop()
    {
        while (isRunning && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string message = serialPort.ReadLine();
                message = message.Trim();

                if (TryParseButtonMessage(message, out string buttonName, out bool isPressed))
                {
                    SetRawButtonState(buttonName, isPressed);
                }
            }
            catch (System.TimeoutException) { /* ignore */ }
            catch (System.Exception) { /* ignore */ }
        }
    }

    void OnDestroy() { Shutdown(); }
    void OnApplicationQuit() { Shutdown(); }

    private void Shutdown()
    {
        isRunning = false;
        if (readThread != null && readThread.IsAlive) readThread.Join();
        if (serialPort != null && serialPort.IsOpen) serialPort.Close();
        if (instance == this) instance = null;
    }

    private void SyncButtonStates()
    {
        previousButtonStates.Clear();
        foreach (var pair in buttonStates) previousButtonStates[pair.Key] = pair.Value;
        lock (buttonStateLock)
        {
            foreach (var pair in rawButtonStates) buttonStates[pair.Key] = pair.Value;
        }
    }

    private void SetRawButtonState(string buttonName, bool isPressed)
    {
        string resolvedButtonName = ResolveButtonName(buttonName);
        lock (buttonStateLock)
        {
            rawButtonStates[resolvedButtonName] = isPressed;
        }
    }

    private bool GetButtonState(Dictionary<string, bool> states, string buttonName)
    {
        string resolvedButtonName = ResolveButtonName(buttonName);
        return states.TryGetValue(resolvedButtonName, out bool isPressed) && isPressed;
    }

    private string ResolveButtonName(string buttonName)
    {
        return string.IsNullOrWhiteSpace(buttonName) ? defaultButtonName : buttonName.Trim();
    }

    private bool TryParseButtonMessage(string message, out string buttonName, out bool isPressed)
    {
        buttonName = null;
        isPressed = false;

        if (string.IsNullOrWhiteSpace(message)) return false;

        // 🌟 新增：攔截 Arduino 傳來的文字方向 (例如 DIR_UP)
        if (message.StartsWith("DIR_"))
        {
            JoystickDirection = message.Substring(4); // 取得 "UP", "DOWN" 等
            return false; 
        }

        // 原本的攔截類比訊號
        if (message.StartsWith("JOY_X_") || message.StartsWith("JOY_Y_"))
        {
            string[] joyParts = message.Split('_');
            if (joyParts.Length == 3 && float.TryParse(joyParts[2], out float rawVal))
            {
                float normalized = (rawVal - 512f) / 512f;
                if (Mathf.Abs(normalized) < 0.15f) normalized = 0f; 
                if (joyParts[1] == "X") JoystickX = normalized;
                if (joyParts[1] == "Y") JoystickY = normalized;
            }
            return false; 
        }

        // 原本的按鈕 UP/DOWN 邏輯 (解析 J_DOWN, K_UP 等)
        string[] parts = message.Split('_');
        if (parts.Length < 2) return false;

        string action = parts[parts.Length - 1];
        if (action.Equals("DOWN", StringComparison.OrdinalIgnoreCase)) isPressed = true;
        else if (action.Equals("UP", StringComparison.OrdinalIgnoreCase)) isPressed = false;
        else return false;

        if (parts.Length == 2) { buttonName = parts[0]; return true; }

        buttonName = string.Join("_", parts, 0, parts.Length - 1);
        return true;
    }
}