using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 인스펙터에서 대상 3요소(아이콘/텍스트/버튼)와
// 덮어씌울 새 아이콘/텍스트를 미리 할당해두고,
// Reveal() 함수 하나만 호출하면 그 값으로 덮어쓰고 버튼을 활성화하는 컴포넌트.
public class ButtonRevealer : MonoBehaviour
{
    [Header("대상 오브젝트 (덮어써질 대상)")]
    [SerializeField] private Image targetIconImage;       // 아이콘 역할을 하는 오브젝트
    [SerializeField] private TextMeshProUGUI targetText;   // 텍스트 역할을 하는 오브젝트
    [SerializeField] private Button targetButton;          // 버튼 역할을 하는 오브젝트

    [Header("새로 덮어씌울 요소")]
    [SerializeField] private Sprite newIcon;   // targetIconImage에 적용할 새 스프라이트
    [SerializeField] private string newText;   // targetText에 적용할 새 문자열

    [Header("색상")]
    [SerializeField] private Color enabledColor = Color.white; // 활성화 시 버튼 배경색

    // 이 함수 하나만 호출하면 위에서 할당해둔 값들로 아이콘/텍스트를 덮어쓰고 버튼을 활성화한다.
    public void Reveal()
    {
        if (targetIconImage != null)
        {
            targetIconImage.sprite = newIcon;
        }

        if (targetText != null)
        {
            targetText.text = newText;
        }

        if (targetButton != null)
        {
            targetButton.image.color = enabledColor; // 버튼 자체의 배경 이미지 색상을 하양으로
            targetButton.interactable = true;         // 클릭 가능하게 활성화
        }
    }
}