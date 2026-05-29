using UnityEngine;
using FluidHTN;

[RequireComponent(typeof(AIAgent))]
public class ProximitySensor : MonoBehaviour
{
    [Header("Proximity Settings")]
    public float meleeRadius = 2.5f;
    public LayerMask playerLayerMask;

    private AIAgent _agent;
    private Collider[] _hitColliders = new Collider[5];
    private float _scanInterval = 0.25f;
    private float _timer = 0f;

    void Awake()
    {
        _agent = GetComponent<AIAgent>();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _scanInterval)
        {
            _timer = 0f;

            int hits = Physics.OverlapSphereNonAlloc(transform.position, meleeRadius, _hitColliders, playerLayerMask);
            if (hits > 0)
            {
                // SỬA LỖI BIÊN DỊCH: Truy xuất phần tử [0] của mảng thay vì gọi thẳng mảng!
                Transform player = _hitColliders[0].transform;

                _agent.Context.Target = player;
                _agent.Context.SetState(AIWorldState.HasTarget, true, EffectType.Permanent);
                _agent.Context.SetState(AIWorldState.IsAlerted, true, EffectType.Permanent);

                // Kích hoạt Hive Mind cho cả bầy đàn vây bắt
                if (_agent.Context.Group != null)
                {
                    _agent.Context.Group.BroadcastPlayerLocation(player.position);
                }
            }
        }
    }
}