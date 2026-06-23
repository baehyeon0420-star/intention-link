using System;
using UnityEngine;

/// <summary>
/// EMG 수축 패턴을 분류하여 이벤트로 발행하는 클래스.
/// EMGSerialReader의 CurrentState를 매 프레임 감시하여
/// 수축 시간에 따라 Short / Long / Double 이벤트를 구분한다.
///
/// 패턴 분류 기준:
///   Short  : 수축 지속 시간 < shortMax (ms)
///   Long   : 수축 지속 시간 >= longMin (ms)
///   Double : Short 후 doubleWindow (ms) 내에 다음 Short 발생
/// </summary>
public class EMGStateManager : MonoBehaviour
{
    // 다른 스크립트에서 += 로 구독하여 EMG 이벤트를 받을 수 있음
    public static event Action OnActivationStarted;  // 수축 시작 (REST → ACTIVE)
    public static event Action OnActivationEnded;    // 수축 종료 (ACTIVE → REST)
    public static event Action OnShortContraction;   // 짧은 수축 1회 → Select
    public static event Action OnLongContraction;    // 긴 수축   → Grab
    public static event Action OnDoubleContraction;  // 짧은 수축 2회 → Menu

    [Header("Timing (ms)")]
    [Tooltip("이 시간 미만으로 수축하면 Short로 분류")]
    public float shortMax = 350f;

    [Tooltip("이 시간 이상 수축을 유지하면 Long으로 분류")]
    public float longMin = 700f;

    [Tooltip("첫 번째 Short 종료 후 이 시간 내에 두 번째 Short가 오면 Double로 분류")]
    public float doubleWindow = 500f;

    private bool  _wasActive        = false;  // 직전 프레임의 ACTIVE 여부
    private float _activeStartTime  = 0f;     // 수축이 시작된 시각 (Time.time)
    private float _lastShortEndTime = -999f;  // 마지막 Short 수축이 끝난 시각
    private bool  _waitingForDouble = false;  // Double 인식을 위해 두 번째 Short 대기 중 여부

    void Update()
    {
        // LIGHT 또는 STRONG이면 근육이 활성화된 것으로 판단
        bool isActive = EMGSerialReader.CurrentState == "LIGHT" ||
                        EMGSerialReader.CurrentState == "STRONG";

        // REST → ACTIVE: 수축 시작 시각 기록
        if (isActive && !_wasActive)
        {
            _activeStartTime = Time.time;
            OnActivationStarted?.Invoke();
        }

        // ACTIVE → REST: 수축 종료, 지속 시간으로 패턴 분류
        if (!isActive && _wasActive)
        {
            float duration = (Time.time - _activeStartTime) * 1000f; // 초 → ms 변환
            OnActivationEnded?.Invoke();

            if (duration >= longMin)
            {
                // 긴 수축 → Grab 용도
                OnLongContraction?.Invoke();
                _waitingForDouble = false; // Long이 발생하면 Double 대기 초기화
            }
            else if (duration < shortMax)
            {
                if (_waitingForDouble &&
                    (Time.time - _lastShortEndTime) * 1000f < doubleWindow)
                {
                    // 두 번째 Short가 doubleWindow 내에 도착 → Double
                    OnDoubleContraction?.Invoke();
                    _waitingForDouble = false;
                }
                else
                {
                    // 첫 번째 Short → 두 번째 Short 대기 시작
                    _waitingForDouble = true;
                    _lastShortEndTime = Time.time;
                }
            }
            // shortMax <= duration < longMin 구간은 노이즈로 간주하여 무시
        }

        // doubleWindow가 만료될 때까지 두 번째 Short가 오지 않으면 Single Short로 처리
        // (Double 판정을 위한 대기 시간 때문에 Single Short 이벤트는 약간 지연됨)
        if (_waitingForDouble &&
            (Time.time - _lastShortEndTime) * 1000f >= doubleWindow)
        {
            OnShortContraction?.Invoke();
            _waitingForDouble = false;
        }

        _wasActive = isActive;
    }
}
