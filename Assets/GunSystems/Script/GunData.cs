using UnityEngine;

[CreateAssetMenu(fileName = "Gun", menuName = "Weapons/Gun Data", order = 0)]
public class GunData : ScriptableObject
{
    [Header("--- BASE STATS ---")]
    public GunType Type;
    public string gunName = "New Gun";
    public float damage = 20f;
    public bool isAutomatic = true;
    public float fireRate = 0.1f;
    public float soundRadius = 15f;

    [Header("--- SHOOT CONFIG ---")]
    public LayerMask hitMask;
    public Vector3 spread;

    [Header("--- VISUAL & PREFAB ---")]
    public GameObject modelPrefab;

    [Header("Hold Socket (Right Hand)")]
    public Vector3 holdPositionOffset;
    public Vector3 holdRotationOffset;

    [Header("Holster Socket (Back/Spine)")]
    public Vector3 holsterPositionOffset;
    public Vector3 holsterRotationOffset;

    // VI: Cấu hình vị trí/góc xoay ban đầu khi cầm súng (Tùy chọn)
    // EN: Initial position/rotation config when holding the gun (Optional)
    public Vector3 spawnPositionOffset;
    public Vector3 spawnRotationOffset;

    [Header("--- JUICE EFFECTS ---")]
    public TrailConfigScriptableObject trailConfig;
    public CameraShakeData shakeData; // Sẵn sàng để làm hiệu ứng giật camera / Ready for camera shake effects
}