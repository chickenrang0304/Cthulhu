using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class FilterManager : MonoBehaviour
{
    public static FilterManager Instance { get; private set; }

    [SerializeField] private DialogueRunner dialogueRunner;
    private const string VariableName = "$filterOn";

    public bool FilterOn { get; private set; } = true;

    [Header("필터 전환 플래시 효과")]
    [SerializeField] private Image flashImage; // 필터 On/Off 전환 시 깜빡일 UI 이미지 (화면 전체를 덮는 오버레이 등)
    [SerializeField] private float flashInDuration = 0.1f;  // 투명(0) -> 불투명(1)로 가는 데 걸리는 시간
    [SerializeField] private float flashOutDuration = 0.3f; // 불투명(1) -> 다시 투명(0)으로 돌아가는 데 걸리는 시간

    private Coroutine flashCoroutine; // 현재 진행 중인 플래시 코루틴 참조 (중복 실행 방지용)

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (dialogueRunner.VariableStorage.TryGetValue<bool>(VariableName, out bool value))
        {
            FilterOn = value;
        }
        else
        {
            dialogueRunner.VariableStorage.SetValue(VariableName, FilterOn);
        }

        // 시작 시 플래시 이미지는 완전히 투명한 상태여야 하므로 알파값을 0으로 초기화
        SetFlashAlpha(0f);
    }

    public void ToggleFilter()
    {
        FilterOn = !FilterOn;
        dialogueRunner.VariableStorage.SetValue(VariableName, FilterOn);

        foreach (var tracker in FilterableOptionText.ActiveTrackers)
        {
            tracker.RefreshText(FilterOn);
        }

        if (FilterableLastLine.Instance != null)
        {
            FilterableLastLine.Instance.RefreshText(FilterOn);
        }

        // 필터가 전환될 때마다 화면 플래시 효과를 실행
        TriggerFlash();
    }

    // 플래시 코루틴을 시작하는 함수. 이미 진행 중인 플래시가 있다면 멈추고 새로 시작해서
    // 연속으로 빠르게 토글해도 애니메이션이 꼬이지 않도록 함.
    private void TriggerFlash()
    {
        if (flashImage == null)
        {
            Debug.LogWarning("[FilterManager] flashImage가 연결되어 있지 않아 플래시 효과를 실행할 수 없습니다.");
            return;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    // 투명(0) -> 불투명(1) -> 다시 투명(0) 순서로 알파값을 애니메이션하는 코루틴
    private IEnumerator FlashRoutine()
    {
        // 시작은 항상 완전 투명한 상태에서 출발 (혹시 이전 플래시가 중간에 끊겨서 알파가 애매하게 남아있을 경우 대비)
        SetFlashAlpha(0f);

        // 0 -> 1로 빠르게 밝아짐
        yield return FadeAlpha(0f, 1f, flashInDuration);

        // 1 -> 0으로 다시 서서히 투명해짐
        yield return FadeAlpha(1f, 0f, flashOutDuration);

        flashCoroutine = null;
    }

    // from 값에서 to 값까지 duration 시간 동안 알파값을 선형으로 보간하는 범용 코루틴
    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration; // 0~1 사이 진행률
            float alpha = Mathf.Lerp(from, to, t);
            SetFlashAlpha(alpha);
            yield return null;
        }

        // 부동소수점 오차로 정확히 to에 도달 못할 수 있으니 마지막에 정확한 값으로 스냅
        SetFlashAlpha(to);
    }

    // flashImage의 알파값만 바꿔서 적용하는 헬퍼. 색상(RGB)은 그대로 두고 투명도만 조절.
    private void SetFlashAlpha(float alpha)
    {
        Color c = flashImage.color;
        c.a = alpha;
        flashImage.color = c;
    }
}