using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using FluidHTN;
using TaskStatus = FluidHTN.TaskStatus;

public class AIGunController : MonoBehaviour
{
    public enum AIBodyType { Humanoid, Mech }

    [Header("--- CORE CONFIG ---")]
    public AIBodyType bodyType;
    [Tooltip("VI: Kéo file GunData chung với Player vào đây / EN: Drag the shared GunData file here")]
    public GunData equippedGun;

    [Header("--- HUMANOID SETUP (Ignore if Mech) ---")]
    public Transform handSocket;
    private RuntimeWeapon _spawnedWeapon;

    [Header("--- MECH SETUP (Ignore if Human) ---")]
    public Transform mechBarrel;
    public ParticleSystem mechMuzzleFlash;

    [Header("--- COMBAT BEHAVIOR ---")]
    public float burstDuration = 2f;    // Thời gian xả đạn / Time spent firing
    public float recoveryTime = 1.5f;   // Thời gian nghỉ / Cooldown time

    // ==========================================
    // HTN BRAIN INTEGRATION
    // ==========================================
    public TaskStatus CurrentCombatStatus { get; private set; } = TaskStatus.Success;
    private AIAgent _agent;
    private Transform _target;
    private int _phase;
    private float _timer;

    // ==========================================
    // GUN MECHANICS
    // ==========================================
    private ObjectPool<TrailRenderer> _trailPool;
    private float _lastShootTime;

    private AIAnimatorBridge _animBridge;
    private VisionSensor _visionSensor;
    private int _strafeDir;

    void Awake()
    {
        _agent = GetComponent<AIAgent>();
        _animBridge = GetComponent<AIAnimatorBridge>();
        _visionSensor = GetComponent<VisionSensor>();
    }
    void Start()
    {
        _trailPool = new ObjectPool<TrailRenderer>(CreateTrail, OnTake, OnReturn, OnDestroyTrail);
        InitializeWeapon();
    }

    private void InitializeWeapon()
    {
        if (equippedGun == null) return;

        // VI: Chỉ sinh ra súng vật lý nếu đây là hình người
        // EN: Only spawn physical gun if this is a humanoid
        if (bodyType == AIBodyType.Humanoid && equippedGun.modelPrefab != null && handSocket != null)
        {
            GameObject obj = Instantiate(equippedGun.modelPrefab, handSocket);
            obj.transform.localPosition = equippedGun.holdPositionOffset;
            obj.transform.localRotation = Quaternion.Euler(equippedGun.holdRotationOffset);
            obj.transform.localScale = equippedGun.baseScale;
            _spawnedWeapon = obj.GetComponent<RuntimeWeapon>();
        }
    }

