using UnityEngine;
using System.Collections.Generic;
using Yarn.Unity;
using UnityEngine.UI;

public class CorrectAnswerTracker : MonoBehaviour
{
    // 현재 화면에 활성화된 모든 OptionItem 추적용 정적 리스트
    public static List<CorrectAnswerTracker> ActiveTrackers = new List<CorrectAnswerTracker>();

    private OptionItem optionItem;
    private Image backgroundImage;
    private Color normalBackgroundColor;

    [Header("Highlight")]
    [SerializeField] private Color correctHighlightColor = new Color(0.32f, 0.27f, 0.08f, 0.96f);
    //[SerializeField] private Sprite image; // 67% 이상


    void Awake()
    {
        optionItem = GetComponent<OptionItem>();
        backgroundImage = GetComponent<Image>();
        if (backgroundImage != null)
        {
            normalBackgroundColor = backgroundImage.color;
        }
    }

    void OnEnable()
    {
        if (!ActiveTrackers.Contains(this))
        {
            ActiveTrackers.Add(this);
        }
    }

    void OnDisable()
    {
        ActiveTrackers.Remove(this);
        SetHighlight(false); // 꺼질 때 강조 효과도 초기화
    }

    public bool IsCorrect()
    {
        if (optionItem == null) return false;

        try
        {
            var option = optionItem.Option; // Option이 아직 할당 안 됐으면 예외 발생
            return option.Line.Metadata != null &&
                   System.Array.IndexOf(option.Line.Metadata, "correct") >= 0;
        }
        catch
        {
            return false;
        }
    }

    public void SetHighlight(bool on)
    {
        if (backgroundImage == null) return;

        // 강조가 끝난 뒤 흰색으로 덮지 않고 프리팹의 원래 어두운 색으로 되돌린다.
        backgroundImage.color = on ? correctHighlightColor : normalBackgroundColor;
    }
}
