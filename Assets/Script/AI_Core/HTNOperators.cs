using UnityEngine;
using UnityEngine.AI;
using FluidHTN;
using FluidHTN.Operators;
using System;

// ==========================================================
// 1. CHỌN ĐIỂM TUẦN TRA 
// ==========================================================
public class GetNextWaypointOperator : IOperator
{
    public TaskStatus Start(IContext ctx)
    {
        AiContext context = (AiContext)ctx;
        if (context.PatrolPath == null || context.PatrolPath.WaypointCount == 0) return TaskStatus.Failure;

        context.CurrentMoveDestination = context.PatrolPath.GetWaypointPosition(context.CurrentWaypointIndex);
        context.CurrentWaypointIndex = (context.CurrentWaypointIndex + 1) % context.PatrolPath.WaypointCount;

        return TaskStatus.Success;
    }

    public TaskStatus Update(IContext ctx) => TaskStatus.Success;
    public void Stop(IContext ctx) { }
    public void Abort(IContext ctx) { }
}

// ==========================================================
// 2. XOAY THÂN NGƯỜI (CÓ CẦU CHÌ BẢO VỆ CỔ)
// ==========================================================
public class TurnToFaceOperator : IOperator
{
    private bool _isInvestigateMode;
    private Func<AiContext, float> _getTurnSpeed;

    public TurnToFaceOperator(bool isInvestigateMode, Func<AiContext, float> getTurnSpeed)
    {
        _isInvestigateMode = isInvestigateMode;
        _getTurnSpeed = getTurnSpeed;
    }

    public TaskStatus Start(IContext ctx)
    {
        AiContext context = (AiContext)ctx;
        if (context.Agent?.NavAgent != null && context.Agent.NavAgent.isActiveAndEnabled && context.Agent.NavAgent.isOnNavMesh)
        {
            context.Agent.NavAgent.ResetPath();
            context.Agent.NavAgent.updateRotation = false;
        }
        return TaskStatus.Continue;
    }

    public TaskStatus Update(IContext ctx)
    {
        AiContext context = (AiContext)ctx;
        Vector3 targetPos = context.CurrentMoveDestination;

        if (_isInvestigateMode)
        {
            targetPos = (context.HasState(AIWorldState.HasTarget) && context.Target != null)
                ? context.Target.position : context.SuspectedLocation;
        }

        if (Vector3.Distance(context.Agent.transform.position, targetPos) > 1.5f)
            context.LookFocusOverride = targetPos + Vector3.up * 1.2f;
        else
            context.LookFocusOverride = null;

        Vector3 dir = (targetPos - context.Agent.transform.position).normalized;
        dir.y = 0;
        if (dir == Vector3.zero)
        {
            context.LookFocusOverride = null;
            return TaskStatus.Success;
        }

        Quaternion lookRot = Quaternion.LookRotation(dir);

        // VI: Ép tốc độ xoay (nhân hệ số 20 để chuyển đổi logic Speed cũ sang Độ/Giây)
        // EN: Force rotation speed (multiply by 20 to convert old Speed logic to Degrees/Second)
        float turnSpeedInDegrees = _getTurnSpeed(context) * 20f;
        context.Agent.transform.rotation = Quaternion.RotateTowards(context.Agent.transform.rotation, lookRot, turnSpeedInDegrees * Time.deltaTime);

        if (Quaternion.Angle(context.Agent.transform.rotation, lookRot) <= 5f)
        {
            context.LookFocusOverride = null;
            return TaskStatus.Success;
        }
        return TaskStatus.Continue;
    }

    public void Stop(IContext ctx)
    {
        AiContext context = (AiContext)ctx;
        context.LookFocusOverride = null;
        if (context.Agent?.NavAgent != null) context.Agent.NavAgent.updateRotation = true;
    }
    public void Abort(IContext ctx) => Stop(ctx);
}

// ==========================================================
// 3. DI CHUYỂN CÓ MỤC ĐÍCH
// ==========================================================
public class MoveToVectorDestinationOperator : IOperator
{
    private bool _isInvestigateMode;
    private Func<AiContext, float> _getStoppingDistance;
    private bool _isDestinationSet = false;

    public MoveToVectorDestinationOperator(bool isInvestigateMode, Func<AiContext, float> getStoppingDistance)
    {
        _isInvestigateMode = isInvestigateMode;
        _getStoppingDistance = getStoppingDistance;
    }

    public TaskStatus Start(IContext ctx)
    {
        _isDestinationSet = false;
        AiContext context = (AiContext)ctx;
        if (context.Agent?.NavAgent != null) context.Agent.NavAgent.updateRotation = true;
        return TaskStatus.Continue;
    }

