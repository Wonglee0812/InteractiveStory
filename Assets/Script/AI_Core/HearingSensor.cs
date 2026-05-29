using UnityEngine;
using FluidHTN;

[RequireComponent(typeof(AIAgent))]
public class HearingSensor : MonoBehaviour
{
    [Header("Hearing Settings")]
    public float hearingSensitivityRadius = 15f;
    private AIAgent _agent;

    void Awake()
    {
        _agent = GetComponent<AIAgent>();
    }

    void OnEnable()
    {
        AIAuditorySystem.OnSoundReported += OnHeardSound;
    }

    void OnDisable()
    {
        AIAuditorySystem.OnSoundReported -= OnHeardSound;
    }

    private void OnHeardSound(Vector3 soundPosition, float radius)
    {
        if (_agent == null || _agent.Context == null) return;

        float sqrDist = (transform.position - soundPosition).sqrMagnitude;
        // Logic cộng dồn bán kính âm thanh của sếp
        float effectiveRadius = hearingSensitivityRadius + radius;
        float sensitivitySqr = effectiveRadius * effectiveRadius;

        if (sqrDist <= sensitivitySqr)
        {
            if (_agent.Context.Group != null)
            {
                // [CHUẨN AAA TẦNG 1]: Tuyệt đối không tự xử lý! Chỉ báo cáo tọa độ lên Bộ Chỉ Huy
                _agent.Context.Group.ReportDisturbance(soundPosition, _agent);
            }
            else
            {
                // Fallback: Trong trường hợp sếp test 1 con lẻ chưa có Group
                _agent.Context.SuspectedLocation = soundPosition;
                _agent.Context.SetState(AIWorldState.HasSuspectedLocation, true, EffectType.Permanent);
                _agent.Context.SetState(AIWorldState.HasInvestigateToken, true, EffectType.Permanent);
            }
        }
    }
}