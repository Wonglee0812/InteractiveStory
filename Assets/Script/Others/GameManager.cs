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

    [Tooltip("VI: Biến ghi nhớ lựa chọn của Player. True = Theo Zenith, False = Theo Apex.\nEN: Variable remembering Player's choice. True = Trust Zenith, False = Trust Apex.")]
    public bool isTrustingZenith = false;

    [Header("--- QUEST UI ---")]
    public GameObject questPanel;
    public TextMeshProUGUI questTextUI;

    [Header("--- NARRATIVE BRANCHES: ARMORY ---")]
    public UnityEvent onArmoryTrustZenith;
    public UnityEvent onArmoryBetrayZenith;

    [Header("--- NARRATIVE BRANCHES: MONITOR ---")]
    public UnityEvent onMonitorTrustZenith;
    public UnityEvent onMonitorBetrayZenith;

    [Header("--- NARRATIVE BRANCHES: ENDING ---")]
    [Tooltip("VI: Kích hoạt khi chạy đến điểm cuối và Player đã chọn THEO Zenith.\nEN: Triggered at the end point if Player TRUSTED Zenith.")]
    public UnityEvent onEndingTrustZenith;

    [Tooltip("VI: Kích hoạt khi chạy đến điểm cuối và Player đã chọn THEO Apex (Phản Zenith).\nEN: Triggered at the end point if Player TRUSTED Apex (Betrayed Zenith).")]
    public UnityEvent onEndingTrustApex;

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
        isTrustingZenith = true; // Lưu lựa chọn / Save choice
        if (onArmoryTrustZenith != null) onArmoryTrustZenith.Invoke();
    }

    private void ArmoryBetrayZenith()
    {
        Debug.Log("<color=yellow>[STORY]</color> Armory Branch: Betray Zenith");
        isTrustingZenith = false; // Lưu lựa chọn / Save choice
        if (onArmoryBetrayZenith != null) onArmoryBetrayZenith.Invoke();
    }

    private void MonitorTrustZenith()
    {
        Debug.Log("<color=yellow>[STORY]</color> Monitor Branch: Trust Zenith");
        isTrustingZenith = true; // Lưu lựa chọn / Save choice
        if (onMonitorTrustZenith != null) onMonitorTrustZenith.Invoke();
    }

    private void MonitorBetrayZenith()
    {
        Debug.Log("<color=yellow>[STORY]</color> Monitor Branch: Betray Zenith");
        isTrustingZenith = false; // Lưu lựa chọn / Save choice
        if (onMonitorBetrayZenith != null) onMonitorBetrayZenith.Invoke();
    }

    // ==========================================
    // HÀM KÍCH HOẠT ENDING TỪ MÁY BAY
    // ENDING TRIGGER FUNCTIONS FROM HELICOPTER
    // ==========================================
    public void EndingGiveCore()
    {
        Debug.Log("<color=magenta>[ACTION]</color> Player đã chạy vào vùng sáng và NỘP LÕI!");
        if (isTrustingZenith)
        {
            Debug.Log("<color=green>=> Kết quả:</color> THEO ZENITH (Lên máy bay thoát khỏi hòn đảo).");
            if (onEndingTrustZenith != null) onEndingTrustZenith.Invoke();
        }
        else
        {
            Debug.Log("<color=red>=> Kết quả:</color> THEO APEX (Giao nộp lõi nhưng bị tẩu tán/tiêu diệt).");
            if (onEndingTrustApex != null) onEndingTrustApex.Invoke();
        }
    }

    public void EndingShootHelicopter()
    {
        Debug.Log("<color=magenta>[ACTION]</color> Player đã BẮN RƠI máy bay Apex!");
        if (isTrustingZenith)
        {
            Debug.Log("<color=orange>=> Kết quả:</color> THEO ZENITH (Zenith thả quân ra, kích hoạt Escape Run).");
        }
        else
        {
            Debug.Log("<color=orange>=> Kết quả:</color> THEO APEX (Bắn rụng mồi nhử, kẹt lại đảo tử chiến).");
        }
    }
}