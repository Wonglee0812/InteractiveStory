using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public class GunController : MonoBehaviour
{
    [Header("--- REFERENCES ---")]
    [SerializeField] private WeaponInventory inventory; //
    [SerializeField] private Transform gunParent; // Nơi đặt súng trên tay nhân vật /

    private GunData _activeGunData;
    private RuntimeWeapon _currentWeaponInstance;
    private float _lastShootTime;
    private ObjectPool<TrailRenderer> _trailPool;

    [Header("--- SOCKET REFERENCES ---")]
    [Tooltip("VI: Kéo xương bàn tay phải vào đây / EN: Drag the right hand bone here")]
    [SerializeField] private Transform handSocket;

    [Tooltip("VI: Kéo xương Lưng (Spine2 hoặc Cổ) vào đây / EN: Drag the Back bone (Spine2 or Neck) here")]
    [SerializeField] private Transform holsterSocket;

    public bool isWeaponDrawn = false; // VI: Súng đang cầm trên tay hay vắt sau lưng? / EN: Gun in hand or holstered?

    private void Start()
    {
        // VI: Tự động trang bị vũ khí mặc định từ Inventory khi vào game
        // EN: Automatically equip default weapon from Inventory upon start
        if (inventory != null && inventory.CurrentWeapon != null) //
        {
            EquipWeapon(inventory.CurrentWeapon);
        }
    }

    // ==========================================
    // VI: HÀM TRANG BỊ VÀ THAY ĐỔI SÚNG (SWITCH WEAPON)
    // EN: WEAPON EQUIPPING AND SWITCHING METHOD
    // ==========================================
    public void EquipWeapon(GunData newGunData)
    {
        // ... (Code xóa súng cũ giữ nguyên / Keep old weapon destroy code) ...

        _activeGunData = newGunData;

        // Spawn súng mặc định vào Lưng (Vắt súng)
        GameObject gunObj = Instantiate(_activeGunData.modelPrefab, holsterSocket);
        _currentWeaponInstance = gunObj.GetComponent<RuntimeWeapon>();

        // Áp dụng trạng thái mặc định (Cất súng)
        SetWeaponDrawnState(false);

        // ... (Code Object Pool Trail đạn giữ nguyên / Keep Trail Object Pool code) ...
    }
    public void SetWeaponDrawnState(bool isDrawn)
    {
        if (_currentWeaponInstance == null || _activeGunData == null) return;

        isWeaponDrawn = isDrawn;
        Transform gunTransform = _currentWeaponInstance.transform;

        if (isWeaponDrawn)
        {
            // VI: Chuyển súng sang TAY PHẢI (Hold/Aim)
            // EN: Move gun to RIGHT HAND (Hold/Aim)
            gunTransform.SetParent(handSocket);
            gunTransform.localPosition = _activeGunData.holdPositionOffset;
            gunTransform.localRotation = Quaternion.Euler(_activeGunData.holdRotationOffset);
        }
        else
        {
            // VI: Chuyển súng sang SAU LƯNG (Holster)
            // EN: Move gun to BACK (Holster)
            gunTransform.SetParent(holsterSocket);
            gunTransform.localPosition = _activeGunData.holsterPositionOffset;
            gunTransform.localRotation = Quaternion.Euler(_activeGunData.holsterRotationOffset);
        }
    }
    // ==========================================
    // VI: HÀM TIẾP NHẬN LỆNH BẮN LIÊN TỤC TỪ INPUT
    // EN: CONTINUOUS FIRE INPUT HANDLING METHOD
    // ==========================================
    public void HandleFiring(bool inputPressed, bool inputHeld)
    {
        if (_activeGunData == null || _currentWeaponInstance == null) return;

        // VI: CHẶN LOGIC - Súng cất sau lưng thì không được bắn!
        // EN: LOGIC BLOCK - Cannot shoot if gun is holstered on the back!
        if (!isWeaponDrawn) return;

        bool canShoot = _activeGunData.isAutomatic ? inputHeld : inputPressed;
        if (canShoot && Time.time >= _lastShootTime + _activeGunData.fireRate)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        _lastShootTime = Time.time;

        // 1. VI: Phát tín hiệu âm thanh tiếng súng cho hệ thống AI nghe thấy
        // 1. EN: Report gunfire audio signal to the AI auditory system
        AIAuditorySystem.ReportSound(_currentWeaponInstance.bulletSpawnPoint.position, _activeGunData.soundRadius); //

        // 2. VI: Kích hoạt hiệu ứng tia lửa đầu nòng vật lý
        // 2. EN: Play physical muzzle flash particle effect
        if (_currentWeaponInstance.muzzleFlashVFX != null)
        {
            _currentWeaponInstance.muzzleFlashVFX.Play(); //
        }

        // 3. VI: Tính toán tia bắn Raycast kèm độ lệch tâm đạn (Spread) từ tâm Camera
        // 3. EN: Compute firing Raycast with weapon Spread from Camera center
        var playerCamera = Camera.main; //
        Ray baseRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); //

        Vector3 spreadVector = (playerCamera.transform.right * Random.Range(-_activeGunData.spread.x, _activeGunData.spread.x)) +
                               (playerCamera.transform.up * Random.Range(-_activeGunData.spread.y, _activeGunData.spread.y)) +
                               (playerCamera.transform.forward * Random.Range(-_activeGunData.spread.z, _activeGunData.spread.z)); //

        Vector3 finalDirection = (baseRay.direction + spreadVector).normalized; //
        Ray finalRay = new Ray(baseRay.origin, finalDirection); //

        Vector3 startPoint = _currentWeaponInstance.bulletSpawnPoint.position; // Trích xuất từ vỏ RuntimeWeapon

        // 4. VI: Tiến hành quét va chạm tia đạn vật lý
        // 4. EN: Perform physical bullet collision sweep
        if (Physics.Raycast(finalRay, out RaycastHit hit, float.MaxValue, _activeGunData.hitMask)) //
        {
            // Xử lý trừ máu mục tiêu / Deal damage to target
            IDamageable target = hit.collider.GetComponent<IDamageable>(); //
            if (target != null) target.TakeDamage(_activeGunData.damage, hit.point); //

            // Bắn hiệu ứng văng bụi đất/máu lên bề mặt va chạm
            // Play surface particle impact vfx and decals
            if (SurfaceManager.Instance != null)
            {
                SurfaceManager.Instance.PlayImpact(hit.point, hit.normal, hit.collider.tag); //
            }

            // Gọi Coroutine di chuyển vệt đạn từ Pool đến điểm trúng
            // Call Coroutine to move trail renderer to impact point
            StartCoroutine(PlayTrailRoutine(startPoint, hit.point, hit)); //
        }
        else
        {
            // VI: Nếu bắn hụt vào khoảng không, kéo dài đường đạn ra xa theo cấu hình Miss Distance
            // EN: If raycast misses, extend trail to max Miss Distance configured in SO
            Vector3 targetPoint = finalRay.origin + (finalDirection * _activeGunData.trailConfig.MissDisstance); //
            StartCoroutine(PlayTrailRoutine(startPoint, targetPoint, new RaycastHit())); //
        }

        // 5. VI: Chuẩn bị kích hoạt hiệu ứng giật và rung lắc màn hình ở các giai đoạn sau
        // 5. EN: Prepare to trigger recoil and screen shake juice in later phases
        TriggerWeaponJuice();
    }

    private void TriggerWeaponJuice()
    {
        if (_activeGunData.shakeData == null) return;
        // Sẽ triển khai nhúng code giật nòng tại đây / Will implement recoil code injection here
    }

    // ==========================================
    // VI: CÁC HÀM QUẢN LÝ SỰ KIỆN OBJECT POOL 
    // EN: OBJECT POOL EVENT MANAGEMENT METHODS
    // ==========================================
    private TrailRenderer CreateTrail()
    {
        GameObject instance = new GameObject("Bullet Trail");
        TrailRenderer trail = instance.AddComponent<TrailRenderer>(); //
        trail.colorGradient = _activeGunData.trailConfig.Color; //
        trail.material = _activeGunData.trailConfig.Materail; //
        trail.widthCurve = _activeGunData.trailConfig.WidthCurve; //
        trail.time = _activeGunData.trailConfig.Duration; //
        trail.minVertexDistance = _activeGunData.trailConfig.MinVertexDistance; //
        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; //
        return trail;
    }

    private void OnTakeTrailFromPool(TrailRenderer trail) => trail.gameObject.SetActive(true);
    private void OnReturnTrailToPool(TrailRenderer trail) => trail.gameObject.SetActive(false);
    private void OnDestroyTrail(TrailRenderer trail) => Destroy(trail.gameObject);

    private IEnumerator PlayTrailRoutine(Vector3 startPoint, Vector3 endPoint, RaycastHit hit)
    {
        TrailRenderer instance = _trailPool.Get(); //
        instance.transform.position = startPoint; //

        // VI: Tắt phát tia và đợi 1 frame để xóa sạch vệt đạn cũ bị kéo dãn từ vị trí cũ
        // EN: Reset emitting and wait 1 frame to clear stretched trails from previous pooling position
        instance.emitting = false; //
        yield return null;
        instance.emitting = true; //

        float distance = Vector3.Distance(startPoint, endPoint); //
        float remainDistance = distance; //

        while (remainDistance > 0)
        {
            instance.transform.position = Vector3.Lerp(startPoint, endPoint, Mathf.Clamp01(1 - (remainDistance / distance))); //
            remainDistance -= _activeGunData.trailConfig.StimlationSpeed * Time.deltaTime; //
            yield return null;
        }

        instance.transform.position = endPoint; //

        // VI: Tạo sóng âm thanh tiếng động tại điểm va chạm đạn để đánh động AI
        // EN: Report bullet impact audio signal to alert nearby AI agents
        if (hit.collider != null)
        {
            AIAuditorySystem.ReportSound(hit.point, 100f); //
        }

        yield return new WaitForSeconds(_activeGunData.trailConfig.Duration); //
        instance.emitting = false; //
        _trailPool.Release(instance); // Hoàn trả về bộ nhớ đệm /
    }
}