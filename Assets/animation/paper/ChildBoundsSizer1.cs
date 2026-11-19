using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 자식들을 "정렬하지 않고", 자식들이 차지하는 크기만 계산해서
/// Content Size Fitter에게 알려주는 레이아웃 컴포넌트.
///
/// [동작 방식]
/// - 자식들의 위치는 절대 건드리지 않는다. (Horizontal/Vertical Layout Group과의 차이점)
/// - 이 오브젝트의 "왼쪽 위 모서리"에서부터 가장 멀리 있는 자식의
///   오른쪽 끝 / 아래쪽 끝까지의 거리를 재서 preferred 크기로 대답한다.
/// - 같은 오브젝트에 Content Size Fitter를 붙이고 Preferred Size로 설정하면
///   그 값만큼 크기가 바뀐다.
///
/// [사용 조건]
/// - 자식들의 Anchor는 "왼쪽 위(Top-Left)"로 두는 것을 권장한다.
///   부모 크기가 바뀌어도 자식이 왼쪽 위 기준으로 제자리에 있어야 측정값이 흔들리지 않는다.
/// - 자식이 부모 크기에 맞춰 늘어나는 Stretch Anchor를 쓰면 안 된다.
///   (부모가 커짐 → 자식이 커짐 → 부모가 또 커짐... 무한히 커지는 문제가 생긴다)
/// - 직계 자식만 측정한다. 손자 오브젝트가 자식 밖으로 삐져나온 부분은 계산하지 않는다.
/// </summary>
[ExecuteAlways]                          // 에디터에서 플레이하지 않아도 크기가 바로 반영되게 한다
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class ChildBoundsSizer : UIBehaviour, ILayoutElement, ILayoutGroup
{
    // ───────────────────────── 인스펙터 설정 ─────────────────────────

    [Tooltip("가장 오른쪽 자식 뒤에 더할 여백")]
    [SerializeField] private float paddingRight = 0f;

    [Tooltip("가장 아래쪽 자식 밑에 더할 여백")]
    [SerializeField] private float paddingBottom = 0f;

    [Tooltip("켜 두면 매 프레임 자식 크기/위치 변화를 감시한다.\n" +
             "자식을 Animator나 코드로 움직이면 레이아웃 시스템이 알아채지 못하기 때문에 필요하다.\n" +
             "자식이 거의 안 바뀌면 꺼 두고, 바뀔 때 SetDirty()를 직접 호출해도 된다.")]
    [SerializeField] private bool watchEveryFrame = true;

    // ───────────────────────── 내부 변수 ─────────────────────────

    // 이 오브젝트의 RectTransform (매번 GetComponent 하지 않도록 저장해 둠)
    private RectTransform _rect;
    private RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);

    // 마지막으로 계산한 크기 (x = 가로, y = 세로)
    private Vector2 _calculatedSize;

    // 모서리 4개를 받아 올 배열. 매번 새로 만들면 가비지가 생기므로 하나만 만들어 재사용한다.
    private static readonly Vector3[] s_Corners = new Vector3[4];

    // ───────────────────────── ILayoutElement ─────────────────────────
    // Content Size Fitter가 "너 얼마나 커야 해?"라고 물을 때 읽어 가는 값들.

    // 최소 크기: 따로 제한하지 않으므로 0
    public float minWidth => 0f;
    public float minHeight => 0f;

    // 원하는 크기: 자식들을 측정한 결과
    public float preferredWidth => _calculatedSize.x;
    public float preferredHeight => _calculatedSize.y;

    // 남는 공간을 차지하려는 정도: -1은 "설정 안 함"이라는 뜻
    public float flexibleWidth => -1f;
    public float flexibleHeight => -1f;

    // 같은 오브젝트에 Text나 Image가 있어도 이 컴포넌트의 값이 우선되도록 1로 둔다 (기본값은 0)
    public int layoutPriority => 1;

    // 레이아웃 시스템이 "가로 크기를 계산해라"라고 호출하는 함수
    public void CalculateLayoutInputHorizontal()
    {
        _calculatedSize = MeasureChildren();
    }

    // 레이아웃 시스템이 "세로 크기를 계산해라"라고 호출하는 함수
    // (가로 계산 뒤에 부모 너비가 바뀌었을 수 있으니 한 번 더 측정한다)
    public void CalculateLayoutInputVertical()
    {
        _calculatedSize = MeasureChildren();
    }

    // ───────────────────────── ILayoutGroup ─────────────────────────
    // 이 인터페이스를 붙인 이유는 딱 하나다.
    // 자식의 크기가 바뀌었을 때(예: 자식 Text의 글자가 바뀜) 유니티는
    // 부모 중에 ILayoutGroup이 있는지 찾아서 "다시 계산해라"라고 알려준다.
    // 이 인터페이스가 없으면 그 알림을 못 받는다.
    //
    // 실제로 자식을 배치하는 함수는 이 두 개인데,
    // 우리는 자식을 정렬하고 싶지 않으므로 일부러 비워 둔다.

    public void SetLayoutHorizontal() { /* 자식 위치를 건드리지 않음 */ }
    public void SetLayoutVertical()   { /* 자식 위치를 건드리지 않음 */ }

    // ───────────────────────── 측정 ─────────────────────────

    /// <summary>
    /// 이 오브젝트의 왼쪽 위 모서리에서부터
    /// 활성화된 직계 자식들의 가장 먼 오른쪽 끝 / 아래쪽 끝까지의 거리를 잰다.
    /// </summary>
    private Vector2 MeasureChildren()
    {
        RectTransform self = Rect;

        // self.rect는 이 오브젝트 자신의 로컬 좌표 기준 사각형이다.
        // xMin = 왼쪽 끝, yMax = 위쪽 끝
        Rect selfRect = self.rect;

        float farthestRight = 0f;  // 왼쪽 끝에서 가장 오른쪽 자식 끝까지의 거리
        float farthestBottom = 0f; // 위쪽 끝에서 가장 아래쪽 자식 끝까지의 거리

        for (int i = 0; i < self.childCount; i++)
        {
            // RectTransform이 아닌 자식(일반 Transform)은 건너뛴다
            if (!(self.GetChild(i) is RectTransform child)) continue;

            // 꺼져 있는 자식은 크기 계산에서 뺀다.
            // → 키워드 패널을 SetActive(false) 하면 서류가 자동으로 줄어든다.
            if (!child.gameObject.activeSelf) continue;

            // 자식에 Layout Element가 있고 Ignore Layout이 켜져 있으면 무시한다.
            // (장식용 그림자, 배경 이펙트 등을 계산에서 빼고 싶을 때 사용)
            ILayoutIgnorer ignorer = child.GetComponent<ILayoutIgnorer>();
            if (ignorer != null && ignorer.ignoreLayout) continue;

            // 자식의 네 모서리를 월드 좌표로 구한 뒤,
            // 이 오브젝트의 로컬 좌표로 바꿔서 비교한다.
            // (자식이 회전되거나 Scale이 바뀌어 있어도 정확하게 잴 수 있다)
            child.GetWorldCorners(s_Corners);
            for (int c = 0; c < 4; c++)
            {
                Vector3 local = self.InverseTransformPoint(s_Corners[c]);

                // 왼쪽 끝에서 이 모서리까지의 가로 거리
                farthestRight = Mathf.Max(farthestRight, local.x - selfRect.xMin);

                // 위쪽 끝에서 이 모서리까지의 세로 거리 (유니티 UI는 위가 +y이므로 위에서 뺀다)
                farthestBottom = Mathf.Max(farthestBottom, selfRect.yMax - local.y);
            }
        }

        return new Vector2(farthestRight + paddingRight, farthestBottom + paddingBottom);
    }

    // ───────────────────────── 다시 계산 요청 ─────────────────────────

    /// <summary>
    /// "크기를 다시 계산해 달라"고 레이아웃 시스템에 요청한다.
    /// 실제 계산은 이번 프레임이 그려지기 직전에 한 번에 처리된다.
    /// 자식을 코드로 움직인 직후 바로 크기를 알고 싶다면 대신
    /// LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform)를 호출한다.
    /// </summary>
    public void SetDirty()
    {
        if (!IsActive()) return;
        LayoutRebuilder.MarkLayoutForRebuild(Rect);
    }

    // 매 프레임 끝에서 자식 변화를 감시한다.
    // Animator나 코드로 자식의 위치/크기를 바꾸면 레이아웃 시스템이 알아채지 못하므로,
    // 직접 다시 재 보고 이전 값과 다르면 다시 계산을 요청한다.
    private void LateUpdate()
    {
        if (!watchEveryFrame) return;

        Vector2 now = MeasureChildren();

        // 아주 작은 차이(소수점 오차)는 무시한다. 안 그러면 매 프레임 다시 계산하게 된다.
        if ((now - _calculatedSize).sqrMagnitude > 0.0001f)
        {
            SetDirty();
        }
    }

    // ───────────────────────── 유니티 이벤트 ─────────────────────────

    // 컴포넌트가 켜질 때: 처음 크기 계산
    protected override void OnEnable()
    {
        base.OnEnable();
        SetDirty();
    }

    // 컴포넌트가 꺼질 때: 부모 레이아웃도 다시 계산되도록 알린다
    protected override void OnDisable()
    {
        LayoutRebuilder.MarkLayoutForRebuild(Rect);
        base.OnDisable();
    }

    // 자식이 추가/삭제되거나 순서가 바뀌었을 때
    private void OnTransformChildrenChanged()
    {
        SetDirty();
    }

    // 이 오브젝트에 붙은 Animator가 값을 바꿨을 때
    protected override void OnDidApplyAnimationProperties()
    {
        SetDirty();
    }

#if UNITY_EDITOR
    // 인스펙터에서 padding 같은 값을 바꿨을 때
    protected override void OnValidate()
    {
        base.OnValidate();
        SetDirty();
    }
#endif
}
