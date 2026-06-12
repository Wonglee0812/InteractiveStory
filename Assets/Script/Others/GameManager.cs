using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Yarn.Unity;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Localization.Settings; // [VI] Thư viện Unity Localization / [EN] Unity Localization library

public class GameManager : MonoBehaviour
{
    // VI: Tạo Singleton để các thành phần khác dễ dàng truy cập
    // EN: Create Singleton for easy access by other components
    public static GameManager Instance { get; private set; }
    public enum GameLanguage { Vietnamese = 0, English = 1 }

    [Header("--- LOCALIZATION STATE ---")]
    [Tooltip("VI: Ngôn ngữ hiện tại của hệ thống.\nEN: Current system language.")]
    public GameLanguage currentLanguage = GameLanguage.Vietnamese;

    [Header("--- YARN SPINNER ---")]
    public DialogueRunner dialogueRunner;

    [Header("--- STORY STATES ---")]
    [Tooltip("VI: Lựa chọn của Player. True = Theo Zenith, False = Theo Apex.\nEN: Player's choice. True = Trust Zenith, False = Trust Apex.")]
    public bool isTrustingZenith = false;

    [Header("--- QUEST UI ---")]
    public GameObject questPanel;
    public TextMeshProUGUI questTextUI;
    [Tooltip("VI: Tên của String Table chứa nhiệm vụ.\nEN: Name of the String Table containing quests.")]
    public string questStringTableName = "QuestTable";
    [Header("--- TYPING EFFECT ---")]
    [Tooltip("VI: Thời gian chờ giữa mỗi chữ cái (giây).\nEN: Delay between each letter (seconds).")]
    public float typingSpeed = 0.03f;
    private Coroutine typingCoroutine; // Lưu trữ tiến trình gõ chữ để dừng lại nếu có nhiệm vụ mới chèn vào / Stores the typing process to stop it if a new quest interrupts


    [Header("--- LOCATION EVENTS: ARMORY ---")]
    public UnityEvent onArmoryChooseZenith;
    public UnityEvent onArmoryChooseApex;

    [Header("--- LOCATION EVENTS: MONITOR ---")]
    public UnityEvent onMonitorChooseZenith;
    public UnityEvent onMonitorChooseApex;

    [Header("--- FINAL ENDING EVENTS ---")]
    public UnityEvent onEndingTrustZenith;
    public UnityEvent onEndingLoyalToApex;

    [Header("--- FADE EFFECT SETTINGS ---")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1.5f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    // [VI] Chuyển Start thành IEnumerator để đợi hệ thống Localization khởi tạo xong
    // [EN] Change Start to IEnumerator to wait for Localization system to initialize
    private IEnumerator Start()
    {
        ClearQuest();

        // [VI] Chờ hệ thống Localization sẵn sàng
        // [EN] Wait for Localization system to be ready
        yield return LocalizationSettings.InitializationOperation;

        // [VI] Đồng bộ ngôn ngữ khởi điểm
        // [EN] Sync initial language
        UpdateCurrentLanguage(LocalizationSettings.SelectedLocale);

        // [VI] Đăng ký lắng nghe sự kiện mỗi khi ngôn ngữ của game bị thay đổi
        // [EN] Subscribe to listen to the event whenever the game's language changes
        LocalizationSettings.SelectedLocaleChanged += UpdateCurrentLanguage;
    }

    private void OnDestroy()
    {
        // [VI] Hủy đăng ký lắng nghe khi GameManager bị hủy để tránh rò rỉ bộ nhớ
        // [EN] Unsubscribe when GameManager is destroyed to prevent memory leaks
        LocalizationSettings.SelectedLocaleChanged -= UpdateCurrentLanguage;
    }

    // ==========================================
    // VI: HÀM ĐỒNG BỘ NGÔN NGỮ VỚI UNITY LOCALIZATION
    // EN: UNITY LOCALIZATION SYNC FUNCTION
    // ==========================================
    private void UpdateCurrentLanguage(UnityEngine.Localization.Locale locale)
    {
        // [VI] Kiểm tra mã code của Locale (vd: "vi" hoặc "en") để đồng bộ với Enum
        // [EN] Check the Locale code (e.g., "vi" or "en") to sync with the Enum
        if (locale.Identifier.Code.Contains("vi"))
        {
            currentLanguage = GameLanguage.Vietnamese;
        }
        else
        {
            currentLanguage = GameLanguage.English;
        }

        Debug.Log($"<color=orange>[LOCALIZATION]</color> GameManager đã đồng bộ: {currentLanguage}");
    }

    private void OnEnable()
    {
        if (dialogueRunner == null) return;

        // [VI] Sửa <string, string> để nhận cả 2 bản dịch Quest từ Yarn
        // [EN] Fixed <string, string> to receive both Quest translations from Yarn
        dialogueRunner.AddCommandHandler<string, string>("set_quest", SetQuest);
        dialogueRunner.AddCommandHandler("complete_quest", CompleteQuest);
        dialogueRunner.AddCommandHandler("clear_quest", ClearQuest);

        dialogueRunner.AddCommandHandler<string>("choose_zenith", ChooseZenith);
        dialogueRunner.AddCommandHandler<string>("choose_apex", ChooseApex);
    }

    public void ChooseZenith(string location)
    {
        Debug.Log($"<color=green>[STORY]</color> Người chơi đã chọn: PHẢN BỘI ĐỂ THEO ZENITH (Tại: {location}).");
        isTrustingZenith = true;
        if (location == "armory" && onArmoryChooseZenith != null) onArmoryChooseZenith.Invoke();
        else if (location == "monitor" && onMonitorChooseZenith != null) onMonitorChooseZenith.Invoke();
    }

    public void ChooseApex(string location)
    {
        Debug.Log($"<color=cyan>[STORY]</color> Người chơi đã chọn: TRUNG THÀNH VỚI APEX (Tại: {location}).");
        isTrustingZenith = false;
        if (location == "armory" && onArmoryChooseApex != null) onArmoryChooseApex.Invoke();
        else if (location == "monitor" && onMonitorChooseApex != null) onMonitorChooseApex.Invoke();
    }

    public void TriggerFinalEndingEvents()
    {
        if (isTrustingZenith) { if (onEndingTrustZenith != null) onEndingTrustZenith.Invoke(); }
        else { if (onEndingLoyalToApex != null) onEndingLoyalToApex.Invoke(); }
    }

    public void ExecuteEndingTransition()
    {
        if (isTrustingZenith) ChangeSceneWithFade("ZenithEndingScene");
        else ChangeSceneWithFade("ApexEndingScene");
    }

    public void SetQuest(string questDescEN, string questDescVN)
    {
        if (questPanel != null) questPanel.SetActive(true);
        if (questTextUI != null)
        {
            string finalText = (currentLanguage == GameLanguage.Vietnamese) ? questDescVN : questDescEN;
            string prefix = (currentLanguage == GameLanguage.Vietnamese) ? "Nhiệm vụ: " : "Objective: ";

            questTextUI.color = Color.white;

            // VI: Dừng hiệu ứng gõ chữ cũ nếu đang chạy / EN: Stop old typing effect if running
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);

            // VI: Bắt đầu gõ chữ mới / EN: Start new typing
            typingCoroutine = StartCoroutine(TypeQuestText(prefix + finalText));
        }
    }

