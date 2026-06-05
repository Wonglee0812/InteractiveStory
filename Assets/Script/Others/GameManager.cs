using UnityEngine;
using UnityEngine.Events; // [VI] Bắt buộc phải có để dùng UnityEvent / [EN] Mandatory to use UnityEvent
using TMPro;
using Yarn.Unity;

public class GameManager : MonoBehaviour
{
    // VI: Tạo Singleton để các thành phần khác dễ dàng truy cập
    // EN: Create Singleton for easy access by other components
    public static GameManager Instance { get; private set; }

    [Header("--- YARN SPINNER ---")]
    public DialogueRunner dialogueRunner;

    [Header("--- STORY STATES (Sổ tay cốt truyện) ---")]
    public bool isArmoryCleared = false;

    [Header("--- QUEST UI ---")]
    public GameObject questPanel;
    public TextMeshProUGUI questTextUI;

    [Header("--- NARRATIVE BRANCHES: ARMORY ---")]
    [Tooltip("VI: Kích hoạt khi Player chọn THEO Zenith tại Armory.\nEN: Triggered when Player TRUSTS Zenith at the Armory.")]
    public UnityEvent onArmoryTrustZenith;

    [Tooltip("VI: Kích hoạt khi Player chọn KHÔNG THEO Zenith tại Armory (Tắt General, Bật Quái).\nEN: Triggered when Player BETRAYS Zenith at the Armory (Disable General, Enable Enemies).")]
    public UnityEvent onArmoryBetrayZenith;

    [Header("--- NARRATIVE BRANCHES: MONITOR ---")]
    [Tooltip("VI: Kích hoạt khi Player chọn THEO Zenith tại Monitor.\nEN: Triggered when Player TRUSTS Zenith at the Monitor.")]
    public UnityEvent onMonitorTrustZenith;

    [Tooltip("VI: Kích hoạt khi Player chọn KHÔNG THEO Zenith tại Monitor.\nEN: Triggered when Player BETRAYS Zenith at the Monitor.")]
    public UnityEvent onMonitorBetrayZenith;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
        // ĐĂNG KÝ LỆNH CHO QUEST (QUEST COMMANDS)
        // ==========================================
        dialogueRunner.AddCommandHandler<string>("set_quest", SetQuest);
        dialogueRunner.AddCommandHandler("complete_quest", CompleteQuest);
        dialogueRunner.AddCommandHandler("clear_quest", ClearQuest);

        // ==========================================
        // ĐĂNG KÝ LỆNH CHO CỐT TRUYỆN (STORY COMMANDS)
        // ==========================================
        dialogueRunner.AddCommandHandler("armory_trust_zenith", ArmoryTrustZenith);
        dialogueRunner.AddCommandHandler("armory_betray_zenith", ArmoryBetrayZenith);
        dialogueRunner.AddCommandHandler("monitor_trust_zenith", MonitorTrustZenith);
        dialogueRunner.AddCommandHandler("monitor_betray_zenith", MonitorBetrayZenith);
    }

    // ==========================================
    // HÀM XỬ LÝ NHIỆM VỤ (QUEST FUNCTIONS)
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
    // HÀM XỬ LÝ CỐT TRUYỆN (STORY FUNCTIONS)
    // ==========================================
    private void ArmoryTrustZenith()
    {
        Debug.Log("<color=yellow>[STORY]</color> Armory Branch: Trust Zenith");
        if (onArmoryTrustZenith != null) onArmoryTrustZenith.Invoke();
    }

    private void ArmoryBetrayZenith()
    {
        Debug.Log("<color=yellow>[STORY]</color> Armory Branch: Betray Zenith");
        if (onArmoryBetrayZenith != null) onArmoryBetrayZenith.Invoke();
    }

    private void MonitorTrustZenith()
    {
        Debug.Log("<color=yellow>[STORY]</color> Monitor Branch: Trust Zenith");
        if (onMonitorTrustZenith != null) onMonitorTrustZenith.Invoke();
    }

    private void MonitorBetrayZenith()
    {
        Debug.Log("<color=yellow>[STORY]</color> Monitor Branch: Betray Zenith");
        if (onMonitorBetrayZenith != null) onMonitorBetrayZenith.Invoke();
    }
}