using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

// [VI] Cấu trúc để map Tên nhân vật trong Yarn với Animator của họ trên Scene
// [EN] Struct to map Character Name in Yarn with their Animator in the Scene
[System.Serializable]
public struct ActorAnimationSetup
{
    public string characterName;
    public Animator animator;
}

public class CutsceneAnimationController : MonoBehaviour
{
    [Header("--- YARN SPINNER ---")]
    public DialogueRunner dialogueRunner;

    [Header("--- ACTORS ---")]
    [Tooltip("Danh sách các nhân vật và Animator của họ / List of characters and their Animators")]
    public List<ActorAnimationSetup> actors;

    // Từ điển lưu trữ Animator để tìm kiếm nhanh O(1) / Dictionary to store Animators for fast O(1) lookup
    private Dictionary<string, Animator> _animatorDict;

    private void Awake()
    {
        // [VI] Nạp toàn bộ Actor từ List vào Dictionary khi game bắt đầu
        // [EN] Load all Actors from the List into the Dictionary when game starts
        _animatorDict = new Dictionary<string, Animator>();
        foreach (var actor in actors)
        {
            if (actor.animator != null && !_animatorDict.ContainsKey(actor.characterName))
            {
                _animatorDict.Add(actor.characterName, actor.animator);
            }
        }
    }

    private void OnEnable()
    {
        // [VI] Đăng ký các lệnh Animation cho Yarn Spinner
        // [EN] Register Animation commands for Yarn Spinner
        if (dialogueRunner != null)
        {
            dialogueRunner.AddCommandHandler<string, string>("play_anim", PlayAnimation);
            dialogueRunner.AddCommandHandler<string, string>("trigger_anim", TriggerAnimation);
            dialogueRunner.AddCommandHandler<string, string, bool>("set_anim_bool", SetAnimBool);
        }
    }

    private void OnDisable()
    {
        // [VI] Gỡ lệnh khi bị tắt
        // [EN] Remove commands when disabled
        if (dialogueRunner != null)
        {
            dialogueRunner.RemoveCommandHandler("play_anim");
            dialogueRunner.RemoveCommandHandler("trigger_anim");
            dialogueRunner.RemoveCommandHandler("set_anim_bool");
        }
    }

    // ==========================================
    // CÁC HÀM ĐƯỢC GỌI TỪ YARN SPINNER (COMMANDS)
    // ==========================================

    /// <summary>
    /// Sử dụng trong Yarn: <<play_anim Tên_Nhân_Vật Tên_State_Animation>>
    /// VD: <<play_anim Zenith Pointing>>
    /// </summary>
    public void PlayAnimation(string charName, string stateName)
    {
        if (_animatorDict.TryGetValue(charName, out Animator anim))
        {
            Debug.Log($"<color=magenta>[ANIMATION]</color> {charName} đang chạy State: {stateName}");
            // Dùng CrossFade để chuyển mượt (0.25s) thay vì giật cục sang state mới
            anim.CrossFadeInFixedTime(stateName, 0.25f);
        }
        else
        {
            Debug.LogWarning($"<color=orange>[ANIMATION WARN]</color> Không tìm thấy Animator cho: {charName}");
        }
    }

    /// <summary>
    /// Sử dụng trong Yarn: <<trigger_anim Tên_Nhân_Vật Tên_Trigger>>
    /// VD: <<trigger_anim Player TakeDamage>>
    /// </summary>
    public void TriggerAnimation(string charName, string triggerName)
    {
        if (_animatorDict.TryGetValue(charName, out Animator anim))
        {
            Debug.Log($"<color=magenta>[ANIMATION]</color> Kích hoạt Trigger '{triggerName}' cho {charName}");
            anim.SetTrigger(triggerName);
        }
    }

    /// <summary>
    /// Sử dụng trong Yarn: <<set_anim_bool Tên_Nhân_Vật Tên_Bool true/false>>
    /// VD: <<set_anim_bool Zenith isTalking true>>
    /// </summary>
    public void SetAnimBool(string charName, string paramName, bool value)
    {
        if (_animatorDict.TryGetValue(charName, out Animator anim))
        {
            Debug.Log($"<color=magenta>[ANIMATION]</color> Set Bool '{paramName}' = {value} cho {charName}");
            anim.SetBool(paramName, value);
        }
    }
}