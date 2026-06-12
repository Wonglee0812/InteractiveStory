using KinematicCharacterController;
using Unity.Jobs;
using UnityEngine;
public enum Stance
{
    Stand,
    Crouch, // Trạng thái ngồi đi bộ lén lút mới
    Slide
}
public struct CharacterState
{
    public bool Grounded;
    public Stance Stance;
    public Vector3 Velocity;
}
[RequireComponent(typeof(KinematicCharacterMotor))]
public class PlayerCharater : MonoBehaviour, ICharacterController
{

    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform cameraTarget;

    private CharacterState _state;
    private CharacterState _lastState;
    private CharacterState _tempState;

    [Header("Movement Settings")]
    public float walkSpeed = 8f;
    public float sprintSpeed = 14f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float walkResponse = 20f;
    [Space]
    [Header("Crouch Settings (Thêm Mới)")]
    [SerializeField] private float crouchSpeed = 4f; // Tốc độ đi bộ khi ngồi
    [SerializeField] private float crouchResponse = 15f; // Độ mượt khi gia tốc ngồi
    [Space]
    [Header("Capsule Height Settings (Chuẩn Mẫu Video)")]
    [SerializeField] private float standHeight = 2f; // Chiều cao Collider đứng mặc định
    [SerializeField] private float crouchHeight = 1f; // Chiều cao Collider ngồi
    [Space]
    [Tooltip("Normalized height multiplier for Camera Target (0-1)")]
    [Range(0f, 1f)][SerializeField] private float standCameraTargetHeight = 0.85f; // Tỉ lệ cao độ cam lúc đứng (Ví dụ 0.85 <=> 1.7m)
    [Range(0f, 1f)][SerializeField] private float crouchCameraTargetHeight = 0.45f; // Tỉ lệ cao độ cam lúc ngồi
    [Header("Jump Settings")]
    [SerializeField] private float gravity = -90f;
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private float coyoteTime = 0.15f;   
    [Range(0f, 1f)]
    [SerializeField] private float jumpSustainGravity = 0.4f;
    [Space]
    [Header("Air Movement")]
    [SerializeField] private float airSpeed = 15f;
    [SerializeField] private float airAcceleration = 50f;
    [Space]
    [Header("Slide Settings")]
    [SerializeField] private float slideStartSpeed = 100f; 
    [SerializeField] private float slideFriction = 0.8f;
    [SerializeField] private float slideEndSpeed = 22f;
    [SerializeField] private float slideGravity = -90f;
    [SerializeField] private float slideSteerAcceleration = 5f;
    [SerializeField] private float slideCooldown = 1f; // [VI] Chờ 1 giây / [EN] 1 second wait
    [Space]
    [Header("Player State & Physics")]
    [SerializeField] private PlayerState playerState; // [VI] Kéo script PlayerState vào đây trên Inspector
    private Vector3 _externalForce; // [VI] Lực hất văng lưu trữ tạm thời
    [Space]
    [Header("Audio & Stealth Integration")]
    [SerializeField] private float walkSoundRadius = 6f;    // Bán kính tiếng ồn khi đi bộ
    [SerializeField] private float sprintSoundRadius = 15f; // Bán kính tiếng ồn khi chạy nhanh
    [SerializeField] private float soundEmitInterval = 0.4f; // Khoảng cách giữa các lần phát tiếng (Chống Lag/Spam)
    private float _timeSinceLastSound = 0f;
    [Space]
    [Header("Animation Settings")]
    [SerializeField] private Animator animator; // [VI] Kéo thả Model có chứa Animator vào đây / [EN] Drag and drop the Model containing the Animator here
    [Tooltip("VI: Thời gian trễ để làm mượt animation (giây). Càng cao bước chân càng có độ trễ.\nEN: Delay time to smooth animation (seconds). Higher means more footstep delay.")]
    [SerializeField] private float animSmoothTime = 0.1f;

    // [VI] Biến lưu trữ nội bộ cho thuật toán SmoothDamp / [EN] Internal variables for SmoothDamp algorithm
    private float _currentVelocityXRef;
    private float _currentVelocityZRef;
    private float _animVelocityX;
    private float _animVelocityZ;
    [Space]
    private float _lastSlideTime = -999f;
    private bool _jumpRequest;
    private bool _requireSlideRelease = false;
    private bool _slideRequestedInAir = false;
    private CharacterInputs currentInputs;
    private Collider[] _uncrouchOverlapResults;
    private float _timeSinceUngrounded;
    private float _timeSinceJumpRequest;
    private bool _unGroundedDueToJump;
    public Stance CurrentStance => _state.Stance;
    public bool isGround;

