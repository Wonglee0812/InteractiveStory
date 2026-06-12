using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using UnityEngine.Localization.Settings; // [VI] Thư viện Unity Localization / [EN] Unity Localization library

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

    // [VI] Chuyển Start thành IEnumerator
    // [EN] Change Start to IEnumerator
    private IEnumerator Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);

        // [VI] Chờ hệ thống Localization khởi tạo để tránh lỗi Null
        // [EN] Wait for Localization system to initialize to avoid Null errors
        yield return LocalizationSettings.InitializationOperation;

        if (languageDropdown != null)
        {
            // [VI] Đồng bộ chỉ số Dropdown với ngôn ngữ đang được chọn trong Unity
            // [EN] Sync Dropdown index with the currently selected language in Unity
            var currentLocale = LocalizationSettings.SelectedLocale;
            var locales = LocalizationSettings.AvailableLocales.Locales;

            for (int i = 0; i < locales.Count; i++)
            {
                if (locales[i] == currentLocale)
                {
                    languageDropdown.value = i;
                    break;
                }
            }

            // [VI] Đăng ký sự kiện
            // [EN] Register event
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }
    }

    private void Update()
    {
        if (playerInput == null) return;

        if (playerInput.actions["Menu"].WasPressedThisFrame())
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        if (menuPanel != null) menuPanel.SetActive(isMenuOpen);
        Time.timeScale = isMenuOpen ? 0f : 1f;

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

    private void OnLanguageChanged(int index)
    {
        // [VI] Gọi hàm thay đổi ngôn ngữ thông qua Coroutine
        // [EN] Call the language change function via Coroutine
        StartCoroutine(SetLocale(index));
    }

    // ==========================================
    // VI: Lệnh đổi ngôn ngữ trực tiếp cho Unity Package
    // EN: Direct language change command for Unity Package
    // ==========================================
    private IEnumerator SetLocale(int localeID)
    {
        // [VI] Chờ hệ thống xác nhận trước khi đổi
        // [EN] Wait for system confirmation before changing
        yield return LocalizationSettings.InitializationOperation;

        // [VI] Đổi ngôn ngữ nội bộ của Unity. Sự kiện SelectedLocaleChanged sẽ tự động được kích hoạt
        // [EN] Change Unity's internal language. The SelectedLocaleChanged event will automatically trigger
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];
    }
}