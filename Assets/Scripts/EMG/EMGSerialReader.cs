using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class EMGSerialReader : MonoBehaviour
{
    [Header("Serial Settings")]
    [Tooltip("ESP32가 연결된 포트 이름. Windows: COM3 / Mac: /dev/cu.usbserial-XXXX")]
    public string portName = "/dev/cu.usbserial-1110";

    [Tooltip("ESP32 코드의 Serial.begin() 값과 동일하게 설정")]
    public int baudRate = 115200;

    [Header("Calibration (사람마다 다름 — 수집한 rest/grip 평균값을 넣기)")]
    [Tooltip("힘 뺀 상태(rest)의 raw 평균값")]
    public int restBaseline = 18;

    [Tooltip("최대 수축(grip) 상태의 raw 평균값")]
    public int maxContraction = 1321;

    [Header("Threshold (0~1, restBaseline~maxContraction 정규화 값 기준)")]
    [Tooltip("이 비율 이상이면 REST → LIGHT")]
    [Range(0f, 1f)] public float thresholdLight = 0.1f;

    [Tooltip("이 비율 이상이면 LIGHT → STRONG")]
    [Range(0f, 1f)] public float thresholdStrong = 0.4f;

    [Tooltip("이 비율 이상이면 STRONG → GRIP")]
    [Range(0f, 1f)] public float thresholdGrip = 0.7f;

    public static int    CurrentValue = 0;
    public static float  CurrentNormalized = 0f;
    public static string CurrentState = "REST";

    // ML 분류기(EMGMLClassifier)가 특징 추출에 쓰는 최근 raw 샘플 윈도우 (features.py의 WIN=20과 동일)
    public const int WindowSize = 20;
    private static readonly int[] _window = new int[WindowSize];
    private static int _windowIndex = 0;
    private static int _windowFilled = 0;

    private SerialPort  _port;
    private Thread      _thread;
    private bool        _running;
    private string      _latestLine = "";
    private readonly object _lock = new object();

    void Start()
    {
        _port = new SerialPort(portName, baudRate) { ReadTimeout = 100 };
        try
        {
            _port.Open();
            _running = true;
            _thread = new Thread(ReadThread) { IsBackground = true };
            _thread.Start();
            Debug.Log("[EMG] Serial opened: " + portName);
        }
        catch (Exception e)
        {
            Debug.LogError("[EMG] Cannot open serial: " + e.Message);
        }
    }

    void ReadThread()
    {
        while (_running && _port.IsOpen)
        {
            try
            {
                string line = _port.ReadLine().Trim();
                lock (_lock) { _latestLine = line; }
            }
            catch (TimeoutException) { }
            catch (Exception e)
            {
                Debug.LogWarning("[EMG] Read error: " + e.Message);
            }
        }
    }

    void Update()
    {
        string line;
        lock (_lock) { line = _latestLine; _latestLine = ""; }
        if (string.IsNullOrEmpty(line)) return;

        // 아두이노가 "시간ms,raw" 형식(collect.py와 동일)으로 보내므로 콤마 있으면 두 번째 값만 사용.
        // 콤마 없이 raw 하나만 오는 펌웨어와도 호환되도록 둘 다 처리.
        string[] parts = line.Split(',');
        string rawPart = parts.Length >= 2 ? parts[1] : parts[0];

        if (int.TryParse(rawPart, out int val))
        {
            CurrentValue = val;
            CurrentNormalized = Normalize(val);
            CurrentState = ClassifyState(CurrentNormalized);

            _window[_windowIndex] = val;
            _windowIndex = (_windowIndex + 1) % WindowSize;
            if (_windowFilled < WindowSize) _windowFilled++;
        }
    }

    /// <summary>
    /// 최근 WindowSize개의 raw 샘플을 오래된 순서대로 반환. 아직 다 안 찼으면 false.
    /// </summary>
    public static bool TryGetWindow(out int[] window)
    {
        if (_windowFilled < WindowSize) { window = null; return false; }
        window = new int[WindowSize];
        for (int i = 0; i < WindowSize; i++)
            window[i] = _window[(_windowIndex + i) % WindowSize];
        return true;
    }

    private float Normalize(int value)
    {
        int range = maxContraction - restBaseline;
        if (range <= 0) return 0f;
        return Mathf.Clamp01((float)(value - restBaseline) / range);
    }

    private string ClassifyState(float normalized)
    {
        if (normalized >= thresholdGrip)   return "GRIP";
        if (normalized >= thresholdStrong) return "STRONG";
        if (normalized >= thresholdLight)  return "LIGHT";
        return "REST";
    }

    void OnDestroy()
    {
        _running = false;
        _thread?.Join(200);
        if (_port?.IsOpen == true) _port.Close();
    }
}