    // ==========================================
    // VI: HÀM XỬ LÝ NHIỆM VỤ TỪ INSPECTOR
    // EN: QUEST FUNCTIONS FROM INSPECTOR
    // ==========================================
    public void SetQuestFromInspector(string questKey)
    {
        if (questPanel != null) questPanel.SetActive(true);
        if (questTextUI != null)
        {
            var localizedString = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedStringAsync(questStringTableName, questKey);

            localizedString.Completed += (handle) =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    string prefix = (currentLanguage == GameLanguage.Vietnamese) ? "Nhiệm vụ: " : "Objective: ";
                    questTextUI.color = Color.white;

                    // VI: Dừng hiệu ứng gõ chữ cũ nếu đang chạy / EN: Stop old typing effect if running
                    if (typingCoroutine != null) StopCoroutine(typingCoroutine);

                    // VI: Bắt đầu gõ chữ mới / EN: Start new typing
                    typingCoroutine = StartCoroutine(TypeQuestText(prefix + handle.Result));
                }
                else
                {
                    Debug.LogWarning($"<color=red>[LOCALIZATION]</color> Không tìm thấy Key nhiệm vụ: {questKey} trong bảng {questStringTableName}");
                }
            };
        }
    }

    public void CompleteQuest()
    {
        if (questTextUI != null)
        {
            // VI: Nếu Quest hoàn thành mà chữ còn đang gõ dở, ép nó dừng lại và gạch ngang luôn
            // EN: If Quest completes while still typing, force it to stop and strike through immediately
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);

            questTextUI.text = "<s>" + questTextUI.text + "</s>";
            questTextUI.color = Color.green;
        }
    }

    public void ClearQuest()
    {
        // VI: Dọn dẹp tiến trình gõ chữ khi ẩn UI
        // EN: Clean up typing process when hiding UI
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (questPanel != null) questPanel.SetActive(false);
        if (questTextUI != null) questTextUI.text = "";
    }
    private IEnumerator TypeQuestText(string fullText)
    {
        questTextUI.text = ""; // VI: Xóa chữ cũ / EN: Clear old text

        foreach (char letter in fullText.ToCharArray())
        {
            questTextUI.text += letter; // VI: Thêm từng chữ cái / EN: Add each letter
            yield return new WaitForSeconds(typingSpeed); // VI: Đợi một nhịp / EN: Wait a beat
        }
    }

    public void ChangeSceneWithFade(string targetSceneName)
    {
        StartCoroutine(FadeAndLoadRoutine(targetSceneName));
    }

    private IEnumerator FadeAndLoadRoutine(string sceneName)
    {
        if (fadeCanvasGroup == null || string.IsNullOrEmpty(sceneName))
        {
            if (!string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(sceneName);
            yield break;
        }
        fadeCanvasGroup.blocksRaycasts = true;
        float timer = 0f;
        while (timer < fadeDuration) { timer += Time.deltaTime; fadeCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration); yield return null; }
        fadeCanvasGroup.alpha = 1f;
        yield return new WaitForSeconds(0.5f);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone) yield return null;
        timer = 0f;
        while (timer < fadeDuration) { timer += Time.deltaTime; fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / fadeDuration)); yield return null; }
        fadeCanvasGroup.alpha = 0f; fadeCanvasGroup.blocksRaycasts = false;
    }
}