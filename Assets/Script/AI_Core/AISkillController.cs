using System.Collections.Generic;
using UnityEngine;
using FluidHTN;
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
    [Tooltip("VI: Gắn script AIGunController mới vào đây.\nEN: Attach the new AIGunController script here.")]
    public AIGunController aiGunController;

    public TaskStatus CurrentSkillStatus { get; private set; } = TaskStatus.Success;

    private AIAgent _agent;
    private ActiveSkill _currentSkill;
    private ActiveSkill _lastSelectedSkill;

    private int _phase = 0;
    private float _timer = 0f;
    private int _strafeDir = 1;

    void Awake()
    {
        _agent = GetComponent<AIAgent>();
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

        // VI: Chọn ngẫu nhiên hướng trượt ngang cho lượt bắn này
        // EN: Randomly choose a strafing direction for this firing sequence
        _strafeDir = UnityEngine.Random.value > 0.5f ? 1 : -1;

        if (_agent.NavAgent != null && _agent.NavAgent.isOnNavMesh)
        {
            _agent.NavAgent.ResetPath();
            // VI: Ép vận tốc về 0 để dừng di chuyển theo path, KHÔNG DÙNG isStopped để NavAgent vẫn tính toán di chuyển trượt!
            // EN: Force velocity to 0 to stop pathing, DO NOT USE isStopped so NavAgent still calculates strafing!
            _agent.NavAgent.velocity = Vector3.zero;
            _agent.NavAgent.updateRotation = false;
        }

        _agent.Context.CurrentExecutingSkill = _currentSkill.data;
    }

    void Update()
    {
        if (CurrentSkillStatus != TaskStatus.Continue || _currentSkill == null || _agent.Context.Target == null) return;

        _timer += Time.deltaTime;

        // VI: Luôn luôn xoay dìm mặt về phía Player một cách đầm chắc
        // EN: Always ponderously rotate to face the Player
        _agent.FaceTargetLazy(_agent.Context.Target.position, _agent.TacticalSettings.combatTurnSpeed, 0f, 0f);

        // ==========================================
        // CỖ MÁY TRẠNG THÁI / STATE MACHINE
        // ==========================================

        // PHASE 0: CHỜ GÓC NGẮM (FOV CHECK)
        if (_phase == 0)
        {
            Vector3 dirToTarget = (_agent.Context.Target.position - transform.position).normalized;
            dirToTarget.y = 0;
            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

            if (angleToTarget <= _currentSkill.data.weaponFOV / 2f)
            {
                _phase = 1; _timer = 0f;
            }
            else if (_timer > 4f)
            {
                AbortExecution(TaskStatus.Failure);
            }
        }
        // PHASE 1: ĐỨNG YÊN NGẮM (WIND-UP)
        else if (_phase == 1)
        {
            if (_timer >= _currentSkill.data.windUpTime)
            {
                _phase = 2; _timer = 0f;
            }
        }
        // PHASE 2: XẢ ĐẠN & TRƯỢT NGANG (EXECUTE & STRAFE)
        else if (_phase == 2)
        {
            if (aiGunController != null)
            {
                Vector3 aimTarget = _agent.Context.Target.position + (Vector3.up * 1.2f);
                aiGunController.ShootAt(aimTarget);
            }

            // VI: Logic trượt ngang liên tục không dùng Tween
            // EN: Continuous strafing logic without using Tween
            if (_agent.NavAgent != null && _agent.NavAgent.isOnNavMesh)
            {
                Vector3 dirToTarget = (_agent.Context.Target.position - transform.position).normalized;
                Vector3 rightDir = Vector3.Cross(dirToTarget, Vector3.up) * _strafeDir;
                _agent.NavAgent.Move(rightDir * _agent.TacticalSettings.prowlSpeed * Time.deltaTime);
            }

            if (_timer >= _currentSkill.data.executeTime)
            {
                _phase = 3; _timer = 0f;
            }
        }
        // PHASE 3: THU SÚNG (RECOVERY)
        else if (_phase == 3)
        {
            if (_timer >= _currentSkill.data.recoveryTime)
            {
                _currentSkill.RecordCastTime();
                AbortExecution(TaskStatus.Success);
            }
        }
    }

    public void AbortExecution(TaskStatus finalStatus = TaskStatus.Failure)
    {
        CurrentSkillStatus = finalStatus;

        if (_agent != null && _agent.NavAgent != null && _agent.NavAgent.isOnNavMesh)
        {
            _agent.NavAgent.updateRotation = true;
        }

        _currentSkill = null;
        if (_agent != null && _agent.Context != null)
        {
            _agent.Context.CurrentExecutingSkill = null;
        }
    }

    public void AddSkill(AISkillData data) { if (!availableSkills.Exists(s => s.data == data)) availableSkills.Add(new ActiveSkill { data = data }); }
    public void RemoveSkill(AISkillData data) { availableSkills.RemoveAll(s => s.data == data); }
}