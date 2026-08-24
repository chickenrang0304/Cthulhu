using UnityEngine;
using UnityEngine.InputSystem;

// (SpriteRenderer가 있는 오브젝트, 혹은 그 자식 비주얼 오브젝트)
public class MonsterAppearanceController : MonoBehaviour
{
    [Header("비주얼")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite normalForm;   // 모에화 버전 (필터 On)
    [SerializeField] private Sprite trueForm;     // 본모습 버전 (필터 Off)

    [Header("SAN 연동")]
    [SerializeField] private SanityManager sanityManager;
    [SerializeField] private float sanityDrainPerSecond = 1f;

    public FilterOnOffUI FilterUI;

    private void Update()
    {
        if (FilterManager.Instance == null)
        {
            return; // FilterManager가 아직 씬에 없으면 아무것도 안 함
        }

        bool pressedTab = Keyboard.current.tabKey.wasPressedThisFrame;

        if (pressedTab)
        {
            FilterManager.Instance.ToggleFilter();
        }

        bool filterOn = FilterManager.Instance.FilterOn;

        if (filterOn)
        {
            SetNormalForm();
        }
        else
        {
            SetTrueForm();
        }

        FilterUI.SetOnOff(filterOn);
    }

    private void SetTrueForm()
    {
        spriteRenderer.sprite = trueForm;
        sanityManager.DrainSanity(sanityDrainPerSecond * Time.deltaTime);

        // 여기에 화면 왜곡 이펙트 On, 사운드 재생 등 추가 가능
    }

    private void SetNormalForm()
    {
        spriteRenderer.sprite = normalForm;
    }
}