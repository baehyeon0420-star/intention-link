using UnityEngine;

// 그리퍼(손)에 내릴 수 있는 명령의 종류.
// EMG 신호(EMGStateManager의 이벤트)와 키보드 둘 다 이 명령으로 변환되어 처리된다.
public enum GripperCommand
{
    None,           // 아무 동작 없음
    GripClose,      // 쥐기 (그리퍼를 닫음)
    Release,        // 놓기 (그리퍼를 엶)
    EmergencyStop   // 긴급 정지
}