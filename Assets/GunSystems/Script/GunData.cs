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
    [Tooltip("VI: Scale gốc để chống méo khi gắn vào xương tay / EN: Base scale to prevent distortion when attached to hand bone")]
    public Vector3 baseScale = Vector3.one;

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

    [Header("--- PROCEDURAL RECOIL ---")]

    [Tooltip("VI: Lực đẩy lùi súng (Trục Z âm là lùi lại) / EN: Gun push back force (Negative Z is backward)")]
    public Vector3 recoilKickPosition = new Vector3(0f, 0f, -0.25f);

    [Tooltip("VI: Lực hất nòng súng (Trục X âm là hất lên) / EN: Muzzle flip force (Negative X is upward)")]
    public Vector3 recoilKickRotation = new Vector3(-2f, 0.5f, 0.5f);

    [Tooltip("VI: Độ nảy của súng (Càng cao nảy càng gắt) / EN: Snappiness (Higher is more aggressive)")]
    public float recoilSnappiness = 50f;

    [Tooltip("VI: Tốc độ súng hồi về vị trí ngắm / EN: Speed at which gun returns to aim position")]
    public float recoilReturnSpeed = 20f;

}