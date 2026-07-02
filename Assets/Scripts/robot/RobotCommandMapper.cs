using UnityEngine;

// EMGSerialReader.CurrentState ("REST"/"LIGHT"/"STRONG"/"GRIP")를
// 로봇이 실행할 명령(RobotCommand)으로 변환하는 작은 번역기입니다.
//
// 매핑:
//   REST   -> Release
//   LIGHT  -> Hold
//   STRONG -> GripClose
//   GRIP   -> GripClose
//
// 새 enum 이름은 기존 프로젝트의 어떤 타입과도 겹치지 않도록 RobotCommand로 지었습니다
// (EMGCommand라는 이름은 기존 구조와 중복될 수 있어 만들지 않았습니다).
public enum RobotCommand
{
    Release,
    Hold,
    GripClose
}

public class RobotCommandMapper : MonoBehaviour
{
    public RobotCommand CurrentCommand { get; private set; } = RobotCommand.Release;

    private void Update()
    {
        switch (EMGSerialReader.CurrentState)
        {
            case "LIGHT":
                CurrentCommand = RobotCommand.Hold;
                break;
            case "STRONG":
            case "GRIP":
                CurrentCommand = RobotCommand.GripClose;
                break;
            default:
                // "REST"를 포함해서, 혹시 알 수 없는 값이 들어와도 안전하게 Release로 처리합니다.
                CurrentCommand = RobotCommand.Release;
                break;
        }
    }
}
