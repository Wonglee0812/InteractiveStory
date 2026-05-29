using UnityEngine;
using UnityEngine.AI;
using FluidHTN;

[RequireComponent(typeof(NavMeshAgent))]
public class AIAgent : MonoBehaviour
{
    public AiContext Context { get; private set; }
    public Planner<AiContext> Planner { get; private set; }
    public NavMeshAgent NavAgent { get; private set; }

    public Domain<AiContext> AIDomain;

    [Header("--- DESIGNER SETUP ---")]
    [Tooltip("Kéo thả File Thông số Chiến thuật của quái vào đây")]
    public AITacticalSettings TacticalSettings;

    public AIPatrolPath shortcutPath;
    public GroupAgent MyHerd;

    [Header("Performance")]
    public int FramesBetweenTicks = 3;
    private int frameCounter = 0;
    private bool _isLazyRotating = false; // Cờ theo dõi trạng thái xoay thân

    void Awake()
    {
        NavAgent = GetComponent<NavMeshAgent>();
        Context = new AiContext { Agent = this };
        Context.Init();
        Planner = new Planner<AiContext>();

        frameCounter = Random.Range(0, FramesBetweenTicks);
    }

    void Start()
    {
        // ÉP CHỈ SỐ VẬT LÝ VÀO NAVMESH (Chống lỗi lù đù, xe tăng)
        if (TacticalSettings != null && NavAgent != null)
        {
            NavAgent.acceleration = 40f; // Bứt tốc cực gắt
            NavAgent.angularSpeed = 500f; // Xoay người như điện
        }

        if (shortcutPath != null) Context.PatrolPath = shortcutPath;
        AIDomain = GreyboxDomain.Build();

        if (MyHerd != null) MyHerd.RegisterAgent(this);
        else
        {
            GroupAgent parentGroup = GetComponentInParent<GroupAgent>();
            if (parentGroup != null) parentGroup.RegisterAgent(this);
            else
            {
                GroupAgent globalGroup = FindFirstObjectByType<GroupAgent>();
                if (globalGroup != null) globalGroup.RegisterAgent(this);
            }
        }
    }
    void Update()
    {
        frameCounter++;
        if (frameCounter >= FramesBetweenTicks)
        {
            frameCounter = 0;
            if (AIDomain != null) Planner.Tick(AIDomain, Context);
        }

        UpdateLocomotion();
    }

    private void UpdateLocomotion()
    {
        // 1. KHI ĐANG DI CHUYỂN: Xoay mượt theo hướng đi (Giới hạn góc quay tối đa 120 độ/giây)
        if (NavAgent.hasPath && NavAgent.velocity.sqrMagnitude > 0.1f && NavAgent.updateRotation)
        {
            _isLazyRotating = false;
            Vector3 dir = NavAgent.desiredVelocity.normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 120f * Time.deltaTime);
            }
        }
        // 2. KHI ĐỨNG YÊN: Kích hoạt Lazy Rotation Toàn cục
        else if (NavAgent.updateRotation)
        {
            if (Context.HasState(AIWorldState.HasTarget) && Context.Target != null)
            {
                FaceTargetLazy(Context.Target.position, TacticalSettings != null ? TacticalSettings.combatTurnSpeed : 90f, 45f, 10f);
            }
            else if (Context.HasState(AIWorldState.HasSuspectedLocation))
            {
                FaceTargetLazy(Context.SuspectedLocation, TacticalSettings != null ? TacticalSettings.turnSpeed : 60f, 45f, 10f);
            }
        }
    }
    private void OnDestroy()
    {
        // Khi xác chết bị xóa khỏi map, báo cho Bầy đàn gạch tên mình ra
        if (MyHerd != null)
        {
            // Sếp nhớ check lại xem hàm xóa AI trong GroupAgent của sếp tên là gì nhé.
            // Có thể là UnregisterAgent(this) hoặc RemoveAgent(this)
            // MyHerd.UnregisterAgent(this); 
        }
    }

    // [HỆ THỐNG LAZY ROTATION ĐÃ NÂNG CẤP]: Bổ sung Start/Stop Angle
    public void FaceTargetLazy(Vector3 targetPos, float turnSpeed, float startAngle = 45f, float stopAngle = 10f)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        dir.y = 0;
        if (dir == Vector3.zero) return;

        float angle = Vector3.Angle(transform.forward, dir);

        // Kích hoạt khi lệch quá góc Start, tắt đi khi đã gom vào góc Stop
        if (angle >= startAngle) _isLazyRotating = true;
        if (angle <= stopAngle) _isLazyRotating = false;

        if (_isLazyRotating)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);

            // [FIX]: Dùng RotateTowards để xoay với tốc độ ổn định (Độ/Giây)
            // Ép tốc độ xoay lười biếng nằm trong khoảng 30 đến 120 độ/giây để trông nặng nề
            float actualTurnSpeed = Mathf.Clamp(turnSpeed, 30f, 120f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, actualTurnSpeed * Time.deltaTime);
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // =========================================================
        // [FIX LỖI ĐỎ CONSOLE]: Chặn đứng mọi hoạt động vẽ Gizmo nếu Quái đã chết (bị tắt script)
        // =========================================================
        if (!Application.isPlaying || Context == null || Planner == null || !this.enabled || !gameObject.activeInHierarchy) return;

        Vector3 headPos = transform.position + Vector3.up * 2.5f;

        // 1. Tự suy luận trạng thái thay vì móc nội bộ HTN
        string currentTask = "PATROL (Đi Tuần)";
        if (Context.HasState(AIWorldState.HasTarget)) currentTask = "COMBAT (Chiến đấu)";
        else if (Context.HasState(AIWorldState.IsSearching)) currentTask = "SEARCHING (Mất dấu - Tìm kiếm)";
        else if (Context.HasState(AIWorldState.HasSuspectedLocation)) currentTask = "INVESTIGATING (Điều tra / Cảnh giác)";

        // 2. Hiển thị Trạng thái Token đang nắm giữ
        string tokens = "";
        if (Context.HasState(AIWorldState.HasCombatToken)) tokens += "[COMBAT TOKEN] ";
        if (Context.HasState(AIWorldState.HasInvestigateToken)) tokens += "[INVESTIGATE TOKEN]";

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Context.HasState(AIWorldState.HasTarget) ? Color.red : Color.yellow;
        style.fontSize = 14;
        style.alignment = TextAnchor.MiddleCenter;

        // Bọc trong try-catch để phòng hờ 1% Unity Editor vẫn bị kẹt Layout Event
        try
        {
            UnityEditor.Handles.Label(headPos, $"{currentTask}\n{tokens}", style);
        }
        catch { }

        // 3. Vẽ đường đi NavMesh
        if (NavAgent != null && NavAgent.enabled && NavAgent.hasPath)
        {
            Gizmos.color = Color.cyan;
            var corners = NavAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
#endif
}