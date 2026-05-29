using UnityEngine;

// [VI] Định nghĩa 2 kiểu giật màn hình / [EN] Define 2 types of screen shake
public enum ShakeType
{
    DirectionalRecoil, // [VI] Giật theo hướng (Sniper/Shotgun) / [EN] Directional kick (Sniper/Shotgun)
    ContinuousNoise    // [VI] Rung nhiễu liên tục (SMG/AR) / [EN] Continuous vibration (SMG/AR)
}

[CreateAssetMenu(fileName = "NewShakeData", menuName = "Weapons/Camera Shake Data", order = 5)]
public class CameraShakeData : ScriptableObject
{
    public ShakeType shakeType;

    [Header("--- DIRECTIONAL RECOIL ---")]
    [Tooltip("[VI] Hướng và lực giật nòng / [EN] Direction and force of barrel kick")]
    public Vector3 recoilForce = new Vector3(0f, 2f, -0.5f);

    [Header("--- CONTINUOUS NOISE ---")]
    [Tooltip("[VI] Biên độ rung lắc / [EN] Vibration amplitude")]
    public float noiseIntensity = 2f;

    [Tooltip("[VI] Thời gian rung / [EN] Vibration duration")]
    public float duration = 0.1f;
}