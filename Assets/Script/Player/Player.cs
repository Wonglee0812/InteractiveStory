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
        // [VI] Gom Input từ Unity Input System / [EN] Gather Input from Unity Input System
        inputs.MoveInput = playerInput.actions["Move"].ReadValue<Vector2>();
        inputs.LookInput = playerInput.actions["Look"].ReadValue<Vector2>();
        inputs.JumpDown = playerInput.actions["Jump"].WasPressedThisFrame();
        inputs.JumpHeld = playerInput.actions["Jump"].IsPressed();
        inputs.SlideDown = playerInput.actions["Crouch"].IsPressed(); // Tái sử dụng biến struct SlideDown làm phím Crouch giữ
        inputs.SlidePressedThisFrame = playerInput.actions["Crouch"].WasPressedThisFrame(); // Tái sử dụng biến struct SlidePressedThisFrame làm phím Crouch vừa bấm


        // 2. [VI] Đọc Input súng đạn (Đã sửa lỗi WasPerformedThisFrame)
        //    [EN] Read Gun Input (Fixed WasPerformedThisFrame error)
        bool aimInput = playerInput.actions["Aim"].IsPressed();
        bool attackDown = playerInput.actions["Attack"].WasPressedThisFrame();
        bool attackHeld = playerInput.actions["Attack"].IsPressed();

        // Ép ngắm (Style TLOU)
        inputs.AimHeld = aimInput || attackHeld;
        inputs.FirePressedThisFrame = attackDown;

        if (isInputLocked) { inputs = new CharacterInputs(); }

        // 3. [Logic Bắn] Kiểm tra Aim mới cho bắn
        if (inputs.AimHeld && gunController != null && weaponInventory != null)
        {
            GunData gun = weaponInventory.CurrentWeapon;
            if (gun != null)
            {
                bool wantsToShoot = gun.isAutomatic ? attackHeld : attackDown;
                if (wantsToShoot)
                {
                    Debug.Log($"<color=cyan>[DEBUG]</color> Lệnh bắn được gửi từ Player. Súng: {gun.gunName}");
                    gunController.Shoot();
                }
            }
        }


        if (playerCharacter != null)
        {
            playerCharacter.UpdateBody(Time.unscaledDeltaTime);
            playerCharacter.UpdateInput(ref inputs); // [VI] DÒNG THÊM MỚI / [EN] NEWLY ADDED LINE

        }
        if (playerInput.actions["Sprint"].WasPressedThisFrame())
        {
            inputs.SprintToggle = !inputs.SprintToggle; // Bấm 1 lần bật, bấm lần nữa tắt
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
            // Tắt Slow-motion phòng hờ / Turn off Slow-mo just in case
            TimeManager.Instance.RequestSlowMotion(false);
        }
    }

}
