using UnityEngine;

[DisallowMultipleComponent]
public class RuntimeWeapon : MonoBehaviour
{
    [Header("--- MUZZLE REFERENCES ---")]
    [Tooltip("VI: Điểm đầu nòng súng để bắn tia lửa và bắn đạn / EN: Barrel tip point to emit muzzle flash and shoot bullets")]
    public Transform bulletSpawnPoint;

    [Tooltip("VI: Hiệu ứng tia lửa đầu nòng / EN: Muzzle flash particle effect")]
    public ParticleSystem muzzleFlashVFX;

    // ==========================================
    // VI: CÁC BIẾN NÀY ĐỂ CHUẨN BỊ CHO PHẦN ANIMATION TLOU
    // EN: THESE VARIABLES PREPARE FOR THE TLOU ANIMATION PART
    // ==========================================
    [Header("--- ANIMATION RIGGING TARGETS (OPTIONAL) ---")]
    public Transform leftHandIKTarget;
    public Transform rightHandIKTarget;

    private void OnDrawGizmos()
    {
        // VI: Vẽ đường line đỏ trong Scene để bạn dễ nhìn thấy hướng bắn của súng
        // EN: Draw a red line in Scene so you can easily see the gun's firing direction
        if (bulletSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(bulletSpawnPoint.position, bulletSpawnPoint.forward * 2f);
        }
    }
}