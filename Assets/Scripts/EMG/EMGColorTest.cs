using UnityEngine;

/// <summary>
/// EMG 상태에 따라 오브젝트 색을 바꾸는 테스트용 스크립트.
/// Serial 수신이 정상인지 시각적으로 확인하는 용도.
/// 큐브 등 Renderer가 있는 오브젝트에 부착.
/// </summary>
public class EMGColorTest : MonoBehaviour
{
    private Material _mat;

    void Start()
    {
        // material에 직접 접근하면 인스턴스 복사본이 생성되어 이 오브젝트에만 색 변화 적용됨
        _mat = GetComponent<Renderer>().material;
    }

    void Update()
    {
        // EMGSerialReader의 CurrentState에 따라 색 결정
        Color c = EMGSerialReader.CurrentState switch
        {
            "REST"   => Color.white,   // 휴식 상태
            "LIGHT"  => Color.yellow,  // 약한 수축
            "STRONG" => Color.red,     // 강한 수축
            _        => Color.gray     // 알 수 없는 상태 (초기화 전 등)
        };

        // Unity 6 기본 렌더러는 URP 사용.
        // URP 쉐이더의 색상 프로퍼티는 _color가 아닌 _BaseColor이므로 분기 처리.
        if (_mat.HasProperty("_BaseColor"))
            _mat.SetColor("_BaseColor", c);
        else
            _mat.color = c; // Built-in 렌더러 대응
    }
}