    public TaskStatus Update(IContext ctx)
    {
        AiContext context = (AiContext)ctx;
        if (context.Agent == null || context.Agent.NavAgent == null) return TaskStatus.Failure;

        if (!_isDestinationSet)
        {
            Vector3 targetTarget = _isInvestigateMode ?
                ((context.HasState(AIWorldState.HasTarget) && context.Target != null) ? context.Target.position : context.SuspectedLocation)
                : context.CurrentMoveDestination;
            context.Agent.NavAgent.stoppingDistance = _getStoppingDistance(context);
            context.Agent.NavAgent.SetDestination(targetTarget);
            _isDestinationSet = true;
        }

        if (!context.Agent.NavAgent.pathPending && context.Agent.NavAgent.remainingDistance <= context.Agent.NavAgent.stoppingDistance)
        {
            _isDestinationSet = false;
            return TaskStatus.Success;
        }
        return TaskStatus.Continue;
    }

    public void Stop(IContext ctx)
    {
        AiContext context = (AiContext)ctx;
        if (context.Agent?.NavAgent != null && context.Agent.NavAgent.isActiveAndEnabled && context.Agent.NavAgent.isOnNavMesh)
        {
            context.Agent.NavAgent.ResetPath();
        }
        _isDestinationSet = false;
    }
    public void Abort(IContext ctx) => Stop(ctx);
}

// ==========================================================
// 4. CHỜ ĐỢI
// ==========================================================
public class WaitOperator : IOperator
{
    private Func<AiContext, float> _getWaitTime;
    private float _timer;
    public WaitOperator(Func<AiContext, float> getWaitTime) { _getWaitTime = getWaitTime; }
    public TaskStatus Start(IContext ctx) { _timer = 0f; return TaskStatus.Continue; }
    public TaskStatus Update(IContext ctx)
    {
        _timer += Time.deltaTime;
        return _timer >= _getWaitTime((AiContext)ctx) ? TaskStatus.Success : TaskStatus.Continue;
    }
    public void Stop(IContext ctx) { }
    public void Abort(IContext ctx) => Stop(ctx);
}

// ==========================================================
// 5. TELEGRAPH (ĐỨNG NHÌN)
// ==========================================================
public class TelegraphOperator : IOperator
{
    private bool _isInvestigateMode;
    private Func<AiContext, float> _getTelegraphTime;
    private float _timer;

    public TelegraphOperator(bool isInvestigateMode, Func<AiContext, float> getTelegraphTime)
    {
        _isInvestigateMode = isInvestigateMode;
        _getTelegraphTime = getTelegraphTime;
    }

    public TaskStatus Start(IContext ctx)
    {
        AiContext context = (AiContext)ctx;
        _timer = 0f;

        if (context.Agent?.NavAgent != null && context.Agent.NavAgent.isActiveAndEnabled && context.Agent.NavAgent.isOnNavMesh)
        {
            context.Agent.NavAgent.ResetPath();
        }

        Vector3 targetPos = _isInvestigateMode ?
            ((context.HasState(AIWorldState.HasTarget) && context.Target != null) ? context.Target.position : context.SuspectedLocation)
            : context.CurrentMoveDestination;

        // Cầu chì an toàn
        if (Vector3.Distance(context.Agent.transform.position, targetPos) > 1.5f)
            context.LookFocusOverride = targetPos + Vector3.up * 1.2f;
        else
            context.LookFocusOverride = null;

        return TaskStatus.Continue;
    }

    public TaskStatus Update(IContext ctx)
    {
        _timer += Time.deltaTime;
        if (_timer >= _getTelegraphTime((AiContext)ctx))
        {
            ((AiContext)ctx).LookFocusOverride = null;
            return TaskStatus.Success;
        }
        return TaskStatus.Continue;
    }

    public void Stop(IContext ctx) { ((AiContext)ctx).LookFocusOverride = null; }
    public void Abort(IContext ctx) => Stop(ctx);
}

// ==========================================================
// 6. RÀ QUÉT KHU VỰC (SẠCH SẼ, KHÔNG HACK TỌA ĐỘ)
// ==========================================================
public class ScanAreaOperator : IOperator
{
    private System.Func<AiContext, float> _getDuration;
    private float _timer;

    public ScanAreaOperator(System.Func<AiContext, float> getDuration)
    {
        _getDuration = getDuration;
    }

    public TaskStatus Start(IContext ctx)
    {
        var context = (AiContext)ctx;
        _timer = 0f;

        if (context.Agent.NavAgent != null && context.Agent.NavAgent.isOnNavMesh)
        {
            context.Agent.NavAgent.ResetPath();
            context.Agent.NavAgent.isStopped = true;
        }

        // CHỈ set IsScanning = true. AILookController sẽ tự động đảo mắt radar!
        context.IsScanning = true;

        return TaskStatus.Continue;
    }

    public TaskStatus Update(IContext ctx)
    {
        _timer += Time.deltaTime;

        if (_timer >= _getDuration((AiContext)ctx))
        {
            CleanUp((AiContext)ctx);
            return TaskStatus.Success;
        }
        return TaskStatus.Continue;
    }

    public void Stop(IContext ctx) => CleanUp((AiContext)ctx);
    public void Abort(IContext ctx) => CleanUp((AiContext)ctx);

