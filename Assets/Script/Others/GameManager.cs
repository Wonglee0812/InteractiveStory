using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Yarn.Unity;
using UnityEngine.SceneManagement;
using System.Collections;

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

    [Header("--- LOCATION EVENTS: ARMORY ---")]
    public UnityEvent onArmoryChooseZenith;
    public UnityEvent onArmoryChooseApex;

    [Header("--- LOCATION EVENTS: MONITOR ---")]
    public UnityEvent onMonitorChooseZenith;
    public UnityEvent onMonitorChooseApex;

    [Header("--- FINAL ENDING EVENTS ---")]
    [Tooltip("VI: Kích hoạt khi chạy đến điểm cuối và Player đã chọn THEO Zenith.\nEN: Triggered at the end point if Player TRUSTED Zenith.")]
    public UnityEvent onEndingTrustZenith;

    [Tooltip("VI: Kích hoạt khi chạy đến điểm cuối và Player đã chọn THEO Apex.\nEN: Triggered at the end point if Player TRUSTED Apex.")]
    public UnityEvent onEndingLoyalToApex;

    [Header("--- FADE EFFECT SETTINGS ---")]
    [Tooltip("VI: Kéo thả thành phần CanvasGroup của UI màn hình đen vào đây.\nEN: Drag and drop the CanvasGroup component of the black screen UI here.")]
    public CanvasGroup fadeCanvasGroup;

    [Tooltip("VI: Thời gian hiệu ứng làm mờ diễn ra (tính bằng giây).\nEN: Fade effect duration (in seconds).")]
    public float fadeDuration = 1.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return; // VI: Ngăn code chạy tiếp nếu bị hủy / EN: Prevent further execution if destroyed
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        ClearQuest();
    }

    private void OnEnable()
    {
        if (dialogueRunner == null) return;

        // ==========================================
        // VI: ĐĂNG KÝ LỆNH CHO QUEST 
        // EN: REGISTER QUEST COMMANDS
        // ==========================================
        dialogueRunner.AddCommandHandler<string>("set_quest", SetQuest);
        dialogueRunner.AddCommandHandler("complete_quest", CompleteQuest);
        dialogueRunner.AddCommandHandler("clear_quest", ClearQuest);

        // ==========================================
        // VI: ĐĂNG KÝ LỆNH CHO CỐT TRUYỆN 
        // EN: REGISTER STORY COMMANDS
        // VI: Thêm <string> để hàm có thể nhận tên khu vực (armory/monitor)
        // EN: Added <string> so the function can receive the location name (armory/monitor)
        // ==========================================
        dialogueRunner.AddCommandHandler<string>("choose_zenith", ChooseZenith);
        dialogueRunner.AddCommandHandler<string>("choose_apex", ChooseApex);
    }

    // VI: Hàm giờ đây yêu cầu truyền vào 1 chuỗi (string location)
    // EN: The function now requires passing a string (string location)
    public void ChooseZenith(string location)
    {
        Debug.Log($"<color=green>[STORY]</color> Người chơi đã chọn: PHẢN BỘI ĐỂ THEO ZENITH (Tại: {location}).");
        isTrustingZenith = true;

        if (location == "armory" && onArmoryChooseZenith != null) onArmoryChooseZenith.Invoke();
        else if (location == "monitor" && onMonitorChooseZenith != null) onMonitorChooseZenith.Invoke();
    }

    // VI: Hàm giờ đây yêu cầu truyền vào 1 chuỗi (string location)
    // EN: The function now requires passing a string (string location)
    public void ChooseApex(string location)
    {
        Debug.Log($"<color=cyan>[STORY]</color> Người chơi đã chọn: TRUNG THÀNH VỚI APEX (Tại: {location}).");
        isTrustingZenith = false;

        if (location == "armory" && onArmoryChooseApex != null) onArmoryChooseApex.Invoke();
        else if (location == "monitor" && onMonitorChooseApex != null) onMonitorChooseApex.Invoke();
    }

    public void TriggerFinalEndingEvents()
    {
        if (isTrustingZenith)
        {
            Debug.Log("<color=green>[ENDING]</color> Gọi sự kiện: Theo Zenith.");
            if (onEndingTrustZenith != null) onEndingTrustZenith.Invoke();
        }
        else
        {
            Debug.Log("<color=cyan>[ENDING]</color> Gọi sự kiện: Trung thành với Apex.");
            if (onEndingLoyalToApex != null) onEndingLoyalToApex.Invoke();
        }
    }

    public void ExecuteEndingTransition()
    {
        if (isTrustingZenith)
        {
            Debug.Log("<color=green>[SCENE]</color> Bắt đầu chuyển sang Scene: ZenithEndingScene.");
            ChangeSceneWithFade("ZenithEndingScene");
        }
        else
        {
            Debug.Log("<color=cyan>[SCENE]</color> Bắt đầu chuyển sang Scene: ApexEndingScene.");
            ChangeSceneWithFade("ApexEndingScene");
        }
    }

    public void SetQuest(string questDescription)
    {
        if (questPanel != null) questPanel.SetActive(true);
        if (questTextUI != null)
        {
            questTextUI.text = "Objective: " + questDescription;
            questTextUI.color = Color.white;
        }
    }

    public void CompleteQuest()
    {
        if (questTextUI != null)
        {
            questTextUI.text = "<s>" + questTextUI.text + "</s>";
            questTextUI.color = Color.green;
        }
    }

    public void ClearQuest()
    {
        if (questPanel != null) questPanel.SetActive(false);
        if (questTextUI != null) questTextUI.text = "";
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
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(0.5f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / fadeDuration));
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }
}