    public void ApplyExternalForce(Vector3 force)
    {
        _externalForce += force;
        motor.ForceUnground(); // [VI] Văng lên không trung / [EN] Launch into the air
    }
    public void Initialize()
    {
        motor.CharacterController = this;
        _state.Stance = Stance.Stand;
        _uncrouchOverlapResults = new Collider[8];
    }
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(Camera.main.transform.forward, motor.CharacterUp).normalized;

        if (currentInputs.AimHeld)
        {
            if (cameraPlanarDirection.sqrMagnitude > 0f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraPlanarDirection, motor.CharacterUp);
                currentRotation = Quaternion.Slerp(currentRotation, targetRotation, rotationSpeed * 2f * deltaTime);
            }
        }
        else if (currentInputs.MoveInput.sqrMagnitude > 0f)
        {
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, motor.CharacterUp);
            Vector3 moveDirection = cameraPlanarRotation * new Vector3(currentInputs.MoveInput.x, 0f, currentInputs.MoveInput.y);

            if (moveDirection.sqrMagnitude > 0f)
            {
                // ==========================================
                // VI: MÔ PHỎNG THEO LAST OF US (DYNAMIC ROTATION LAG)
                // EN: LAST OF US SIMULATION (DYNAMIC ROTATION LAG)
                // ==========================================
                float dynamicRotationSpeed = rotationSpeed; // Khởi tạo bằng tốc độ gốc / Init with base speed

                if (_state.Stance == Stance.Slide)
                {
                    // VI: Khi đang trượt, bóp chết tốc độ xoay để lực quán tính đẩy dọc trục Z.
                    // EN: While sliding, kill the rotation speed so inertia pushes along the Z-axis.
                    dynamicRotationSpeed = 0.5f; // Xoay cực chậm / Extremely slow rotation
                }
                else if (_state.Stance == Stance.Crouch)
                {
                    // VI: Khi lén lút, xoay cực kỳ chậm để bắt Animator dùng các bước đi ngang
                    // EN: When sneaking, rotate extremely slowly to force the Animator to use strafe steps
                    dynamicRotationSpeed = rotationSpeed / 2;
                }
                else if (!currentInputs.SprintToggle)
                {
                    // VI: Khi đi bộ bình thường, xoay vừa phải để tạo độ đầm (Quán tính)
                    // EN: When walking normally, rotate moderately to create weight (Inertia)
                    dynamicRotationSpeed = rotationSpeed / 2;
                }
                else
                {
                    // VI: Khi Sprint, dùng tốc độ gốc để xoay nhanh, bẻ lái gấp
                    // EN: When Sprinting, use base speed to rotate fast and corner sharply
                    dynamicRotationSpeed = rotationSpeed;
                }

                // VI: Áp dụng góc xoay với tốc độ linh hoạt
                // EN: Apply rotation with dynamic speed
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, motor.CharacterUp);
                currentRotation = Quaternion.Slerp(currentRotation, targetRotation, dynamicRotationSpeed * deltaTime);
            }
        }
    }
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(Camera.main.transform.forward, motor.CharacterUp).normalized;
        Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, motor.CharacterUp);
        Vector3 moveDirection = cameraPlanarRotation * Vector3.ClampMagnitude(new Vector3(currentInputs.MoveInput.x, 0f, currentInputs.MoveInput.y), 1f);
        //If on the ground
        if (motor.GroundingStatus.IsStableOnGround)
        {
            isGround = true;
            _timeSinceUngrounded = 0;
            _unGroundedDueToJump = false;

            // Ép hướng di chuyển bám sát theo bề mặt dốc
            moveDirection = motor.GetDirectionTangentToSurface(moveDirection, motor.GroundingStatus.GroundNormal) * moveDirection.magnitude;

            // Xả khóa khi nhả phím Crouch
            if (!currentInputs.SlideDown)
            {
                _requireSlideRelease = false;
                _slideRequestedInAir = false;
            }

            bool isMoving = moveDirection.sqrMagnitude > 0f;
            bool wasInAir = !_lastState.Grounded;

            // =========================================================
            // 1. LOGIC KÍCH HOẠT TRƯỢT (CHUẨN VIDEO)
            // =========================================================
            // =========================================================
            // 1. LOGIC KÍCH HOẠT TRƯỢT (ĐÃ FIX LỖI "LÚC ĂN LÚC KHÔNG")
            // =========================================================
            if (!_requireSlideRelease && currentInputs.SlideDown && isMoving)
            {
                bool canStartSlide = false;

                // [FIX LỖI BẤM KHÔNG ĂN]: Thay vì dùng SlidePressedThisFrame dễ bị lọt lưới,
                // Ta bắt trực tiếp khoảnh khắc vật lý: Vừa chuyển từ trạng thái Stand sang Crouch
                bool startedCrouchingThisFrame = _state.Stance == Stance.Crouch && _lastState.Stance != Stance.Crouch;

                // [FIX LỖI TRÊN KHÔNG]: Rơi chạm đất trong lúc đang giữ phím lết
                bool landedCrouching = wasInAir && _state.Stance == Stance.Crouch;

                // Cú trigger 1: Vừa ngồi xuống + Đang bật Sprint
                if (startedCrouchingThisFrame && currentInputs.SprintToggle) canStartSlide = true;

                // Cú trigger 2: Rơi xuống đất + Đang bật Sprint
                if (landedCrouching && currentInputs.SprintToggle) canStartSlide = true;

                if (canStartSlide && Time.time >= (_lastSlideTime + slideCooldown))
                {
                    // TRẢ LẠI LOGIC TRỪ STAMINA THEO Ý SẾP
                    if (playerState != null && !playerState.TryConsumeStaminaForSlide())
                    {
                        // Hết Stamina -> Bị khóa, ép nhả nút C ra mới được thao tác tiếp
                        _requireSlideRelease = true;
                    }
                    else
                    {
                        _lastSlideTime = Time.time;
                        _state.Stance = Stance.Slide;

                        // BẢO TOÀN ĐỘNG NĂNG RƠI
                        if (wasInAir)
                        {
                            currentVelocity = Vector3.ProjectOnPlane(_lastState.Velocity, motor.GroundingStatus.GroundNormal);
                        }

                        // Fix lỗi trượt bậy ở mép vực
                        float effectiveStartSpeed = slideStartSpeed;
                        if (wasInAir && !_slideRequestedInAir) effectiveStartSpeed = 0f;

                        float slideSpeed = Mathf.Max(effectiveStartSpeed, currentVelocity.magnitude);
                        Vector3 slideDir = currentVelocity.sqrMagnitude > 0.1f ? currentVelocity.normalized : moveDirection;
                        currentVelocity = motor.GetDirectionTangentToSurface(slideDir, motor.GroundingStatus.GroundNormal) * slideSpeed;

                        _slideRequestedInAir = false; // Xả cờ
                    }
                }
            }
            if (_state.Stance == Stance.Slide && !currentInputs.SlideDown)
            {
                // Ép trạng thái về Crouch để bàn giao cho khối vật lý bên dưới.
                _state.Stance = Stance.Crouch;

                // Sau đó, hàm AfterCharacterUpdate() phía dưới sẽ tự động quét trần nhà,
                // nếu đầu không vướng vật cản sẽ tự động trả về Stance.Stand để đứng thẳng dậy ngay lập tức!
            }
            // =========================================================
            // 2. VẬT LÝ DI CHUYỂN THEO STANCE
            // =========================================================
            if (_state.Stance == Stance.Stand || _state.Stance == Stance.Crouch)
            {
                // XÁC ĐỊNH TỐC ĐỘ: Ngồi -> Chậm | Chạy -> Nhanh | Đi bộ -> Vừa
                float baseSpeed = walkSpeed;
                if (_state.Stance == Stance.Crouch) baseSpeed = crouchSpeed;
                else if (currentInputs.SprintToggle) baseSpeed = sprintSpeed;

                float currentWalkSpeed = currentInputs.AimHeld ? baseSpeed * 0.4f : baseSpeed;

                if (playerState != null) currentWalkSpeed *= playerState.CurrentSpeedMultiplier;

                Vector3 targetVelocity = moveDirection * currentWalkSpeed;
                float currentResponse = (_state.Stance == Stance.Crouch) ? crouchResponse : walkResponse;

                currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-currentResponse * deltaTime));
            }
            else if (_state.Stance == Stance.Slide)
            {
                // A. Ma sát (Friction)
                currentVelocity -= currentVelocity * (slideFriction * deltaTime);

                // B. Bẻ lái (Steering) - KHÔNG LÀM TĂNG TỐC ĐỘ (Phút 36:30)
                if (moveDirection.sqrMagnitude > 0f)
                {
                    float currentSpeed = currentVelocity.magnitude;
                    Vector3 targetVelocity = moveDirection * currentSpeed;
                    Vector3 steerForce = (targetVelocity - currentVelocity) * slideSteerAcceleration * deltaTime;

                    currentVelocity += steerForce;
                    currentVelocity = Vector3.ClampMagnitude(currentVelocity, currentSpeed); // Giới hạn tốc độ
                }

                // C. Tăng tốc khi xuống dốc (Slope Acceleration - Phút 37:25)
                Vector3 slopeForce = Vector3.ProjectOnPlane(-motor.CharacterUp, motor.GroundingStatus.GroundNormal) * slideGravity;
                currentVelocity -= slopeForce * deltaTime;

                // D. Kết thúc trượt
                if (currentVelocity.magnitude < slideEndSpeed)
                {
                    // [VI] SỬA LỖI CHÍ MẠNG: Đổi về Crouch chứ KHÔNG PHẢI Stand
                    _state.Stance = Stance.Crouch;
                    _requireSlideRelease = true; // Khóa lại, bắt người chơi nhả phím C ra mới được thao tác tiếp
                }
            }
        }
        else 
        {
            isGround = false;
            _timeSinceUngrounded += deltaTime;
            // 1. TRỌNG LỰC & JUMP SUSTAIN (GIỮ NGUYÊN / KEEP THIS SAME)
            float effectiveGravity = gravity;
            float verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            if (currentInputs.JumpHeld && verticalSpeed > 0f)
            {
                effectiveGravity *= jumpSustainGravity;
            }
            currentVelocity += motor.CharacterUp * effectiveGravity * deltaTime;

            // 2. AIR STRAFING (CẬP NHẬT THEO VIDEO / UPDATED TO MATCH VIDEO)
            if (currentInputs.MoveInput.sqrMagnitude > 0f)
            {
                // [VI] Vector lực nhập vào / [EN] Input force vector
                Vector3 planarMovement = Vector3.ProjectOnPlane(moveDirection, motor.CharacterUp).normalized * currentInputs.MoveInput.magnitude;
                // [VI] Vận tốc ngang hiện tại / [EN] Current planar velocity
                Vector3 currentPlanarVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);

                // [VI] Lực đẩy tổng / [EN] Total pushing force
                Vector3 movementForce = planarMovement * airAcceleration * deltaTime;

                // [VI] LUỒNG 1: Nếu bay chậm hơn airSpeed -> Cho phép tăng tốc tự do rồi gọt (Clamp)
                // [EN] PATH 1: If flying slower than airSpeed -> Allow free acceleration then Clamp
                if (currentPlanarVelocity.magnitude < airSpeed)
                {
                    Vector3 targetPlanarVelocity = currentPlanarVelocity + movementForce;
                    targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);
                    movementForce = targetPlanarVelocity - currentPlanarVelocity; // Lấy lực chênh lệch thực tế / Get actual delta force
                }
                // [VI] LUỒNG 2: Đang bay quá nhanh (Vượt airSpeed do Slide/Dash) VÀ cố tình bơm ga cùng hướng
                // [EN] PATH 2: Flying too fast (Exceeding airSpeed via Slide/Dash) AND intentionally gassing in the same direction
                else if (Vector3.Dot(currentPlanarVelocity, movementForce) > 0f)
                {
                    // [VI] Chặn lực đâm tới, chỉ cho lực đi ngang (Strafe)
                    // [EN] Block forward force, only allow lateral force (Strafe)
                    Vector3 constrainedMovementForce = Vector3.ProjectOnPlane(movementForce, currentPlanarVelocity.normalized);
                    movementForce = constrainedMovementForce;
                }

                //Anti air climbing
                if (motor.GroundingStatus.FoundAnyGround)
                {
                    // [VI] Nếu lực đẩy (W) đang cùng hướng với Vận tốc tổng
                    // [EN] If the pushing force (W) is in the same direction as the total Velocity
                    if (Vector3.Dot(movementForce, currentVelocity + movementForce) > 0f)
                    {
                        // [VI] Tính toán vách tường thẳng đứng ảo
                        // [EN] Calculate the virtual vertical wall
                        Vector3 obstructionNormal = Vector3.Cross(
                            motor.CharacterUp,
                            Vector3.Cross(motor.CharacterUp, motor.GroundingStatus.GroundNormal)
                        ).normalized;

                        // [VI] Ép lực đẩy vào vách ảo này để triệt tiêu lực leo lên
                        // [EN] Project pushing force onto this virtual wall to nullify climbing force
                        movementForce = Vector3.ProjectOnPlane(movementForce, obstructionNormal);
                    }
                }
                // [VI] Cuối cùng, cộng lực lượng đã được xử lý vào Vận tốc
                // [EN] Finally, add the processed force to the Velocity
                currentVelocity += movementForce;
            }
        }
        if (_externalForce != Vector3.zero)
        {
            currentVelocity += _externalForce;
            _externalForce = Vector3.zero; // [VI] Xả xong thì reset / [EN] Reset after discharging
        }
        if (_jumpRequest)
        {
            _jumpRequest = false;
            var canCoyateJump = _timeSinceUngrounded < coyoteTime && !_unGroundedDueToJump;
            if (motor.GroundingStatus.IsStableOnGround || canCoyateJump)
            {
                _unGroundedDueToJump = true;
                motor.ForceUnground(time: 0);
                float currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                float targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpPower);
                currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);

            }
        }
        else
        {
            _timeSinceJumpRequest += deltaTime;

            var canJumpLater = _timeSinceJumpRequest < coyoteTime;
            _jumpRequest = canJumpLater;
        }    
    }


    public void BeforeCharacterUpdate(float deltaTime)
    {
        _tempState = _state;

        // Nếu người chơi đang đè phím Crouch hoặc đang trạng thái Trượt -> Ép lùn Collider vật lý ngay lập tức
        if (currentInputs.SlideDown || _state.Stance == Stance.Slide)
        {
            if (_state.Stance == Stance.Stand)
            {
                _state.Stance = Stance.Crouch;
            }
            motor.SetCapsuleDimensions(motor.Capsule.radius, crouchHeight, crouchHeight * 0.5f);
        }
    }
    public void PostGroundingUpdate(float deltaTime)
    {       
    }
    public void AfterCharacterUpdate(float deltaTime)
    {
        _lastState = _tempState;

        // KIỂM TRA ĐIỀU KIỆN ĐỨNG DẬY: Nếu thả nút Crouch nhưng cơ thể đang lùn
        if (!currentInputs.SlideDown && _state.Stance == Stance.Crouch)
        {
            // Giả lập thử nâng Collider lên chiều cao đứng để quét va chạm
            motor.SetCapsuleDimensions(motor.Capsule.radius, standHeight, standHeight * 0.5f);

            // Quét xem gầm trần nhà có vật thể nào đè lên không
            int overlapCount = motor.CharacterOverlap(
                motor.TransientPosition,
                motor.TransientRotation,
                _uncrouchOverlapResults,
                motor.CollidableLayers,
                QueryTriggerInteraction.Ignore
            );

            if (overlapCount > 0)
            {
                // BỊ KẸT TRẦN! Ép Motor thu nhỏ trở lại, không cho phép đứng lên
                motor.SetCapsuleDimensions(motor.Capsule.radius, crouchHeight, crouchHeight * 0.5f);
                _state.Stance = Stance.Crouch;
            }
            else
            {
                // THOÁNG ĐẦU! Cho phép trả về trạng thái Đứng bình thường
                _state.Stance = Stance.Stand;
            }
        }
        else if (!currentInputs.SlideDown && _state.Stance == Stance.Stand)
        {
            // Đảm bảo Collider luôn ở trạng thái đứng nếu không bấm gì
            motor.SetCapsuleDimensions(motor.Capsule.radius, standHeight, standHeight * 0.5f);
        }

        _state.Grounded = motor.GroundingStatus.IsStableOnGround;
        _state.Velocity = motor.Velocity;

        if (_state.Grounded && _state.Velocity.sqrMagnitude > 1f && _state.Stance != Stance.Crouch)
        {
            _timeSinceLastSound += deltaTime;

            // Bộ lọc chống Spam (Cứ mỗi 0.4s mới phát tiếng 1 lần)
            if (_timeSinceLastSound >= soundEmitInterval)
            {
                _timeSinceLastSound = 0f;

                // Nếu đang giữ nút Chạy (Sprint) -> Tiếng ồn to hơn
                float currentRadius = currentInputs.SprintToggle ? sprintSoundRadius : walkSoundRadius;

                // Phát tín hiệu cho toàn bộ AI trên bản đồ
                AIAuditorySystem.ReportSound(transform.position, currentRadius);
            }
        }
        else
        {
            // Nếu đứng im hoặc ngồi, nạp đầy bình Timer 
            // -> Lần tới đứng lên đi bộ sẽ phát ra tiếng động ngay lập tức (Responsive)
            if (_state.Velocity.sqrMagnitude <= 1f)
            {
                _timeSinceLastSound = soundEmitInterval;
            }
        }
        UpdateAnimation(deltaTime);
    }


    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }
    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }
    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    private void UpdateAnimation(float deltaTime)
    {
        if (animator == null) return;

        Vector3 localVelocity = transform.InverseTransformDirection(motor.Velocity);
        float targetX = (localVelocity.x / sprintSpeed) * 2f;
        float targetZ = (localVelocity.z / sprintSpeed) * 2f;

        _animVelocityX = Mathf.SmoothDamp(_animVelocityX, targetX, ref _currentVelocityXRef, animSmoothTime);
        _animVelocityZ = Mathf.SmoothDamp(_animVelocityZ, targetZ, ref _currentVelocityZRef, animSmoothTime);

        animator.SetFloat("VelocityX", _animVelocityX);
        animator.SetFloat("VelocityZ", _animVelocityZ);

        // 1. TRẠNG THÁI (STANCE)
        animator.SetBool("isCrouch", _state.Stance == Stance.Crouch);
        animator.SetBool("isSlide", _state.Stance == Stance.Slide);

        // 2. CHẠM ĐẤT & TRÊN KHÔNG (GROUNDING & AIRBORNE)
        animator.SetBool("isGrounded", _state.Grounded);

        // Tự động suy luận Nhảy/Rơi dựa trên Vận tốc trục Y (Gravity)
        bool isFalling = !_state.Grounded && (motor.Velocity.y < 0f || motor.Velocity.y < -2);
        bool isJumping = !_state.Grounded && motor.Velocity.y > 0f;
        
        animator.SetBool("isFalling", isFalling);
        animator.SetBool("isJump", isJumping);

        // (Tùy chọn) Có đang di chuyển không
        animator.SetBool("IsMoving", motor.Velocity.sqrMagnitude > 0.1f);
    }

    public void UpdateBody(float deltaTime)
    {
        // 1. Tính toán tỉ lệ chiều cao hiện tại của Collider so với chiều cao đứng mặc định
        float currentHeight = motor.Capsule.height;
        float normalizedHeight = currentHeight / standHeight;

        // 2. Tính toán cao độ mục tiêu cho Camera Target dựa trên Stance hiện tại
        float targetCameraFactor = (_state.Stance == Stance.Stand) ? standCameraTargetHeight : crouchCameraTargetHeight;
        float cameraTargetHeight = currentHeight * targetCameraFactor;

        // 3. Nội suy mượt mà vị trí Camera Target theo trục Y cục bộ để tránh giật hình
        //if (cameraTarget != null)
        //{
        //    Vector3 targetCamPos = new Vector3(0f, cameraTargetHeight, 0f);
        //    cameraTarget.localPosition = Vector3.Lerp(cameraTarget.localPosition, targetCamPos, 1f - Mathf.Exp(-crouchHeightResponse * deltaTime));
        //}
    }
    public void UpdateInput(ref CharacterInputs inputs)
    {
        currentInputs = inputs;
        bool wasRequestingJump = _jumpRequest;
        _jumpRequest = _jumpRequest || currentInputs.JumpDown;
        if (_jumpRequest && !wasRequestingJump)
        {
            _timeSinceJumpRequest = 0f;
        }

        if (currentInputs.SlidePressedThisFrame && !motor.GroundingStatus.IsStableOnGround)
        {
            _slideRequestedInAir = true;
        }
    }
    public Transform GetCameraTarget() => cameraTarget;
}