    // ==========================================
    // KẾT NỐI VỚI HTN OPERATOR / HTN OPERATOR CONNECTION
    // ==========================================
    public void StartCombatSequence(Transform target)
    {
        if (equippedGun == null)
        {
            AbortCombat(TaskStatus.Failure);
            return;
        }

        _target = target;
        CurrentCombatStatus = TaskStatus.Continue;

        // [VI] Human thì cần thời gian ngắm (Phase 0). Mech thì xả đạn luôn (Phase 1).
        // [EN] Human needs aiming time (Phase 0). Mech shoots instantly (Phase 1).
        _phase = (bodyType == AIBodyType.Humanoid) ? 0 : 1;
        _timer = 0f;
        _strafeDir = Random.value > 0.5f ? 1 : -1; // Random hướng trượt ngang / Random strafe dir

        if (_agent.NavAgent != null && _agent.NavAgent.isOnNavMesh)
        {
            _agent.NavAgent.ResetPath();
            _agent.NavAgent.velocity = Vector3.zero;
        }
    }
    private void LateUpdate()
    {
        // VI: Chỉ nắn nòng súng nếu đang trong Combat và là hình người
        // EN: Only align barrel if in Combat and is humanoid
        if (CurrentCombatStatus != TaskStatus.Continue || _target == null) return;
        if (bodyType != AIBodyType.Humanoid || _spawnedWeapon == null || handSocket == null) return;

        Transform barrel = _spawnedWeapon.bulletSpawnPoint;
        if (barrel == null) return;

        // 1. Xác định điểm cần bắn trúng (Ngực của Player)
        // 1. Determine the exact hit point (Player's chest)
        Vector3 targetPoint = _target.position + Vector3.up * 1.2f;
        Vector3 directionToTarget = (targetPoint - barrel.position).normalized;

        // 2. Phép màu Toán học: Tính góc lệch giữa hướng nòng súng thực tế và hướng cần bắn
        // 2. Math Magic: Calculate the angle difference between actual barrel dir and target dir
        Quaternion correctionRot = Quaternion.FromToRotation(barrel.forward, directionToTarget);

        // 3. Nhân góc lệch này vào khớp tay để bẻ nó mượt mà (Không làm hỏng Recoil)
        // 3. Multiply this offset into the hand socket to bend it smoothly (Doesn't break Recoil)
        handSocket.rotation = Quaternion.Slerp(handSocket.rotation, correctionRot * handSocket.rotation, Time.deltaTime * 15f);
    }
    void Update()
    {
        if (CurrentCombatStatus != TaskStatus.Continue || _target == null) return;

        _timer += Time.deltaTime;

        // 1. Luôn vặn người hướng về Player / Always turn towards Player
        _agent.FaceTargetLazy(_target.position, _agent.TacticalSettings.combatTurnSpeed, 0f, 0f);

        // 2. [VI] Tính góc lệch ngang chuẩn xác (Loại bỏ trục Y để không bị nhiễu)
        // 2. [EN] Calculate accurate flat angle (Strip Y axis to avoid distortion)
        Vector3 currentPos = transform.position; currentPos.y = 0;
        Vector3 targetPos = _target.position; targetPos.y = 0;
        Vector3 dirToTarget = (targetPos - currentPos).normalized;

        Vector3 currentForward = transform.forward; currentForward.y = 0; currentForward.Normalize();

        float angleToTarget = Vector3.Angle(currentForward, dirToTarget);

        // ==========================================
        // PHASE 0: ĐƯA SÚNG LÊN NGẮM / AIM WIND-UP
        // ==========================================
        if (_phase == 0)
        {
            if (_animBridge != null) _animBridge.SetAiming(true);

            if (_timer >= 0.2f && angleToTarget <= 30f)
            {
                _phase = 1; _timer = 0f;
            }
            else if (_timer >= 3f)
            {
                AbortCombat(TaskStatus.Failure);
            }
        }
        // ==========================================
        // PHASE 1: XẢ ĐẠN VÀ TRƯỢT NGANG / SHOOTING & STRAFING
        // ==========================================
        else if (_phase == 1)
        {
            // Trượt ngang né đạn / Strafing
            if (_agent.NavAgent != null && _agent.NavAgent.isOnNavMesh)
            {
                Vector3 rightDir = Vector3.Cross(dirToTarget, Vector3.up) * _strafeDir;
                float combatStrafeSpeed = _agent.TacticalSettings.prowlSpeed * 0.6f;
                _agent.NavAgent.velocity = rightDir * combatStrafeSpeed;
            }

            // [VI] FIX 1: Đẩy điểm quét tia thẳng ra xa 0.5m khỏi cơ thể để không tự đụng chính mình!
            // [EN] FIX 1: Push the linecast start point 0.5m forward to prevent self-collision!
            bool isBlocked = false;
            if (_visionSensor != null && _visionSensor.eyeSource != null)
            {
                Vector3 eyePos = _visionSensor.eyeSource.position + transform.forward * 0.5f;
                Vector3 targetChest = _target.position + Vector3.up * 1.0f;
                isBlocked = Physics.Linecast(eyePos, targetChest, _visionSensor.obstacleLayerMask);
            }

            if (isBlocked)
            {
                AbortCombat(TaskStatus.Success);
                return;
            }

            // [VI] FIX 2: Bỏ lệnh Hủy Combat khi lệch góc. Chỉ cần góc <= 45 độ là cho phép nhả đạn!
            // [EN] FIX 2: Removed Combat Abort on angle offset. Just allow firing if angle <= 45 degrees!
            if (angleToTarget <= 45f)
            {
                ShootAtTarget();
            }

            if (_timer >= burstDuration)
            {
                _phase = 2; _timer = 0f;
                if (_animBridge != null) _animBridge.SetAiming(false);
            }
        }
        // ==========================================
        // PHASE 2: NGHỈ LẤY HƠI / RECOVERY COOLDOWN
        // ==========================================
        else if (_phase == 2)
        {
            if (_timer >= Mathf.Min(recoveryTime, 0.5f))
            {
                AbortCombat(TaskStatus.Success);
            }
        }
    }

