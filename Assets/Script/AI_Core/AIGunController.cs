using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class AIGunController : MonoBehaviour
{
    [Header("--- AI WEAPON STATS ---")]
    public float damage = 20f;
    public float fireRate = 0.15f;
    public float soundRadius = 30f;
    public Vector3 spread = new Vector3(0.5f, 0.5f, 0.5f);
    public LayerMask hitMask;

    [Header("--- VFX & REFS ---")]
    public Transform barrelTransform;
    public ParticleSystem shootSystem;
    public TrailConfigScriptableObject trailConfig;

    private float _lastShootTime;
    private ObjectPool<TrailRenderer> _trailPool;

    void Start()
    {
        _trailPool = new ObjectPool<TrailRenderer>(CreateTrail);
    }

    public void ShootAt(Vector3 targetPoint)
    {
        if (Time.time < _lastShootTime + fireRate) return;
        _lastShootTime = Time.time;

        if (shootSystem != null) shootSystem.Play();
        AIAuditorySystem.ReportSound(barrelTransform.position, soundRadius);

        Vector3 baseDir = (targetPoint - barrelTransform.position).normalized;
        Vector3 spreadVec = new Vector3(
            Random.Range(-spread.x, spread.x),
            Random.Range(-spread.y, spread.y),
            Random.Range(-spread.z, spread.z)
        );
        Vector3 finalDir = (baseDir + spreadVec).normalized;
        Ray ray = new Ray(barrelTransform.position, finalDir);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, hitMask))
        {
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            if (target != null) target.TakeDamage(damage, hit.point, hit.normal);

            // Tích hợp Surface Manager như hệ thống cũ của bạn / Integrating Surface Manager like your old system
            if (SurfaceManager.Instance != null)
                SurfaceManager.Instance.PlayImpact(hit.point, hit.normal, hit.collider.tag);

            StartCoroutine(PlayTrail(barrelTransform.position, hit.point, hit));
        }
        else
        {
            Vector3 missTarget = ray.origin + (finalDir * (trailConfig != null ? trailConfig.MissDisstance : 50f));
            StartCoroutine(PlayTrail(barrelTransform.position, missTarget, new RaycastHit()));
        }
    }

    private IEnumerator PlayTrail(Vector3 startPoint, Vector3 endPoint, RaycastHit hit)
    {
        if (trailConfig == null) yield break;

        TrailRenderer instance = _trailPool.Get();
        instance.gameObject.SetActive(true);
        instance.transform.position = startPoint;
        yield return null;

        instance.emitting = true;
        float distance = Vector3.Distance(startPoint, endPoint);
        float remain = distance;

        while (remain > 0)
        {
            instance.transform.position = Vector3.Lerp(startPoint, endPoint, Mathf.Clamp01(1 - (remain / distance)));
            remain -= trailConfig.StimlationSpeed * Time.deltaTime;
            yield return null;
        }

        instance.transform.position = endPoint;
        yield return new WaitForSeconds(trailConfig.Duration);

        if (hit.collider != null) AIAuditorySystem.ReportSound(endPoint, 15f);

        instance.emitting = false;
        instance.gameObject.SetActive(false);
        _trailPool.Release(instance);
    }

    private TrailRenderer CreateTrail()
    {
        GameObject instance = new GameObject("AI Bullet Trail");
        TrailRenderer trail = instance.AddComponent<TrailRenderer>();
        if (trailConfig != null)
        {
            trail.colorGradient = trailConfig.Color;
            trail.material = trailConfig.Materail;
            trail.widthCurve = trailConfig.WidthCurve;
            trail.time = trailConfig.Duration;
            trail.minVertexDistance = trailConfig.MinVertexDistance;
        }
        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return trail;
    }
}