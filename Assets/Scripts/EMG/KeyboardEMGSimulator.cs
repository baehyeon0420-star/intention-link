using UnityEngine;

public class KeyboardEMGSimulator : MonoBehaviour
{
    [Header("키 매핑 (필요하면 Inspector에서 변경 가능)")]
    [SerializeField] private KeyCode restKey = KeyCode.R;
    [SerializeField] private KeyCode lightKey = KeyCode.L;
    [SerializeField] private KeyCode strongKey = KeyCode.S;
    [SerializeField] private KeyCode gripKey = KeyCode.G;

    [Header("상태별 가상 ADC 값 (0~4095, EMGSerialReader.CurrentValue와 같은 형식)")]
    [SerializeField] private int restValue = 200;
    [SerializeField] private int lightValue = 1500;
    [SerializeField] private int strongValue = 3200;
    [SerializeField] private int gripValue = 4000;

    private void Update()
    {
        if (Input.GetKey(gripKey))
        {
            EMGSerialReader.CurrentState = "GRIP";
            EMGSerialReader.CurrentValue = gripValue;
        }
        else if (Input.GetKey(strongKey))
        {
            EMGSerialReader.CurrentState = "STRONG";
            EMGSerialReader.CurrentValue = strongValue;
        }
        else if (Input.GetKey(lightKey))
        {
            EMGSerialReader.CurrentState = "LIGHT";
            EMGSerialReader.CurrentValue = lightValue;
        }
        else
        {
            EMGSerialReader.CurrentState = "REST";
            EMGSerialReader.CurrentValue = restValue;
        }
    }
}