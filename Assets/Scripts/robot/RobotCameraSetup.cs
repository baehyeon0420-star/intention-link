using UnityEngine;
using UnityEngine.UI;

// RobotCamera가 보는 화면을 RenderTexture로 만들어서
// UI의 RawImage에 출력하는 스크립트입니다.
// 별도의 RenderTexture 에셋 파일을 미리 만들 필요 없이, 실행 중에 코드로 생성합니다.
//
// 이 스크립트는 RawImage 컴포넌트가 붙어있는 UI 오브젝트(예: RobotCameraView)에
// 붙이면 됩니다.
public class RobotCameraSetup : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("로봇 전면 카메라(RobotCamera에 붙은 Camera 컴포넌트)를 연결하세요.")]
    [SerializeField] private Camera robotCamera;

    [Tooltip("화면을 보여줄 RawImage. 비워두면 같은 오브젝트의 RawImage를 자동으로 찾습니다.")]
    [SerializeField] private RawImage targetRawImage;

    [Header("RenderTexture 설정")]
    [SerializeField] private int textureWidth = 480;
    [SerializeField] private int textureHeight = 270;

    private RenderTexture renderTexture;

    private void Awake()
    {
        if (targetRawImage == null)
        {
            targetRawImage = GetComponent<RawImage>();
        }

        if (robotCamera == null)
        {
            Debug.LogWarning("[RobotCameraSetup] Robot Camera가 연결되지 않았습니다. " +
                              "Inspector에서 RobotCamera를 연결해주세요.");
            return;
        }

        if (targetRawImage == null)
        {
            Debug.LogWarning("[RobotCameraSetup] Target Raw Image를 찾을 수 없습니다. " +
                              "이 오브젝트에 RawImage 컴포넌트가 있는지, 또는 Inspector에서 연결했는지 확인해주세요.");
            return;
        }

        // RenderTexture를 코드로 생성합니다 (Assets에 별도 파일을 만들 필요 없음).
        renderTexture = new RenderTexture(textureWidth, textureHeight, 16);
        robotCamera.targetTexture = renderTexture;
        targetRawImage.texture = renderTexture;
    }

    private void OnDestroy()
    {
        // 메모리 누수를 막기 위해 생성했던 RenderTexture를 정리합니다.
        if (renderTexture != null)
        {
            if (robotCamera != null) robotCamera.targetTexture = null;
            renderTexture.Release();
            renderTexture = null;
        }
    }
}
