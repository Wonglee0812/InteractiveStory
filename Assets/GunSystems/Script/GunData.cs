using System.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(fileName = "Gun", menuName = "Weapons/Gun Data", order = 0)]
public class GunData : ScriptableObject
{
    [Header("Base")]
    public GunType Type;
    public string gunName = "New Gun";
    public float damage = 20f;
    public bool isAutomatic = true;
    public float soundRadius;

    [Space]
    [Header("Stats")]
    public GameObject ModelPrefabs;
    public Vector3 SpawnPoint;
    public Vector3 SpawnRotation;
     

    [Space]
    [Header("Shoot Config")]
    public LayerMask hitMask;
    public Vector3 Spread;
    public float fireRate = 0.1f;


    [Space]
    [Header("Trail Configuration")]
    public TrailConfigScriptableObject trailConfig;

    private MonoBehaviour ActiveMonoBehaviour;
    private float LastShootTime;
    private ParticleSystem ShootSystem;
    private GameObject Model;
    private ObjectPool<TrailRenderer> TrailPool;

    public void Spawn(Transform Parent, MonoBehaviour ActiveMonoBehaviour)
    {
        this.ActiveMonoBehaviour = ActiveMonoBehaviour;
        LastShootTime = 0;
        TrailPool = new ObjectPool<TrailRenderer>(CreateTrail);

        Model = Instantiate(ModelPrefabs);
        Model.transform.SetParent(Parent, false);
        Model.transform.localPosition = SpawnPoint;
        Model.transform.localRotation = Quaternion.Euler(SpawnRotation);

        ShootSystem = Model.GetComponentInChildren <ParticleSystem>();
    }    

    public void Shoot()
    {
        if(Time.time > fireRate + LastShootTime)
        {
            AIAuditorySystem.ReportSound(ShootSystem.transform.position, soundRadius);
            LastShootTime = Time.time;
            ShootSystem.Play();

            var playerCamera = Camera.main;
            Ray baseRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 spreadVector = (playerCamera.transform.right * Random.Range(-Spread.x, Spread.x)) +
                                   (playerCamera.transform.up * Random.Range(-Spread.y, Spread.y)) +
                                   (playerCamera.transform.forward * Random.Range(-Spread.z, Spread.z));
            Vector3 finalDirection = (baseRay.direction + spreadVector).normalized;
            Ray finalRay = new Ray(baseRay.origin, finalDirection);
            Vector3 startTrailPoint = (ShootSystem != null) ? ShootSystem.transform.position : baseRay.origin;
            if (Physics.Raycast(finalRay, out RaycastHit hit, float.MaxValue, hitMask))
            {
                IDamageable target = hit.collider.GetComponent<IDamageable>();
                if (target != null) target.TakeDamage(damage, hit.point);
                if (SurfaceManager.Instance != null)
                {
                    SurfaceManager.Instance.PlayImpact(hit.point, hit.normal, hit.collider.tag);
                }
                ActiveMonoBehaviour.StartCoroutine(PlayTrail(ShootSystem.transform.position, hit.point, hit));

            }
            else 
            {
                Vector3 targetPoint = finalRay.origin + (finalDirection * trailConfig.MissDisstance);
                ActiveMonoBehaviour.StartCoroutine(PlayTrail(ShootSystem.transform.position,targetPoint, new RaycastHit()));
            }
        }
    }

    private IEnumerator PlayTrail(Vector3 StartPoint, Vector3 Endpoint, RaycastHit Hit)
    {
        TrailRenderer instance = TrailPool.Get();
        instance.gameObject.SetActive(true);
        instance.transform.position = StartPoint;
        yield return null; 

        instance.emitting = true;

        float distance = Vector3.Distance(StartPoint, Endpoint);
        float remainDistance = distance;
        while(remainDistance > 0)
        {
            instance.transform.position = Vector3.Lerp(StartPoint, Endpoint, Mathf.Clamp01(1 - (remainDistance / distance)));

            remainDistance -= trailConfig.StimlationSpeed * Time.deltaTime;
            yield return null;
        }

        instance.transform.position = Endpoint;

        if(Hit.collider != null)
        {
            Debug.Log("Thiếu hàm xử lý va chạm");
        }    

        yield return new WaitForSeconds(trailConfig.Duration);
        yield return null;
        AIAuditorySystem.ReportSound(Hit.point, 100f);
        instance.emitting = false;
        instance.gameObject.SetActive(false);
        TrailPool.Release(instance); 

    }    

    private TrailRenderer CreateTrail()
    {
        GameObject instance = new GameObject("Bullet Trail");
        TrailRenderer trail = instance.AddComponent<TrailRenderer>();
        trail.colorGradient = trailConfig.Color;
        trail.material = trailConfig.Materail;
        trail.widthCurve = trailConfig.WidthCurve;
        trail.time = trailConfig.Duration;
        trail.minVertexDistance = trailConfig.MinVertexDistance;
        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return trail;
    }



    //AI
    public void ShootAtAI(Vector3 startPoint, Vector3 targetPoint, MonoBehaviour activeMono)
    {
        if (Time.time > fireRate + LastShootTime)
        {
            LastShootTime = Time.time;

            // Nếu có tia lửa thì bật, không có thì bỏ qua (KHÔNG CRASH)
            if (ShootSystem != null) ShootSystem.Play();

            // [VI] SỬA Ở ĐÂY: Dùng startPoint thay vì ShootSystem.transform.position
            // [EN] FIX HERE: Use startPoint instead of ShootSystem.transform.position
            Vector3 baseDirection = (targetPoint - startPoint).normalized;

            Vector3 spreadVector = new Vector3(
                Random.Range(-Spread.x, Spread.x),
                Random.Range(-Spread.y, Spread.y),
                Random.Range(-Spread.z, Spread.z)
            );
            Vector3 finalDirection = (baseDirection + spreadVector).normalized;
            Ray finalRay = new Ray(startPoint, finalDirection);

            if (Physics.Raycast(finalRay, out RaycastHit hit, float.MaxValue, hitMask))
            {
                IDamageable target = hit.collider.GetComponent<IDamageable>();
                if (target != null) target.TakeDamage(damage, hit.point);
                if (SurfaceManager.Instance != null)
                {
                    SurfaceManager.Instance.PlayImpact(hit.point, hit.normal, hit.collider.tag);
                }
                activeMono.StartCoroutine(PlayTrail(startPoint, hit.point, hit));
            }
            else
            {
                Vector3 missTarget = finalRay.origin + (finalDirection * trailConfig.MissDisstance);
                activeMono.StartCoroutine(PlayTrail(startPoint, missTarget, new RaycastHit()));
            }
        }
    }
}