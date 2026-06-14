using System.Collections;
using System.Collections.Generic;
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
    private Dictionary<GunData, RuntimeWeapon> _spawnedWeapons = new Dictionary<GunData, RuntimeWeapon>();
    [Header("--- SOCKET REFERENCES ---")]
    [Tooltip("VI: Kéo xương bàn tay phải vào đây / EN: Drag the right hand bone here")]
    [SerializeField] private Transform handSocket;

    [Tooltip("VI: Kéo xương Lưng (Spine2 hoặc Cổ) vào đây / EN: Drag the Back bone (Spine2 or Neck) here")]
    [SerializeField] private Transform holsterSocket;

    private Vector3 _targetWeaponRecoilPos;
    private Vector3 _targetWeaponRecoilRot;
    private Vector3 _currentWeaponRecoilPos;
    private Vector3 _currentWeaponRecoilRot;
    public bool isWeaponDrawn = false; // VI: Súng đang cầm trên tay hay vắt sau lưng? / EN: Gun in hand or holstered?

    private void Start()
    {
        // ==========================================
        // [VI] ĐÃ FIX: Sinh ra TẤT CẢ súng trong Inventory và cất vào Holster ngay khi vào game
        // [EN] FIXED: Spawn ALL guns in Inventory and put them in Holster right at start
        // ==========================================
        if (inventory != null && inventory.availableGuns.Count > 0)
        {
            foreach (GunData gunData in inventory.availableGuns)
            {
                SpawnWeaponIntoHolster(gunData);
            }

            EquipWeapon(inventory.CurrentWeapon);
        }
    }

    private void Update()
    {
        if (_activeGunData == null) return;

        // 1. VI: Lò xo kéo súng tự động hồi về vị trí trung tâm (Nhanh dần)
        // 1. EN: Spring automatically pulls the gun back to center position (Accelerating)
        _targetWeaponRecoilPos = Vector3.Lerp(_targetWeaponRecoilPos, Vector3.zero, _activeGunData.recoilReturnSpeed * Time.deltaTime);
        _targetWeaponRecoilRot = Vector3.Lerp(_targetWeaponRecoilRot, Vector3.zero, _activeGunData.recoilReturnSpeed * Time.deltaTime);

        // 2. VI: Nội suy độ nảy cực gắt khi vừa bóp cò (Snappiness)
        // 2. EN: Interpolate extremely aggressive bounce when trigger is pulled (Snappiness)
        _currentWeaponRecoilPos = Vector3.Slerp(_currentWeaponRecoilPos, _targetWeaponRecoilPos, _activeGunData.recoilSnappiness * Time.deltaTime);
        _currentWeaponRecoilRot = Vector3.Slerp(_currentWeaponRecoilRot, _targetWeaponRecoilRot, _activeGunData.recoilSnappiness * Time.deltaTime);

        // 3. VI: Áp dụng Lực đẩy (Pos) và Lực hất (Rot) vào mô hình súng
        // 3. EN: Apply Push force (Pos) and Flip force (Rot) to gun model
        if (isWeaponDrawn && _currentWeaponInstance != null)
        {
            _currentWeaponInstance.transform.localPosition = _activeGunData.holdPositionOffset + _currentWeaponRecoilPos;
            _currentWeaponInstance.transform.localRotation = Quaternion.Euler(_activeGunData.holdRotationOffset + _currentWeaponRecoilRot);
        }
    }
    private void SpawnWeaponIntoHolster(GunData gunData)
    {
        if (gunData == null || _spawnedWeapons.ContainsKey(gunData)) return;

        GameObject gunObj = Instantiate(gunData.modelPrefab, holsterSocket);
        RuntimeWeapon rw = gunObj.GetComponent<RuntimeWeapon>();

        gunObj.transform.localScale = gunData.baseScale;
        gunObj.transform.localPosition = gunData.holsterPositionOffset;
        gunObj.transform.localRotation = Quaternion.Euler(gunData.holsterRotationOffset);

        _spawnedWeapons[gunData] = rw;
    }
    // ==========================================
    // VI: HÀM TRANG BỊ VÀ THAY ĐỔI SÚNG (SWITCH WEAPON)
    // EN: WEAPON EQUIPPING AND SWITCHING METHOD
    // ==========================================
    public void EquipWeapon(GunData newGunData)
    {
        if (newGunData == null) return;

        // 1. [VI] ĐÃ FIX: BỎ LỆNH DESTROY! Chỉ đưa súng cũ về lại vị trí cất sau lưng
        // 1. [EN] FIXED: REMOVED DESTROY! Just move the old gun back to the holster position
        if (_currentWeaponInstance != null && _activeGunData != null)
        {
            Transform oldGunTransform = _currentWeaponInstance.transform;
            oldGunTransform.SetParent(holsterSocket);
            oldGunTransform.localPosition = _activeGunData.holsterPositionOffset;
            oldGunTransform.localRotation = Quaternion.Euler(_activeGunData.holsterRotationOffset);
            oldGunTransform.localScale = _activeGunData.baseScale;
        }

        _activeGunData = newGunData;

        // 2. [VI] Lấy súng mới ra từ kho đồ đã spawn (Nếu chưa có thì spawn bù)
        // 2. [EN] Retrieve the new gun from the spawned storage (Spawn it if not exists)
        if (!_spawnedWeapons.ContainsKey(newGunData))
        {
            SpawnWeaponIntoHolster(newGunData);
        }

        _currentWeaponInstance = _spawnedWeapons[newGunData];

        // 3. Áp dụng trạng thái rút/cất súng hiện tại
        SetWeaponDrawnState(isWeaponDrawn);

        // 4. Làm sạch và tái khởi tạo Object Pool cho Trail đạn
        if (_trailPool != null)
        {
            _trailPool.Clear();
        }
        _trailPool = new ObjectPool<TrailRenderer>(CreateTrail, OnTakeTrailFromPool, OnReturnTrailToPool, OnDestroyTrail);
    }

    public void SetWeaponDrawnState(bool isDrawn)
    {
        if (_currentWeaponInstance == null || _activeGunData == null) return;

        isWeaponDrawn = isDrawn;
        Transform gunTransform = _currentWeaponInstance.transform;

        if (isWeaponDrawn)
        {
            gunTransform.SetParent(handSocket);
            gunTransform.localPosition = _activeGunData.holdPositionOffset;
            gunTransform.localRotation = Quaternion.Euler(_activeGunData.holdRotationOffset);
        }
        else
        {
            gunTransform.SetParent(holsterSocket);
            gunTransform.localPosition = _activeGunData.holsterPositionOffset;
            gunTransform.localRotation = Quaternion.Euler(_activeGunData.holsterRotationOffset);
        }

        // ==========================================
        // [THÊM MỚI] ÉP CỨNG SCALE SAU KHI ĐỔI PARENT
        // [NEW] FORCE HARD SCALE AFTER REPARENTING
        // ==========================================
        gunTransform.localScale = _activeGunData.baseScale;
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
        if (DynamicCrosshair.Instance != null)
        {
            DynamicCrosshair.Instance.SetShootingState(canShoot);
        }

        if (canShoot && Time.time >= _lastShootTime + _activeGunData.fireRate) //
        {
            Shoot(); //
        }
        PlayerCharater playerChar = GetComponentInChildren<PlayerCharater>();

        if (canShoot)
        {
            // [VI] Kích hoạt trạng thái đang sấy đạn
            // [EN] Activate spraying state
            if (playerChar != null) playerChar.SetShootingAnimation(true);

            if (Time.time >= _lastShootTime + _activeGunData.fireRate)
            {
                Shoot();
            }
        }
        else
        {
            // [VI] Người chơi nhả chuột -> Tắt Animation bắn ngay lập tức
            // [EN] Player releases mouse -> Turn off Shoot Animation immediately
            if (playerChar != null) playerChar.SetShootingAnimation(false);
        }
    }

    private void Shoot()
    {
        _lastShootTime = Time.time;
        if (DynamicCrosshair.Instance != null)
        {
            DynamicCrosshair.Instance.ApplyFireKick();
        }
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


            if (DynamicCrosshair.Instance != null)
            {
                if (hit.collider.CompareTag("Head"))
                {
                    DynamicCrosshair.Instance.ShowHitMarker(true);  // Headshot X Mark
                }
                else if (hit.collider.CompareTag("Body") || hit.collider.CompareTag("Enemy"))
                {
                    DynamicCrosshair.Instance.ShowHitMarker(false); // Normal X Mark
                }
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
    //private void LateUpdate()
    //{
    //    // VI: Kiểm tra xem có đang rút súng ra không
    //    // EN: Check if the weapon is currently drawn
    //    if (!isWeaponDrawn || _currentWeaponInstance == null || _activeGunData == null) return;

    //    Transform barrel = _currentWeaponInstance.bulletSpawnPoint;
    //    Transform handSocket = _currentWeaponInstance.transform.parent; // Lấy cái Socket chứa súng / Get the Socket holding the gun

    //    if (barrel == null || handSocket == null) return;

    //    // 1. Bắn tia lade ảo từ giữa Camera để tìm điểm hồng tâm
    //    // 1. Shoot a virtual laser from Camera center to find crosshair point
    //    Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    //    Vector3 targetPoint;

    //    // Kéo dài tia quét ra 100m để xem có chạm tường/quái không
    //    // Extend raycast to 100m to see if it hits walls/monsters
    //    if (Physics.Raycast(ray, out RaycastHit hit, 100f, _activeGunData.hitMask))
    //    {
    //        targetPoint = hit.point;
    //    }
    //    else
    //    {
    //        targetPoint = ray.GetPoint(50f); // Nếu không trúng gì, lấy 1 điểm ảo cách xa 50m / If hits nothing, take a virtual point 50m away
    //    }

    //    Vector3 directionToTarget = (targetPoint - barrel.position).normalized;

    //    // 2. Tính góc lệch và bẻ khớp cổ tay
    //    // 2. Calculate rotation offset and bend the wrist joint
    //    Quaternion correctionRot = Quaternion.FromToRotation(barrel.forward, directionToTarget);
    //    handSocket.rotation = Quaternion.Slerp(handSocket.rotation, correctionRot * handSocket.rotation, Time.deltaTime * 20f);
    //}
    private void TriggerWeaponJuice()
    {
        if (_activeGunData == null) return;

        // VI: Thêm tính ngẫu nhiên để mỗi phát bắn đều có độ lệch nhẹ khác nhau, tạo cảm giác tay thật
        // EN: Add randomness so each shot has a slight different variance, creating a real-hand feel

        // 1. ÉP LỰC ĐẨY LÙI (Punch Force)
        _targetWeaponRecoilPos += new Vector3(
            Random.Range(-_activeGunData.recoilKickPosition.x, _activeGunData.recoilKickPosition.x),
            Random.Range(-_activeGunData.recoilKickPosition.y, _activeGunData.recoilKickPosition.y),
            _activeGunData.recoilKickPosition.z // Luôn đẩy lùi mạnh về trục Z âm / Always hard push back on negative Z
        );

        // 2. ÉP LỰC HẤT NÒNG (Muzzle Flip)
        _targetWeaponRecoilRot += new Vector3(
            _activeGunData.recoilKickRotation.x, // Luôn hếch mũi súng lên (X âm) / Always pitch muzzle up (negative X)
            Random.Range(-_activeGunData.recoilKickRotation.y, _activeGunData.recoilKickRotation.y), // Lắc ngang ngẫu nhiên / Random yaw
            Random.Range(-_activeGunData.recoilKickRotation.z, _activeGunData.recoilKickRotation.z)  // Xoắn nòng ngẫu nhiên / Random roll
        );
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

        instance.emitting = false; //
        _trailPool.Release(instance); // Hoàn trả về bộ nhớ đệm /
    }
}