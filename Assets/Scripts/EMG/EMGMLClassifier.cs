using UnityEngine;

/// <summary>
/// train.py에서 4명(s1~s4) 데이터로 학습한 LogisticRegression을 그대로 이식한 분류기.
/// EMGSerialReader의 최근 20샘플(200ms) 윈도우에서 MAV/RMS/STD/WL/ZC 특징을 뽑아
/// REST(휴식) / GRIP(수축) 2클래스를 판별한다.
/// LOSO(사람 분리) 검증 정확도 약 87% — 새로운 사람에게도 어느 정도 일반화됨.
/// 절대 임계값(EMGSerialReader의 thresholdLight 등)과 달리 raw 스케일 변화에 덜 민감하다.
/// </summary>
public class EMGMLClassifier : MonoBehaviour
{
    // feature 순서: [mav, rms, std, wl, zc] (features.py extract()와 동일)
    private static readonly float[] ScalerMean =
        { 569.4876f, 599.6742f, 104.5339f, 916.9350f, 1.2393f };
    private static readonly float[] ScalerScale =
        { 1116.9513f, 1140.7585f, 279.4912f, 2768.9521f, 2.5008f };
    private static readonly float[] Coef =
        { 1.2128f, 2.0272f, 0.0914f, -0.4476f, 0.9471f };
    private const float Intercept = -0.0252f;

    [Tooltip("이 확률 이상이면 GRIP으로 판정")]
    [Range(0f, 1f)] public float decisionThreshold = 0.5f;

    public static string CurrentMLState = "REST";
    public static float  CurrentMLProbability = 0f;

    void Update()
    {
        if (!EMGSerialReader.TryGetWindow(out int[] window)) return;

        float[] features = ExtractFeatures(window);
        CurrentMLProbability = Predict(features);
        CurrentMLState = CurrentMLProbability >= decisionThreshold ? "GRIP" : "REST";
    }

    // threshold 방식(EMGSerialReader.CurrentState)과 ML 방식(CurrentMLState)을
    // 화면에 나란히 띄워서 실제 동작 중 두 값이 얼마나 일치하는지 비교하기 위한 디버그 오버레이.
    void OnGUI()
    {
        GUI.skin.label.fontSize = 24;
        GUILayout.BeginArea(new Rect(20, 20, 500, 150));
        GUILayout.Label($"Raw: {EMGSerialReader.CurrentValue}  Norm: {EMGSerialReader.CurrentNormalized:F2}");
        GUILayout.Label($"Threshold State: {EMGSerialReader.CurrentState}");
        GUILayout.Label($"ML State: {CurrentMLState}  (p={CurrentMLProbability:F2})");
        GUILayout.EndArea();
    }

    private static float[] ExtractFeatures(int[] window)
    {
        int n = window.Length;

        float mean = 0f;
        for (int i = 0; i < n; i++) mean += window[i];
        mean /= n;

        float sumAbs = 0f, sumSq = 0f, sumVar = 0f, wl = 0f;
        float[] centered = new float[n];
        for (int i = 0; i < n; i++)
        {
            float v = window[i];
            sumAbs += Mathf.Abs(v);
            sumSq += v * v;
            float d = v - mean;
            sumVar += d * d;
            centered[i] = d;
        }
        for (int i = 1; i < n; i++)
            wl += Mathf.Abs(window[i] - window[i - 1]);

        int zc = 0;
        int prevSign = Sign(centered[0]);
        for (int i = 1; i < n; i++)
        {
            int s = Sign(centered[i]);
            if (s != prevSign) zc++;
            prevSign = s;
        }

        float mav = sumAbs / n;
        float rms = Mathf.Sqrt(sumSq / n);
        float std = Mathf.Sqrt(sumVar / n);

        return new float[] { mav, rms, std, wl, zc };
    }

    private static int Sign(float x) => x > 0f ? 1 : (x < 0f ? -1 : 0);

    private static float Predict(float[] features)
    {
        float z = Intercept;
        for (int i = 0; i < features.Length; i++)
        {
            float scaled = (features[i] - ScalerMean[i]) / ScalerScale[i];
            z += Coef[i] * scaled;
        }
        return 1f / (1f + Mathf.Exp(-z));
    }
}
