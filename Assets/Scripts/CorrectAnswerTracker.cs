using UnityEngine;
using System.Collections.Generic;
using Yarn.Unity;
using UnityEngine.UI;

public class CorrectAnswerTracker : MonoBehaviour
{
    // 현재 화면에 활성화된 모든 OptionItem 추적용 정적 리스트
    public static List<CorrectAnswerTracker> ActiveTrackers = new List<CorrectAnswerTracker>();

    private OptionItem optionItem;
    //[SerializeField] private Sprite image; // 67% 이상


    void Awake()
    {
        optionItem = GetComponent<OptionItem>();
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
        // 여기에 강조 효과 구현 (테두리 색, 반짝임 등)
        GetComponent<Image>().color = on ? Color.yellow : Color.white;
    }
}