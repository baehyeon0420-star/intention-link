using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

/// <summary>
/// ESP32로부터 Serial 데이터를 수신하여 EMG 상태를 갱신하는 클래스.
/// ESP32는 "값,상태\n" 형식으로 전송한다. 예: "750,LIGHT"
/// CurrentValue, CurrentState를 static으로 공개하여 다른 스크립트에서 참조 가능.
/// </summary>
public class EMGSerialReader : MonoBehaviour
{
    [Header("Serial Settings")]
    [Tooltip("ESP32가 연결된 포트 이름. Windows: COM3 / Mac: /dev/cu.usbserial-XXXX")]
    public string portName = "COM3";

    [Tooltip("ESP32 코드의 Serial.begin() 값과 동일하게 설정")]
    public int baudRate = 115200;

    // 다른 스크립트에서 EMGSerialReader.CurrentState 로 접근
    public static int    CurrentValue = 0;     // ESP32에서 읽은 ADC 정수값 (0~4095)
    public static string CurrentState = "REST"; // REST / LIGHT / STRONG

    private SerialPort  _port;
    private Thread      _thread;       // 별도 스레드에서 Serial을 읽어 Unity 메인 스레드 블로킹 방지
    private bool        _running;      // 스레드 종료 플래그
    private string      _latestLine = "";
    private readonly object _lock = new object(); // 스레드 간 데이터 접근 충돌 방지용 락

    void Start()
    {
        // ReadTimeout: ReadLine() 호출 시 데이터 없으면 이 시간(ms) 후 TimeoutException 발생
        _port = new SerialPort(portName, baudRate) { ReadTimeout = 100 };
        try
        {
            _port.Open();
            _running = true;

            // IsBackground = true: Unity가 종료되면 스레드도 자동으로 종료
            _thread = new Thread(ReadThread) { IsBackground = true };
            _thread.Start();
            Debug.Log("[EMG] Serial opened: " + portName);
        }
        catch (Exception e)
        {
            // 포트 이름이 틀렸거나 Arduino IDE 시리얼 모니터가 열려 있으면 실패
            Debug.LogError("[EMG] Cannot open serial: " + e.Message);
        }
    }

    /// <summary>
    /// 별도 스레드에서 Serial 데이터를 계속 읽는 루프.
    /// Unity 메인 스레드가 아니므로 Unity API 호출 금지, _latestLine에만 저장.
    /// </summary>
    void ReadThread()
    {
        while (_running && _port.IsOpen)
        {
            try
            {
                string line = _port.ReadLine().Trim();
                lock (_lock) { _latestLine = line; } // 락으로 메인 스레드와 동시 접근 방지
            }
            catch (TimeoutException) { } // 데이터 없을 때 정상적으로 발생, 무시
            catch (Exception e)
            {
                Debug.LogWarning("[EMG] Read error: " + e.Message);
            }
        }
    }

    void Update()
    {
        // 메인 스레드에서 _latestLine을 읽어 CurrentValue/CurrentState 갱신
        string line;
        lock (_lock) { line = _latestLine; _latestLine = ""; }
        if (string.IsNullOrEmpty(line)) return;

        // ESP32 전송 포맷: "750,LIGHT" → parts[0] = "750", parts[1] = "LIGHT"
        string[] parts = line.Split(',');
        if (parts.Length == 2 && int.TryParse(parts[0], out int val))
        {
            CurrentValue = val;
            CurrentState = parts[1];
        }
    }

    void OnDestroy()
    {
        // 씬 종료 시 스레드와 포트를 명시적으로 닫아 리소스 누수 방지
        _running = false;
        _thread?.Join(200); // 스레드가 종료될 때까지 최대 200ms 대기
        if (_port?.IsOpen == true) _port.Close();
    }
}
