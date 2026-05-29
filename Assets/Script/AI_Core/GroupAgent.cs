using System.Collections.Generic;
using UnityEngine;
using FluidHTN;
using System.Linq;

public class GroupAgent : MonoBehaviour
{
    [Header("Herd Members")]
    public List<AIAgent> Members = new List<AIAgent>();

    [Header("Combat Management")]
    public int MaxSimultaneousAttacks = 2; // Số quái tối đa được cấp quyền đánh

    private AIAgent _currentInvestigator = null;

    public void RegisterAgent(AIAgent agent)
    {
        if (!Members.Contains(agent))
        {
            Members.Add(agent);
            agent.Context.Group = this;
        }
    }

    void Update()
    {
        ManageCombatTokens();
    }

    private void ManageCombatTokens()
    {
        Members.RemoveAll(a => a == null || a.Equals(null) || a.gameObject == null || !a.gameObject.activeInHierarchy);
        if (Members.Count == 0) return;

        var combatAgents = Members.Where(a => a != null && a.Context.HasState(AIWorldState.HasTarget)).ToList();
        if (combatAgents.Count == 0) return;

        // Đếm số vé đang lưu hành
        int activeCombatTokens = combatAgents.Count(a => a.Context.HasState(AIWorldState.HasCombatToken));

        // NẾU CÒN DƯ VÉ -> Phát cho đứa thèm khát nhất (Tạm tính là đứa gần nhất)
        if (activeCombatTokens < MaxSimultaneousAttacks)
        {
            var candidate = combatAgents
                .Where(a => !a.Context.HasState(AIWorldState.HasCombatToken))
                .OrderBy(a => Vector3.Distance(a.transform.position, a.Context.Target.position))
                .FirstOrDefault();

            if (candidate != null)
            {
                candidate.Context.SetState(AIWorldState.HasCombatToken, true, EffectType.Permanent);
            }
        }
    }

    public void BroadcastPlayerLocation(Vector3 location)
    {
        foreach (var agent in Members)
        {
            if (agent == null) continue;
            agent.Context.LastKnownPosition = location;

            // Xóa vé điều tra cũ, thu hồi vé combat cũ để phân chia lại
            agent.Context.SetState(AIWorldState.HasInvestigateToken, false, EffectType.Permanent);
            agent.Context.SetState(AIWorldState.HasCombatToken, false, EffectType.Permanent);

            if (!agent.Context.HasState(AIWorldState.HasTarget))
            {
                agent.Context.SuspectedLocation = location;
                agent.Context.SetState(AIWorldState.HasSuspectedLocation, true, EffectType.Permanent);
            }
        }
    }

    public void ReportDisturbance(Vector3 disturbanceLocation, AIAgent reporter)
    {
        if (_currentInvestigator != null && _currentInvestigator.Context.HasState(AIWorldState.HasInvestigateToken)) return;

        AIAgent bestCandidate = reporter;
        float minDistance = float.MaxValue;

        foreach (var agent in Members)
        {
            if (agent == null || agent.Context.HasState(AIWorldState.HasTarget)) continue;
            float dist = Vector3.Distance(agent.transform.position, disturbanceLocation);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestCandidate = agent;
            }
        }

        _currentInvestigator = bestCandidate;
        foreach (var agent in Members)
        {
            if (agent == null) continue;
            bool isInvestigator = (agent == _currentInvestigator);
            agent.Context.SetState(AIWorldState.HasInvestigateToken, isInvestigator, EffectType.Permanent);
            agent.Context.SuspectedLocation = disturbanceLocation;
            agent.Context.SetState(AIWorldState.HasSuspectedLocation, true, EffectType.Permanent);
        }
    }
}