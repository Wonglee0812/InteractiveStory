using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("--- INPUT COMPONENT ---")]
    [Tooltip("VI: Kéo đối tượng chứa PlayerInput của bạn vào đây.\nEN: Drag your GameObject containing PlayerInput here.")]
    [SerializeField] private PlayerInput playerInput;

    [Header("--- UI PANELS ---")]
    [SerializeField] private GameObject menuPanel;

    [Header("--- LOCALIZATION UI ---")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private bool isMenuOpen = false;

    private void Start()
    {
        // VI: Ẩn Menu khi bắt đầu game
        // EN: Hide Menu at game start
        if (menuPanel != null) menuPanel.SetActive(false);

        // VI: Đọc trạng thái ban đầu để đồng bộ giá trị Dropdown
        // EN: Read initial state to sync Dropdown values
        if (languageDropdown != null && GameManager.Instance != null)
        {
            languageDropdown.value = (int)GameManager.Instance.currentLanguage;

            // VI: Đăng ký sự kiện lắng nghe khi người chơi thay đổi Dropdown công cụ
            // EN: Register event listener for when the player changes the Dropdown tool
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }
    }

    private void Update()
    {
        if (playerInput == null) return;

        // VI: Đọc Action tên là "Menu" từ hệ thống Input mới giống cách làm của file Player.cs
        // EN: Read the Action named "Menu" from the new Input System exactly like in Player.cs
        if (playerInput.actions["Menu"].WasPressedThisFrame())
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        if (menuPanel != null) menuPanel.SetActive(isMenuOpen);

        // VI: Đóng băng hoặc cho game tiếp tục chạy
        // EN: Freeze or resume the game loop
        Time.timeScale = isMenuOpen ? 0f : 1f;

        // VI: Quản lý con trỏ chuột
        // EN: Manage mouse cursor state
        if (isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // VI: Hàm xử lý khi nhấn nút hoặc đổi Dropdown ngôn ngữ
    // EN: Function invoked when the language Dropdown changes
    private void OnLanguageChanged(int index)
    {
    }
}