    private void CleanUp(AiContext context)
    {
        context.IsScanning = false;

        if (context.Agent.NavAgent != null && context.Agent.NavAgent.isOnNavMesh)
        {
            context.Agent.NavAgent.isStopped = false;
        }
    }
}

// ==========================================================
// 7. [COMBAT] VÂY RÁP MỤC TIÊU 
// ==========================================================
public class CircleTargetOperator : IOperator
{
    private Func<AiContext, float> _getRadius;
    private bool _isMoving;
    private float _waitTimer;

    public CircleTargetOperator(Func<AiContext, float> getRadius) { _getRadius = getRadius; }

    public TaskStatus Start(IContext ctx)
    {
        AiContext context = (AiContext)ctx;
        if (context.Target == null) return TaskStatus.Failure;

        if (context.Agent?.NavAgent != null && context.Agent.NavAgent.isOnNavMesh)
        {
            context.Agent.NavAgent.isStopped = false;
            context.Agent.NavAgent.stoppingDistance = 0f;
            context.Agent.NavAgent.speed = context.Agent.TacticalSettings.prowlSpeed;
        }

        FindNewCirclePoint(context);
        return TaskStatus.Continue;
    }

    public TaskStatus Update(IContext ctx)
    {
        AiContext context = (AiContext)ctx;
        if (context.Target == null || context.Agent?.NavAgent == null) return TaskStatus.Failure;

        context.LookFocusOverride = context.Target.position + Vector3.up * 1.2f;

        if (_isMoving)
        {
            context.Agent.NavAgent.updateRotation = true;
            if (!context.Agent.NavAgent.pathPending && context.Agent.NavAgent.remainingDistance <= 1f)
            {
                _isMoving = false;
                _waitTimer = UnityEngine.Random.Range(0.2f, 0.6f);

                if (context.Agent.NavAgent.isActiveAndEnabled && context.Agent.NavAgent.isOnNavMesh)
                {
                    context.Agent.NavAgent.ResetPath();
                }
            }
        }
        // ...
        else
        {
            context.Agent.NavAgent.updateRotation = false;
            Vector3 dir = (context.Target.position - context.Agent.transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir);
                context.Agent.transform.rotation = Quaternion.Slerp(context.Agent.transform.rotation, lookRot, Time.deltaTime * context.Agent.TacticalSettings.combatTurnSpeed);
            }

            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f) FindNewCirclePoint(context);
        }
        return TaskStatus.Continue;
    }

    private void FindNewCirclePoint(AiContext context)
    {
        Vector3 dirFromPlayer = (context.Agent.transform.position - context.Target.position).normalized;
        float randomAngle = UnityEngine.Random.Range(45f, 90f) * (UnityEngine.Random.value > 0.5f ? 1f : -1f);
        Vector3 rotatedDir = Quaternion.Euler(0, randomAngle, 0) * dirFromPlayer;
        Vector3 targetPoint = context.Target.position + (rotatedDir * _getRadius(context));

        if (NavMesh.SamplePosition(targetPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            context.Agent.NavAgent.isStopped = false;
            context.Agent.NavAgent.SetDestination(hit.position);
            _isMoving = true;
        }
        else
        {
            _isMoving = false;
            _waitTimer = 1f;
        }
    }

    public void Stop(IContext ctx)
    {
        AiContext context = (AiContext)ctx;
        context.LookFocusOverride = null;
        if (context.Agent?.NavAgent != null && context.Agent.NavAgent.isOnNavMesh)
        {
            context.Agent.NavAgent.ResetPath();
            context.Agent.NavAgent.updateRotation = true;
        }
    }
    public void Abort(IContext ctx) => Stop(ctx);
}

// ==========================================================
// 8. [COMBAT] KHUNG SƯỜN TUNG ĐÒN UTILITY AI (GIAO TIẾP VỚI CONTROLLER)
// ==========================================================
public class UtilityCombatOperator : IOperator
{
    private AISkillController _skillController;

    public TaskStatus Start(IContext ctx)
    {
        AiContext context = (AiContext)ctx;
        if (context.Target == null) return TaskStatus.Failure;

        _skillController = context.Agent.GetComponent<AISkillController>();
        if (_skillController == null) return TaskStatus.Failure;

        // Ra lệnh cho Controller thực thi chiêu tốt nhất
        _skillController.StartBestSkill(context.Target);

        // Đọc trạng thái (Nếu StartBestSkill trả về Failure ngay lập tức vì kẹt FOV/hết đạn)
        return _skillController.CurrentSkillStatus;
    }

    public TaskStatus Update(IContext ctx)
    {
        // HTN chỉ ngồi chơi xơi nước và lắng nghe Controller báo cáo
        return _skillController.CurrentSkillStatus;
    }

    public void Stop(IContext ctx)
    {
        // Bị ngắt ngang (Do Stagger) -> Ép Controller dọn dẹp
        if (_skillController != null) _skillController.AbortExecution(TaskStatus.Failure);
    }

    public void Abort(IContext ctx) => Stop(ctx);
}