using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using FluidHTN;
using DG.Tweening;
using TaskStatus = FluidHTN.TaskStatus;

[System.Serializable]
public class ActiveSkill
{
    public AISkillData data;
    public float lastCastTime = -999f;
    public bool IsCooldownReady() => Time.time >= (lastCastTime + data.cooldown);
    public void RecordCastTime() => lastCastTime = Time.time;
}

public class AISkillController : MonoBehaviour
{
    [Header("--- ARSENAL ---")]
    public List<ActiveSkill> availableSkills = new List<ActiveSkill>();
    public float momentumBonus = 0.2f;

    [Header("--- RANGED COMBAT REFS ---")]
    public GunController aiGunController;
    public Transform bodyMesh; // VI: Để tạo hiệu ứng giật lùi (recoil) nếu cần / EN: For recoil effects if needed

    public TaskStatus CurrentSkillStatus { get; private set; } = TaskStatus.Success;

    private AIAgent _agent;
    private ActiveSkill _currentSkill;
    private ActiveSkill _lastSelectedSkill;
    private int _phase = 0;
    private float _timer = 0f;
    private Sequence _currentSkillSeq;
    private Vector3 _originalBodyPos;

    void Awake()
    {
        _agent = GetComponent<AIAgent>();
        if (bodyMesh != null) _originalBodyPos = bodyMesh.localPosition;
    }

    public void StartBestSkill(Transform target)
    {
        _currentSkill = AIUtilityEvaluator.EvaluateBestSkill(availableSkills, transform, target, _lastSelectedSkill, momentumBonus);
        _lastSelectedSkill = _currentSkill;

        if (_currentSkill == null)
        {
            CurrentSkillStatus = TaskStatus.Failure;
            return;
        }

        CurrentSkillStatus = TaskStatus.Continue;
        _phase = 0;
        _timer = 0f;

        if (_agent.NavAgent != null)
        {
            _agent.NavAgent.ResetPath();
            _agent.NavAgent.updateRotation = false; // VI: Tự xoay tay / EN: Manual rotation
        }

        _agent.Context.CurrentExecutingSkill = _currentSkill.data;
        _agent.Context.LookFocusOverride = target.position + Vector3.up * 1.2f;
    }

    void Update()
    {
        if (CurrentSkillStatus != TaskStatus.Continue || _currentSkill == null || _agent.Context.Target == null) return;

        _timer += Time.deltaTime;

        if (_phase == 0)
        {
            Vector3 dirToTarget = (_agent.Context.Target.position - transform.position).normalized;
            dirToTarget.y = 0;
            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

            if (angleToTarget <= _currentSkill.data.weaponFOV / 2f)
            {
                _phase = 1;
                ExecuteRangedChoreography();
            }
            else
            {
                _agent.FaceTargetLazy(_agent.Context.Target.position, _agent.TacticalSettings.combatTurnSpeed);
                if (_timer > 3f) AbortExecution(TaskStatus.Failure);
            }
        }
    }

    private void ExecuteRangedChoreography()
    {
        _agent.Context.LookFocusOverride = null;

        if (_currentSkillSeq != null && _currentSkillSeq.IsActive()) _currentSkillSeq.Kill();
        _currentSkillSeq = DOTween.Sequence();

        // VI: Animation nạp đạn/ngắm bắn (WindUp)
        // EN: Reload/Aiming animation (WindUp)
        _currentSkillSeq.AppendInterval(_currentSkill.data.windUpTime);

        int strafeDir = UnityEngine.Random.value > 0.5f ? 1 : -1;

        // VI: Giai đoạn xả đạn (Execute)
        // EN: Firing phase (Execute)
        _currentSkillSeq.AppendInterval(_currentSkill.data.executeTime)
            .OnUpdate(() =>
            {
                if (_agent != null && _agent.Context.Target != null)
                {
                    // VI: Giữ cơ thể xoay về phía Player
                    // EN: Keep body rotated towards Player
                    _agent.FaceTargetLazy(_agent.Context.Target.position, _agent.TacticalSettings.combatTurnSpeed);

                    if (aiGunController != null)
                    {
                        Vector3 aimTarget = _agent.Context.Target.position + (Vector3.up * 1.2f);
                        aiGunController.ShootAtAI(aimTarget);
                    }

                    // VI: Trượt ngang (Strafe) né đạn trong lúc bắn
                    // EN: Strafe sideways to dodge bullets while shooting
                    if (_agent.NavAgent != null && _agent.NavAgent.isOnNavMesh)
                    {
                        Vector3 dirToTarget = (_agent.Context.Target.position - transform.position).normalized;
                        Vector3 rightDir = Vector3.Cross(dirToTarget, Vector3.up) * strafeDir;
                        _agent.NavAgent.Move(rightDir * _agent.TacticalSettings.prowlSpeed * Time.deltaTime);
                    }
                }
            });

        // VI: Thời gian hồi chiêu/thu súng (Recovery)
        // EN: Cooldown/Holster time (Recovery)
        _currentSkillSeq.AppendInterval(_currentSkill.data.recoveryTime);

        _currentSkillSeq.OnComplete(() =>
        {
            _currentSkill.RecordCastTime();
            AbortExecution(TaskStatus.Success);
        });
    }

    public void AbortExecution(TaskStatus finalStatus = TaskStatus.Failure)
    {
        CurrentSkillStatus = finalStatus;

        if (_currentSkillSeq != null && _currentSkillSeq.IsActive()) _currentSkillSeq.Kill();
        if (bodyMesh != null) bodyMesh.localPosition = _originalBodyPos;

        if (_agent != null && _agent.NavAgent != null)
        {
            _agent.NavAgent.enabled = true;
            _agent.NavAgent.updatePosition = true;
            _agent.NavAgent.updateRotation = true;
            if (_agent.NavAgent.isOnNavMesh) _agent.NavAgent.Warp(transform.position);
        }

        // VI: ĐÃ DỌN SẠCH CÁC THAM CHIẾU LỖI ĐẾN PROCEDURAL BODY TẠI ĐÂY!
        // EN: COMPLETELY CLEARED ERRONEOUS REFERENCES TO PROCEDURAL BODY HERE!

        _currentSkill = null;
        if (_agent != null && _agent.Context != null)
        {
            _agent.Context.CurrentExecutingSkill = null;
            _agent.Context.LookFocusOverride = null;
        }
    }

    public void AddSkill(AISkillData data) { if (!availableSkills.Exists(s => s.data == data)) availableSkills.Add(new ActiveSkill { data = data }); }
    public void RemoveSkill(AISkillData data) { availableSkills.RemoveAll(s => s.data == data); }
}