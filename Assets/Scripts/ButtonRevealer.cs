using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 인스펙터에서 대상 3요소(아이콘/텍스트/버튼)와
// 덮어씌울 새 아이콘/텍스트를 미리 할당해두고,
// Reveal() 함수 하나만 호출하면 그 값으로 덮어쓰고 버튼을 활성화하는 컴포넌트.
public class ButtonRevealer : MonoBehaviour
{
    private static ButtonRevealer currentSelection;

    [Header("대상 오브젝트 (덮어써질 대상)")]
    [SerializeField] private SpriteRenderer targetIconImage;       // 아이콘 역할을 하는 오브젝트
    [SerializeField] private TextMeshProUGUI targetText;   // 텍스트 역할을 하는 오브젝트
    [SerializeField] private Button targetButton;          // 버튼 역할을 하는 오브젝트

    [Header("새로 덮어씌울 요소")]
    [SerializeField] private Sprite newIcon;   // targetIconImage에 적용할 새 스프라이트
    [SerializeField] private string newText;   // targetText에 적용할 새 문자열

    [Header("색상")]
    [SerializeField] private Color enabledColor = Color.white; // 활성화 시 버튼 배경색

    [Header("선택 표시")]
    [SerializeField] private Color selectedColor = new Color(0.62f, 0.86f, 0.72f, 1f);
    [SerializeField] private Color selectedOutlineColor = new Color(0.04f, 0.28f, 0.2f, 1f);

    [Header("상담 안내")]
    [SerializeField] private string startButtonText = "크투 양 상담 시작";
    [SerializeField, TextArea(6, 10)] private string briefingText =
        "위험도 : 하\n" +
        "상담 목표 : 지구 멸망 의사 확인\n\n" +
        "관찰 기록\n" +
        "- 강한 무료함을 호소함\n" +
        "- 인간 문화에 관심을 보임\n\n" +
        "주의사항\n" +
        "인지필터 장시간 해제 금지";

    private Button cardButton;
    private Outline selectionOutline;
    private ColorBlock originalColors;

    private void Awake()
    {
        cardButton = GetComponent<Button>();
        if (cardButton != null)
        {
            originalColors = cardButton.colors;
        }

        selectionOutline = GetComponent<Outline>();
        if (selectionOutline == null)
        {
            selectionOutline = gameObject.AddComponent<Outline>();
        }

        selectionOutline.effectColor = selectedOutlineColor;
        selectionOutline.effectDistance = new Vector2(4f, -4f);
        selectionOutline.useGraphicAlpha = true;
        selectionOutline.enabled = false;

        SetInitialGuidance();
    }

    private void SetInitialGuidance()
    {
        if (targetText != null)
        {
            targetText.text = "상담 대상을 선택하면\n상세 기록을 확인할 수 있습니다.";
        }

        SetButtonLabel(targetButton, "상담 대상을 선택하세요");
    }

    // 이 함수 하나만 호출하면 위에서 할당해둔 값들로 아이콘/텍스트를 덮어쓰고 버튼을 활성화한다.
    public void Reveal()
    {
        if (currentSelection != null && currentSelection != this)
        {
            currentSelection.SetSelected(false);
        }

        currentSelection = this;
        SetSelected(true);

        if (targetIconImage != null)
        {
            targetIconImage.sprite = newIcon;
            targetIconImage.color = enabledColor;
        }

        if (targetText != null)
        {
            targetText.text = string.IsNullOrWhiteSpace(briefingText) ? newText : briefingText;
        }

        if (targetButton != null)
        {
            targetButton.image.color = enabledColor; // 버튼 자체의 배경 이미지 색상을 하양으로
            targetButton.interactable = true;         // 클릭 가능하게 활성화
            SetButtonLabel(targetButton, startButtonText);
        }
    }

    private void SetSelected(bool selected)
    {
        if (selectionOutline != null)
        {
            selectionOutline.enabled = selected;
        }

        if (cardButton == null) return;

        if (!selected)
        {
            cardButton.colors = originalColors;
            return;
        }

        var colors = originalColors;
        colors.normalColor = selectedColor;
        colors.highlightedColor = Color.Lerp(selectedColor, Color.white, 0.2f);
        colors.selectedColor = selectedColor;
        colors.pressedColor = Color.Lerp(selectedColor, Color.black, 0.1f);
        cardButton.colors = colors;
        cardButton.Select();
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null) return;

        var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.text = label;
        }
    }
}
