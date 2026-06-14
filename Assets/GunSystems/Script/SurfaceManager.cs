using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SurfaceMapping
{
    public string surfaceTag; // Ví dụ: "Head", "Body", "Metal"
    public SurfaceEffectData effectData;
}

public class SurfaceManager : MonoBehaviour
{
    public static SurfaceManager Instance;

    [Header("--- SURFACE SETTINGS ---")]
    public SurfaceEffectData defaultEffect;
    public List<SurfaceMapping> surfaceMappings = new List<SurfaceMapping>();

    // [VI] Bộ nhớ siêu tốc độ O(1) để tra cứu Tag
    private Dictionary<string, SurfaceEffectData> _effectDictionary;

    // [VI] Thùng rác để gom gọn các VFX/Decal sinh ra, giúp Hierarchy sạch sẽ
    private Transform _impactContainer;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        // 1. CHUYỂN DATA SANG DICTIONARY ĐỂ TỐI ƯU TỐC ĐỘ DÒ TÌM
        _effectDictionary = new Dictionary<string, SurfaceEffectData>();
        foreach (var mapping in surfaceMappings)
        {
            // Tránh lỗi trùng Tag
            if (!_effectDictionary.ContainsKey(mapping.surfaceTag))
            {
                _effectDictionary.Add(mapping.surfaceTag, mapping.effectData);
            }
        }

        // 2. TẠO THÙNG CHỨA RÁC TRONG HIERARCHY
        _impactContainer = new GameObject("SurfaceImpacts_Container").transform;
    }

    public void PlayImpact(Vector3 hitPoint, Vector3 hitNormal, string surfaceTag)
    {
        SurfaceEffectData dataToPlay = defaultEffect;

        // [VI] ĐÃ FIX: Tra cứu Dictionary ngay lập tức thay vì dùng Foreach Loop
        if (_effectDictionary.TryGetValue(surfaceTag, out SurfaceEffectData mappedData))
        {
            dataToPlay = mappedData;
        }

        if (dataToPlay == null) return;

        // 1. SPAWN VFX (Bụi/Máu)
        if (dataToPlay.impactVFX != null)
        {
            // Nhét vào _impactContainer
            GameObject vfx = Instantiate(dataToPlay.impactVFX, hitPoint, Quaternion.LookRotation(hitNormal), _impactContainer);
            Destroy(vfx, 0.3f);
        }

        // 2. SPAWN DECAL (Vết đạn)
        if (dataToPlay.decalPrefab != null)
        {
            // [VI] ĐÃ FIX: Thêm vòng xoay ngẫu nhiên từ 0-360 độ ở trục Z để các vết đạn nhìn khác nhau
            Quaternion randomSpin = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            Quaternion finalRotation = Quaternion.LookRotation(hitNormal) * randomSpin;

            // Nhích ra 0.01f để không chìm vào tường, nhét vào _impactContainer
            GameObject decal = Instantiate(dataToPlay.decalPrefab, hitPoint + hitNormal * 0.01f, finalRotation, _impactContainer);
            Destroy(decal, 2f);
        }
    }
}