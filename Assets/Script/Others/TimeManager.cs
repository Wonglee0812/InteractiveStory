using System.Collections;
using UnityEngine;
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float slowMoScale = 0.2f;
    [SerializeField] private float transitionSpeed = 5f;

    private float _targetTimeScale = 1f;
    private float _initialFixedDeltaTime;
    private bool _isStopped = false;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        _initialFixedDeltaTime = Time.fixedDeltaTime;
    }
    public void TriggerHitStop(float duration = 0.05f)
    {
        // [VI] Nếu đang khựng rồi thì bỏ qua luôn, không làm khựng thêm!
        // [EN] If already stopped, ignore it entirely, no extra stopping!
        if (_isStopped) return;

        StartCoroutine(HitStopRoutine(duration));
    }
    private IEnumerator HitStopRoutine(float duration)
    {
        _isStopped = true;
        float originalTimeScale = Time.timeScale;

        // [VI] Khựng gần như hoàn toàn (0.05). Tránh dùng 0f vì có thể gây kẹt vật lý KCC.
        // [EN] Almost complete freeze (0.05). Avoid 0f as it might get KCC physics stuck.
        Time.timeScale = 0.05f;

        // [VI] BẮT BUỘC dùng Realtime, nếu không Coroutine này sẽ chờ vĩnh viễn!
        // [EN] MUST use Realtime, otherwise this Coroutine will wait forever!
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = originalTimeScale;
        _isStopped = false;
    }
    public void RequestSlowMotion(bool active)
    {
        _targetTimeScale = active ? slowMoScale : 1f;
    }

    private void Update()
    {
        // [VI] Dùng unscaledDeltaTime để việc Lerp không bị chậm theo chính nó!
        // [EN] Use unscaledDeltaTime so the Lerp doesn't slow down because of itself!
        Time.timeScale = Mathf.MoveTowards(Time.timeScale, _targetTimeScale, transitionSpeed * Time.unscaledDeltaTime);

        // [VI] QUAN TRỌNG: Giữ cho vật lý luôn mượt / [EN] IMPORTANT: Keep physics smooth
        Time.fixedDeltaTime = _initialFixedDeltaTime * Time.timeScale;
    }
}