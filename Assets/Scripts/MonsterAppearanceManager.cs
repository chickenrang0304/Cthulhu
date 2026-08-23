using UnityEngine;
using UnityEngine.InputSystem; 

// (SpriteRenderer가 있는 오브젝트, 혹은 그 자식 비주얼 오브젝트)
public class MonsterAppearanceController : MonoBehaviour
{
    [Header("비주얼")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite normalForm;   // 모에화 버전
    [SerializeField] private Sprite trueForm;     // 본모습 버전

    [Header("SAN 연동")]
    [SerializeField] private SanityManager sanityManager; // 씬에 있는 SanityManager 오브젝트 드래그
    [SerializeField] private float sanityDrainPerSecond = 1f;

    public bool IsShowingTrueForm { get; private set; }

    private void Update()
    {
        bool holdingTab = Keyboard.current.tabKey.isPressed;

        if (holdingTab)
        {
            SetTrueForm();
        }
        else if (!holdingTab)
        {
            SetNormalForm();
        }

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