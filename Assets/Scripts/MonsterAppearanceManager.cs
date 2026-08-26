using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;

// SAN 연동 필터(모에화/본모습)와 얀 스피너 표정 시스템을 하나로 통합한 컨트롤러.
// 매 프레임 (현재 폼 x 현재 표정) 조합으로 스프라이트를 결정하기 때문에
// Update()가 표정을 덮어쓰는 충돌이 발생하지 않는다.
public class MonsterAppearanceController : MonoBehaviour
{
    [Header("비주얼")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("모에화 폼(필터 On) 표정별 스프라이트")]
    [SerializeField] private Sprite normalDefault;
    [SerializeField] private Sprite normalHappy;
    [SerializeField] private Sprite normalSad;
    [SerializeField] private Sprite normalAngry;

    [Header("본모습 폼(필터 Off) 표정별 스프라이트")]
    [Tooltip("본모습에서 표정 구분이 필요 없으면 4칸 모두 같은 스프라이트를 넣어도 된다")]
    [SerializeField] private Sprite trueDefault;
    [SerializeField] private Sprite trueHappy;
    [SerializeField] private Sprite trueSad;
    [SerializeField] private Sprite trueAngry;

    [Header("SAN 연동")]
    [SerializeField] private SanityManager sanityManager;
    [SerializeField] private float sanityDrainPerSecond = 1f;

    [Header("Dialogue Runner 연결")]
    [SerializeField] private DialogueRunner dialogueRunner;

    public FilterOnOffUI FilterUI;

    // 현재 표정 상태. Yarn 커맨드로 값이 바뀌고, Update()는 이 값을 "참고만" 한다.
    // (덮어쓰지 않고 조합해서 그린다는 게 핵심)
    private string currentExpression = "default";

    private void OnEnable()
    {
        // 표정 전환 커맨드 등록. 캐릭터가 한 명뿐이라 오브젝트 이름 없이
        // <<expression happy>> 형태로 바로 호출 가능하게 함.
        if (dialogueRunner != null)
        {
            dialogueRunner.AddCommandHandler<string>("expression", SetExpression);
        }
    }

    private void OnDisable()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.RemoveCommandHandler("expression");
        }
    }

    private void Update()
    {
        if (FilterManager.Instance == null)
        {
            return; // FilterManager가 아직 씬에 없으면 아무것도 안 함
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            FilterManager.Instance.ToggleFilter();
        }

        bool filterOn = FilterManager.Instance.FilterOn;

        // 본모습일 때만 SAN 드레인 (원래 로직 유지)
        if (!filterOn)
        {
            sanityManager.DrainSanity(sanityDrainPerSecond * Time.deltaTime);
        }

        // 매 프레임 (폼 x 표정) 조합으로 스프라이트를 다시 계산해서 적용.
        // 이렇게 하면 Update가 계속 돌아도 currentExpression 값이 유지되는 한
        // 표정이 기본으로 리셋되지 않는다.
        ApplySprite(filterOn);

        FilterUI.SetOnOff(filterOn);
    }

    // 표정 이름을 받아서 상태만 갱신. 실제 스프라이트 적용은 Update()에서 매 프레임 처리.
    private void SetExpression(string expressionName)
    {
        string normalized = expressionName.ToLower();

        // 유효한 표정 이름인지 미리 검증 (오타 방지)
        if (normalized != "happy" && normalized != "sad" &&
            normalized != "angry" && normalized != "default")
        {
            Debug.LogWarning($"[MonsterAppearanceController] 알 수 없는 표정 이름: {expressionName}");
            return;
        }

        currentExpression = normalized;
    }

    // 현재 폼(filterOn)과 현재 표정(currentExpression)을 조합해서 스프라이트를 결정하고 적용
    private void ApplySprite(bool filterOn)
    {
        Sprite target;

        if (filterOn)
        {
            // 모에화 폼
            target = currentExpression switch
            {
                "happy" => normalHappy,
                "sad" => normalSad,
                "angry" => normalAngry,
                _ => normalDefault, // "default" 및 예외 케이스
            };
        }
        else
        {
            // 본모습 폼
            target = currentExpression switch
            {
                "happy" => trueHappy,
                "sad" => trueSad,
                "angry" => trueAngry,
                _ => trueDefault,
            };
        }

        // 매 프레임 같은 스프라이트를 재할당하는 게 걸린다면
        // (참조 비교 후 다를 때만 할당하도록) 최적화 가능하지만,
        // Sprite 재할당 자체는 가벼운 연산이라 지금은 그대로 둬도 무방하다.
        spriteRenderer.sprite = target;
    }
}