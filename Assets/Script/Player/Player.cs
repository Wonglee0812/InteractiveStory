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
    public bool WeaponDrawn;
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
        if (isInputLocked)
        {
            // [VI] 1. Nếu bị khóa -> Reset toàn bộ trạng thái phím bấm về Rỗng
            // [EN] 1. If locked -> Reset all key states to Empty
            inputs = new CharacterInputs();

            // [VI] 2. Tắt tâm ngắm và chặn súng xả đạn (Tránh lỗi kẹt chuột lúc bật UI)
            // [EN] 2. Turn off crosshair and block gun from firing (Prevent mouse stuck bug when opening UI)
            if (DynamicCrosshair.Instance != null)
            {
                DynamicCrosshair.Instance.SetAimingState(false);
                DynamicCrosshair.Instance.SetShootingState(false);
            }
            if (gunController != null)
            {
                gunController.HandleFiring(false, false);
            }
        }
        else
        {
            // ==========================================
            // [VI] ĐỌC INPUT VÀ XỬ LÝ (CHỈ CHẠY KHI KHÔNG BỊ KHÓA)
            // [EN] READ AND PROCESS INPUT (ONLY RUNS WHEN NOT LOCKED)
            // ==========================================
            inputs.MoveInput = playerInput.actions["Move"].ReadValue<Vector2>();
            inputs.LookInput = playerInput.actions["Look"].ReadValue<Vector2>();
            inputs.JumpDown = playerInput.actions["Jump"].WasPressedThisFrame();
            inputs.JumpHeld = playerInput.actions["Jump"].IsPressed();
            inputs.SlideDown = playerInput.actions["Crouch"].IsPressed();
            inputs.SlidePressedThisFrame = playerInput.actions["Crouch"].WasPressedThisFrame();

            bool aimInput = playerInput.actions["Aim"].IsPressed();
            bool attackDown = playerInput.actions["Attack"].WasPressedThisFrame();
            bool attackHeld = playerInput.actions["Attack"].IsPressed();

            inputs.AimHeld = aimInput || attackHeld;
            inputs.FirePressedThisFrame = attackDown;

            // Xử lý Sprint Toggle
            if (playerInput.actions["Sprint"].WasPressedThisFrame())
            {
                inputs.SprintToggle = !inputs.SprintToggle;
            }
            if (inputs.MoveInput.sqrMagnitude == 0)
            {
                inputs.SprintToggle = false;
            }

            // Giao tiếp với UI Tâm ngắm mới
            if (DynamicCrosshair.Instance != null)
            {
                DynamicCrosshair.Instance.SetAimingState(inputs.AimHeld);
            }

            // Giao phó quyền bắn cho GunController
            if (gunController != null)
            {
                // [VI] Luôn gọi HandleFiring để GunController cập nhật cờ _isShooting cho Crosshair
                // [EN] Always call HandleFiring so GunController updates the _isShooting flag for Crosshair
                gunController.HandleFiring(attackDown, attackHeld);
            }

            // Hệ thống đổi vũ khí
            int weaponIndexToEquip = -1;
            if (Keyboard.current.digit1Key.wasPressedThisFrame) weaponIndexToEquip = 0;
            if (Keyboard.current.digit2Key.wasPressedThisFrame) weaponIndexToEquip = 1;
            if (Keyboard.current.digit3Key.wasPressedThisFrame) weaponIndexToEquip = 2;
            if (Keyboard.current.digit4Key.wasPressedThisFrame) weaponIndexToEquip = 3;

            if (weaponIndexToEquip != -1 && weaponInventory != null && gunController != null)
            {
                if (weaponIndexToEquip < weaponInventory.availableGuns.Count)
                {
                    if (weaponInventory.currentGunIndex == weaponIndexToEquip)
                    {
                        gunController.SetWeaponDrawnState(!gunController.isWeaponDrawn);
                    }
                    else
                    {
                        weaponInventory.EquipWeapon(weaponIndexToEquip);
                        gunController.EquipWeapon(weaponInventory.CurrentWeapon);
                        gunController.SetWeaponDrawnState(true);
                    }
                }
            }
        }

        // ==========================================
        // [VI] CODE VẬT LÝ VÀ ANIMATOR BẮT BUỘC PHẢI CHẠY LIÊN TỤC
        // [EN] PHYSICS AND ANIMATOR CODE MUST RUN CONTINUOUSLY
        // ==========================================
        inputs.WeaponDrawn = gunController != null && gunController.isWeaponDrawn;

        if (playerCharacter != null)
        {
            playerCharacter.UpdateBody(Time.unscaledDeltaTime);
            playerCharacter.UpdateInput(ref inputs);
        }
    }

    private void LateUpdate()
    {
        transform.position = playerCharacter.transform.position;
        if (playerCamera != null)
        {
            playerCamera.UpdateCameraLogic(ref inputs, Time.unscaledDeltaTime);
            playerCamera.UpdateCameraPolish(playerCharacter.CurrentStance, inputs.MoveInput.x, inputs.AimHeld, Time.unscaledDeltaTime);
        }
    }

    public void SetInputLock(bool state)
    {
        isInputLocked = state;
    }

}
