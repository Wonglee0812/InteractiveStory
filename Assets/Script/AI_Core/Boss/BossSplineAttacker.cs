using UnityEngine;
using UnityEngine.Splines;
using Unity.Cinemachine;
using System.Collections;

public class BossSplineAttacker : MonoBehaviour
{
    [Header("--- SYSTEM REFERENCES ---")]
    public SplineContainer splineContainer;
    public CinemachineSplineCart splineCart;
    public Transform playerTarget;

    [Header("--- FLIGHT TRAJECTORY ---")]
    public float emergeDistance = 30f;
    public float undergroundDepth = -15f;
    public float apexHeight = 5f;

    [Header("--- PACING & SPEED ---")]
    public float flightSpeed = 40f;
    public float cooldownBetweenAttacks = 3f;

    private bool _isAttacking = false;

    void Start()
    {
        if (splineContainer.Spline == null) splineContainer.AddSpline();
        splineCart.SplinePosition = 0f;
        StartCoroutine(AttackLoop());
    }

    private IEnumerator AttackLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(cooldownBetweenAttacks);
            if (playerTarget != null && !_isAttacking)
            {
                CalculateDynamicArc();
                yield return StartCoroutine(ExecuteFlight());
            }
        }
    }

    private void CalculateDynamicArc()
    {
        // ========================================================
        // 1. TÍNH TOÁN TỌA ĐỘ THẾ GIỚI (WORLD SPACE CALCULATION)
        // ========================================================
        Vector3 playerPos = playerTarget.position;

        Vector3 randomRange = Random.insideUnitSphere * emergeDistance;
        randomRange.y = 0;

        Vector3 p0_World = playerPos + randomRange;
        Vector3 p2_World = playerPos - randomRange;

        p0_World.y = undergroundDepth;
        Vector3 p1_World = playerPos + (Vector3.up * apexHeight);
        p2_World.y = undergroundDepth;

        // ========================================================
        // 2. CHUYỂN ĐỔI SANG TỌA ĐỘ CỤC BỘ (CONVERT TO LOCAL SPACE)
        // ========================================================
        Transform splineTransform = splineContainer.transform;
        Vector3 p0_Local = splineTransform.InverseTransformPoint(p0_World);
        Vector3 p1_Local = splineTransform.InverseTransformPoint(p1_World);
        Vector3 p2_Local = splineTransform.InverseTransformPoint(p2_World);

        // ========================================================
        // 3. [FIXED] TẠO SPLINE HOÀN TOÀN MỚI ĐỂ XÓA CACHE
        // 3. [FIXED] CREATE COMPLETELY NEW SPLINE TO CLEAR CACHE
        // ========================================================
        Spline newSpline = new Spline();

        newSpline.Add(new BezierKnot(p0_Local));
        newSpline.Add(new BezierKnot(p1_Local));
        newSpline.Add(new BezierKnot(p2_Local));

        newSpline.SetTangentMode(new SplineRange(0, newSpline.Count), TangentMode.AutoSmooth);

        // VI: Hành động gán này ép CinemachineSplineCart phải tính toán lại đường ray!
        // EN: This assignment action forces the CinemachineSplineCart to recalculate the track!
        splineContainer.Spline = newSpline;
    }

    private IEnumerator ExecuteFlight()
    {
        _isAttacking = true;

        // VI: Tính toán thời gian bay dựa trên vận tốc
        // EN: Calculate flight time based on speed
        float pathLength = splineContainer.CalculateLength();
        float travelTime = pathLength / flightSpeed;

        float normalizedTime = 0f;

        // VI: Vòng lặp đơn giản chạy Cart từ 0.0 đến 1.0 (0% đến 100%)
        // EN: Simple loop running Cart from 0.0 to 1.0 (0% to 100%)
        while (normalizedTime < 1f)
        {
            normalizedTime += Time.deltaTime / travelTime;
            splineCart.SplinePosition = normalizedTime;
            yield return null;
        }

        _isAttacking = false;
    }
}