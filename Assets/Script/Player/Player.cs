using KinematicCharacterController.Examples;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
public struct CharacterInputs
{
    public Vector2 MoveInput;
    public Vector2 LookInput;
    public bool JumpDown;
    public bool JumpHeld;
    public bool SlideDown;
    public bool SlidePressedThisFrame;
    public bool AimHeld;
    public bool FirePressedThisFrame;
    public bool InventoryOpen;
    public bool SprintToggle;
}
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharater playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    // [THÊM MỚI] Gắn GunController và WeaponInventory vào đây trên Inspector
    [SerializeField] private GunController gunController;
    [SerializeField] private WeaponInventory weaponInventory;
    private PlayerInput playerInput;
    private CharacterInputs inputs;
    public bool isInputLocked = false;
    [SerializeField] private LayerMask aimColliderMask = ~0; // [VI] Chạm mọi thứ / [EN] Hit everything
    void Start()
    {
        playerCharacter.Initialize();
        playerCamera.Initialize(playerCharacter.GetCameraTarget());
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        // 1. [VI] Gom Input từ Unity Input System / [EN] Gather Input from Unity Input System
        inputs.MoveInput = playerInput.actions["Move"].ReadValue<Vector2>();
        inputs.LookInput = playerInput.actions["Look"].ReadValue<Vector2>();
        inputs.JumpDown = playerInput.actions["Jump"].WasPressedThisFrame();
        inputs.JumpHeld = playerInput.actions["Jump"].IsPressed();
        inputs.SlideDown = playerInput.actions["Crouch"].IsPressed();
        inputs.SlidePressedThisFrame = playerInput.actions["Crouch"].WasPressedThisFrame();

        // 2. [VI] Đọc Input súng đạn / [EN] Read Gun Input
        bool aimInput = playerInput.actions["Aim"].IsPressed();
        bool attackDown = playerInput.actions["Attack"].WasPressedThisFrame();
        bool attackHeld = playerInput.actions["Attack"].IsPressed();

        // [VI] Ép ngắm (Style TLOU): Bấm bắn cũng sẽ tự động đưa súng lên ngắm
        // [EN] Force aim (TLOU Style): Pressing fire also automatically raises the gun to aim
        inputs.AimHeld = aimInput || attackHeld;
        inputs.FirePressedThisFrame = attackDown;

        if (isInputLocked) { inputs = new CharacterInputs(); }

        // ==========================================
        // 3. [VI] GIAO PHÓ QUYỀN BẮN CHO GUN CONTROLLER
        //    [EN] DELEGATE FIRING AUTHORITY TO GUN CONTROLLER
        // ==========================================
        if (inputs.AimHeld && gunController != null && !isInputLocked)
        {
            // [VI] Chỉ truyền trạng thái nút bấm vào, Controller sẽ tự lo liệu (Auto/Semi, Cooldown...)
            // [EN] Only pass the button states in, the Controller will handle it (Auto/Semi, Cooldown...)
            gunController.HandleFiring(attackDown, attackHeld);
        }

        // 4. [VI] Cập nhật vật lý và trạng thái nhân vật / [EN] Update physics and character state
        if (playerCharacter != null)
        {
            playerCharacter.UpdateBody(Time.unscaledDeltaTime);
            playerCharacter.UpdateInput(ref inputs);
        }

        if (playerInput.actions["Sprint"].WasPressedThisFrame())
        {
            inputs.SprintToggle = !inputs.SprintToggle;
        }

        if (inputs.MoveInput.sqrMagnitude == 0)
        {
            inputs.SprintToggle = false;
        }
    }
    private void LateUpdate()
    {
        transform.position = playerCharacter.transform.position;
        if (playerCamera != null)
        {
            playerCamera.UpdateCameraLogic(ref inputs, Time.unscaledDeltaTime);
            playerCamera.UpdateCrosshair(inputs.AimHeld);
            playerCamera.UpdateCameraPolish(playerCharacter.CurrentStance, inputs.MoveInput.x, inputs.AimHeld, Time.unscaledDeltaTime);
        }
    }

    public void SetInputLock(bool state)
    {
        isInputLocked = state;
        if (isInputLocked)
        {
        }
    }

}
