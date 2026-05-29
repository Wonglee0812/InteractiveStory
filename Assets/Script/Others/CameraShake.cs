using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance;

    private CinemachineBasicMultiChannelPerlin _noise;
    private CinemachineImpulseSource _impulseSource;

    [Header("--- GLOBAL SHAKE CONTROL ---")]
    [Range(0f, 2f)]
    public float globalShakeMultiplier = 1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _impulseSource = GetComponent<CinemachineImpulseSource>();

        CinemachineCamera cam = FindAnyObjectByType<CinemachineCamera>();
        if (cam != null)
        {
            _noise = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();
            if (_noise != null) _noise.AmplitudeGain = 0f;
        }
    }

    // ====================================================================
    // [VI] XỬ LÝ THEO SCRIPTABLE OBJECT / [EN] PROCESS VIA SCRIPTABLE OBJECT
    // ====================================================================
    public void PlayShake(CameraShakeData data)
    {
        if (data == null || globalShakeMultiplier <= 0f) return;

        if (data.shakeType == ShakeType.DirectionalRecoil)
        {
            GenerateRecoil(data.recoilForce);
        }
        else if (data.shakeType == ShakeType.ContinuousNoise)
        {
            GenerateNoise(data.noiseIntensity, data.duration);
        }
    }

    // ====================================================================
    // [VI] CÁC HÀM CỐT LÕI VÀ DỰ PHÒNG / [EN] CORE AND FALLBACK FUNCTIONS
    // ====================================================================
    public void Shake(float intensity, float duration)
    {
        // [VI] Giữ lại hàm này cho PlayerState hoặc mảnh vỡ Debris
        // [EN] Retain this function for PlayerState or Debris fragments
        GenerateNoise(intensity, duration);
    }

    public void GenerateRecoil(Vector3 recoilForce)
    {
        if (_impulseSource == null || globalShakeMultiplier <= 0f) return;
        Vector3 finalForce = recoilForce * globalShakeMultiplier;
        _impulseSource.GenerateImpulseWithVelocity(finalForce);
    }

    public void GenerateNoise(float intensity, float duration)
    {
        if (_noise == null || globalShakeMultiplier <= 0f) return;
        StopAllCoroutines();
        float finalIntensity = intensity * globalShakeMultiplier;
        StartCoroutine(NoiseRoutine(finalIntensity, duration));
    }

    private System.Collections.IEnumerator NoiseRoutine(float intensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _noise.AmplitudeGain = Mathf.Lerp(intensity, 0f, elapsed / duration);
            yield return null;
        }
        _noise.AmplitudeGain = 0f;
    }
}