    // ==========================================
    // LOGIC BẮN SÚNG VẬT LÝ / PHYSICAL SHOOTING LOGIC
    // ==========================================
    private void ShootAtTarget()
    {
        if (Time.time < _lastShootTime + equippedGun.fireRate) return;
        _lastShootTime = Time.time;

        // Phân luồng lấy Nòng súng và Tia lửa / Route Barrel and Muzzle Flash
        Transform spawnPoint = (bodyType == AIBodyType.Humanoid && _spawnedWeapon != null) ? _spawnedWeapon.bulletSpawnPoint : mechBarrel;
        ParticleSystem pfx = (bodyType == AIBodyType.Humanoid && _spawnedWeapon != null) ? _spawnedWeapon.muzzleFlashVFX : mechMuzzleFlash;

        if (spawnPoint == null) return;

        if (pfx != null) pfx.Play();
        AIAuditorySystem.ReportSound(spawnPoint.position, equippedGun.soundRadius);

        // Kích hoạt Animation Giật người nếu là Human / Trigger Flinch Anim if Human
        if (bodyType == AIBodyType.Humanoid)
        {
            // [VI] ĐÃ FIX: Gọi sang AIAnimatorBridge thay vì PlayerCharater
            // [EN] FIXED: Call AIAnimatorBridge instead of PlayerCharater
            AIAnimatorBridge animBridge = GetComponent<AIAnimatorBridge>();
            if (animBridge != null) animBridge.PlayShootAnimation();
        }

        // Tính đường đạn có độ lệch / Calculate bullet path with spread
        Vector3 aimTarget = _target.position + Vector3.up * 1.2f;
        Vector3 baseDir = (aimTarget - spawnPoint.position).normalized;
        Vector3 spreadVec = new Vector3(
            Random.Range(-equippedGun.spread.x, equippedGun.spread.x),
            Random.Range(-equippedGun.spread.y, equippedGun.spread.y),
            Random.Range(-equippedGun.spread.z, equippedGun.spread.z)
        );
        Vector3 finalDir = (baseDir + spreadVec).normalized;

        Ray ray = new Ray(spawnPoint.position, finalDir);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, equippedGun.hitMask))
        {
            var damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null) damageable.TakeDamage(equippedGun.damage, hit.point);

            if (SurfaceManager.Instance != null) SurfaceManager.Instance.PlayImpact(hit.point, hit.normal, hit.collider.tag);
            StartCoroutine(PlayTrail(spawnPoint.position, hit.point, hit));
        }
        else
        {
            Vector3 missTarget = ray.origin + (finalDir * equippedGun.trailConfig.MissDisstance);
            StartCoroutine(PlayTrail(spawnPoint.position, missTarget, new RaycastHit()));
        }
    }

    public void AbortCombat(TaskStatus status = TaskStatus.Failure)
    {
        CurrentCombatStatus = status;
        _target = null;
    }

    // ==========================================
    // OBJECT POOLING TRAIL
    // ==========================================
    private TrailRenderer CreateTrail()
    {
        GameObject instance = new GameObject("AI Bullet Trail");
        TrailRenderer trail = instance.AddComponent<TrailRenderer>();
        if (equippedGun != null && equippedGun.trailConfig != null)
        {
            trail.colorGradient = equippedGun.trailConfig.Color;
            trail.material = equippedGun.trailConfig.Materail;
            trail.widthCurve = equippedGun.trailConfig.WidthCurve;
            trail.time = equippedGun.trailConfig.Duration;
            trail.minVertexDistance = equippedGun.trailConfig.MinVertexDistance;
        }
        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return trail;
    }

    private void OnTake(TrailRenderer trail) => trail.gameObject.SetActive(true);
    private void OnReturn(TrailRenderer trail) => trail.gameObject.SetActive(false);
    private void OnDestroyTrail(TrailRenderer trail) => Destroy(trail.gameObject);

    private IEnumerator PlayTrail(Vector3 startPoint, Vector3 endPoint, RaycastHit hit)
    {
        if (equippedGun == null || equippedGun.trailConfig == null) yield break;

        TrailRenderer instance = _trailPool.Get();
        instance.transform.position = startPoint;
        instance.Clear();
        instance.emitting = true;

        float distance = Vector3.Distance(startPoint, endPoint);
        float remain = distance;

        while (remain > 0)
        {
            instance.transform.position = Vector3.Lerp(startPoint, endPoint, Mathf.Clamp01(1 - (remain / distance)));
            remain -= equippedGun.trailConfig.StimlationSpeed * Time.deltaTime;
            yield return null;
        }

        instance.transform.position = endPoint;
        yield return new WaitForSeconds(equippedGun.trailConfig.Duration);

        if (hit.collider != null) AIAuditorySystem.ReportSound(endPoint, 15f);

        instance.emitting = false;
        _trailPool.Release(instance);
    }
}