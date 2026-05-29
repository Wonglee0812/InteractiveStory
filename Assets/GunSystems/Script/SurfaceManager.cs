using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SurfaceMapping
{
    public string surfaceTag; // Ví dụ: "Hitbox" (Bắn quái), "Metal" (Tường sắt)
    public SurfaceEffectData effectData;
}

public class SurfaceManager : MonoBehaviour
{
    public static SurfaceManager Instance;

    [Header("--- SURFACE SETTINGS ---")]
    public SurfaceEffectData defaultEffect; // Dùng khi không tìm thấy tag tương ứng
    public List<SurfaceMapping> surfaceMappings = new List<SurfaceMapping>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void PlayImpact(Vector3 hitPoint, Vector3 hitNormal, string surfaceTag)
    {
        SurfaceEffectData dataToPlay = defaultEffect;

        // Tìm effect tương ứng với Tag
        foreach (var mapping in surfaceMappings)
        {
            if (mapping.surfaceTag == surfaceTag)
            {
                dataToPlay = mapping.effectData;
                break;
            }
        }

        if (dataToPlay == null) return;

        // 1. Spawn VFX (Hạt bụi/máu/lửa)
        if (dataToPlay.impactVFX != null)
        {
            GameObject vfx = Instantiate(dataToPlay.impactVFX, hitPoint, Quaternion.LookRotation(hitNormal));
            Destroy(vfx, 0.3f); // Tự hủy sau 2s cho nhẹ máy
        }

        // 2. Spawn Decal (Vết đạn)
        if (dataToPlay.decalPrefab != null)
        {
            // Nhích vết đạn ra ngoài 0.01f để không bị chìm vào tường (Z-fighting)
            GameObject decal = Instantiate(dataToPlay.decalPrefab, hitPoint + hitNormal * 0.01f, Quaternion.LookRotation(hitNormal));
            Destroy(decal, 2f); // Tự hủy vết đạn sau 5s
        }
    }
}