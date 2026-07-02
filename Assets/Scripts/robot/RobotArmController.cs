using UnityEngine;

public class RobotArmController : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("EMG 상태를 로봇 명령으로 변환하는 RobotCommandMapper를 연결하세요.")]
    [SerializeField] private RobotCommandMapper commandMapper;

    [Header("그리퍼 비주얼 (회전 방식)")]
    [SerializeField] private Transform gripperLeft;
    [SerializeField] private Transform gripperRight;

    [Tooltip("펼쳤을 때(Release) 추가 각도. 두 그리퍼가 서로 벌어지는 정도입니다.")]
    [SerializeField] private float openAngleOffset = 20f;

    [Tooltip("오므렸을 때(GripClose) 추가 각도. 0이면 기준 자세 그대로 모입니다.")]
    [SerializeField] private float closedAngleOffset = 0f;

    [Tooltip("회전 보간 속도(도/초 느낌). 값이 클수록 빠르게 움직입니다.")]
    [SerializeField] private float rotateSpeedDegPerSec = 180f;

    private bool isClosed = false;
    private Quaternion leftBaseRotation;
    private Quaternion rightBaseRotation;

    private void Start()
    {
        if (gripperLeft != null) leftBaseRotation = gripperLeft.localRotation;
        if (gripperRight != null) rightBaseRotation = gripperRight.localRotation;
    }

    private void Update()
    {
        if (commandMapper == null)
        {
            Debug.LogWarning("[RobotArmController] Command Mapper가 연결되지 않았습니다. " +
                              "Inspector에서 RobotCommandMapper를 연결해주세요.");
            return;
        }

        switch (commandMapper.CurrentCommand)
        {
            case RobotCommand.Release:
                isClosed = false;
                break;
            case RobotCommand.GripClose:
                isClosed = true;
                break;
            case RobotCommand.Hold:
                break;
        }

        ApplyGripperVisual();
    }

    private void ApplyGripperVisual()
    {
        if (gripperLeft == null || gripperRight == null) return;

        float extra = isClosed ? closedAngleOffset : openAngleOffset;

        Quaternion leftTarget = leftBaseRotation * Quaternion.Euler(0f, extra, 0f);
        gripperLeft.localRotation = Quaternion.RotateTowards(
            gripperLeft.localRotation,
            leftTarget,
            rotateSpeedDegPerSec * Time.deltaTime);

        Quaternion rightTarget = rightBaseRotation * Quaternion.Euler(0f, -extra, 0f);
        gripperRight.localRotation = Quaternion.RotateTowards(
            gripperRight.localRotation,
            rightTarget,
            rotateSpeedDegPerSec * Time.deltaTime);
    }
}