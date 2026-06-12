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

    [Header("--- YARN SPINNER ---")]
    public DialogueRunner dialogueRunner;

    [Header("--- STORY STATES ---")]
    [Tooltip("VI: Lựa chọn của Player. True = Theo Zenith, False = Theo Apex.\nEN: Player's choice. True = Trust Zenith, False = Trust Apex.")]
    public bool isTrustingZenith = false;
    
    [Header("--- QUEST UI ---")]
    public GameObject questPanel;
    public TextMeshProUGUI questTextUI;

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
        // VI: Ẩn bảng nhiệm vụ lúc bắt đầu game
        // EN: Hide quest panel at the start of the game
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
        // ==========================================
        dialogueRunner.AddCommandHandler("choose_zenith", ChooseZenith);
        dialogueRunner.AddCommandHandler("choose_apex", ChooseApex);
    }


    public void ChooseZenith()
    {
        Debug.Log("<color=green>[STORY]</color> Người chơi đã chọn: PHẢN BỘI ĐỂ THEO ZENITH.");
        isTrustingZenith = true;
    }

    public void ChooseApex()
    {
        Debug.Log("<color=cyan>[STORY]</color> Người chơi đã chọn: TRUNG THÀNH VỚI APEX.");
        isTrustingZenith = false;
    }

    // ==========================================
    // VI: HÀM KÍCH HOẠT ENDING 
    // EN: ENDING TRIGGER FUNCTION
    // ==========================================
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

    // ==========================================
    // VI: BƯỚC 2 - HÀM CHUYỂN SCENE ENDING 
    // EN: STEP 2 - LOAD ENDING SCENE FUNCTION
    // VI: (Bạn có thể tự gọi hàm này từ Animation Event, Timeline, hoặc code bên ngoài)
    // EN: (You can manually call this from an Animation Event, Timeline, or external code)
    // ==========================================
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

    // ==========================================
    // VI: HÀM XỬ LÝ NHIỆM VỤ 
    // EN: QUEST FUNCTIONS
    // ==========================================
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

    // ==========================================
    // VI: QUẢN LÝ FADE VÀ CHUYỂN SCENE
    // EN: FADE AND SCENE MANAGEMENT
    // ==========================================
    public void ChangeSceneWithFade(string targetSceneName)
    {
        StartCoroutine(FadeAndLoadRoutine(targetSceneName));
    }

    private IEnumerator FadeAndLoadRoutine(string sceneName)
    {
        // VI: Kiểm tra an toàn trước khi chạy
        // EN: Safety check before running
        if (fadeCanvasGroup == null || string.IsNullOrEmpty(sceneName))
        {
            if (!string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(sceneName);
            yield break;
        }

        fadeCanvasGroup.blocksRaycasts = true;

        // VI: Fade In (Màn hình tối dần) / EN: Fade In (Screen gets dark)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(0.5f);

        // VI: Tải cảnh mới / EN: Load new scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // VI: Fade Out (Màn hình sáng dần) / EN: Fade Out (Screen lights up)
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